#if IOS || MACCATALYST
using Foundation;
using UIKit;
using UniformTypeIdentifiers;

namespace FSG_PushbackCreator;

/// <summary>
/// Mac/iOS folder picker with a security-scoped bookmark (same approach as TgiModelTransformer).
/// Sandboxed apps cannot write to Documents/Downloads until the user picks that folder.
/// </summary>
internal static class AppleFolderAccess
{
	const string PrefBookmark = "apple_output_folder_bookmark";
	const string PrefName = "apple_output_folder_name";

	static UIDocumentPickerViewController? _picker;
	static PickerDelegate? _delegate;

	public static Task<string?> PickAsync()
	{
		var presenter = Microsoft.Maui.ApplicationModel.Platform.GetCurrentUIViewController();
		if (presenter is null)
			return Task.FromResult<string?>(null);

		var tcs = new TaskCompletionSource<string?>();
		var picker = new UIDocumentPickerViewController(new[] { UTTypes.Folder }, asCopy: false);
		picker.AllowsMultipleSelection = false;
		var del = new PickerDelegate(tcs);
		picker.Delegate = del;
		_picker = picker;
		_delegate = del;
		presenter.PresentViewController(picker, true, null);
		return tcs.Task;
	}

	public static string WriteFile(string fileName, byte[] bytes)
	{
		using var access = AccessSavedFolder();
		var path = Path.Combine(access.Path, fileName);
		File.WriteAllBytes(path, bytes);
		return path;
	}

	static string? Commit(NSUrl? url)
	{
		_picker = null;
		_delegate = null;
		if (url is null)
			return null;
		if (!url.StartAccessingSecurityScopedResource())
			return url.LastPathComponent;
		try
		{
			NSError? error;
			var bookmark = CreateBookmark(url, out error);
			if (bookmark is null || error is not null)
				throw new InvalidOperationException(error?.LocalizedDescription ?? "Could not remember the folder.");
			Preferences.Default.Set(PrefBookmark, Convert.ToBase64String(bookmark.ToArray()));
			Preferences.Default.Set(PrefName, url.LastPathComponent ?? "Folder");
			return url.LastPathComponent ?? "Folder";
		}
		finally
		{
			url.StopAccessingSecurityScopedResource();
		}
	}

	static NSData? CreateBookmark(NSUrl url, out NSError? error)
	{
		var options = NSUrlBookmarkCreationOptions.SuitableForBookmarkFile;
		return url.CreateBookmarkData(options, resourceValues: null, relativeUrl: null, out error);
	}

	static FolderAccess AccessSavedFolder()
	{
		var b64 = Preferences.Default.Get(PrefBookmark, "");
		if (string.IsNullOrEmpty(b64))
			throw new InvalidOperationException("Choose a folder first.");
		var data = NSData.FromArray(Convert.FromBase64String(b64));
		var options = NSUrlBookmarkResolutionOptions.WithoutUI;
		var url = NSUrl.FromBookmarkData(data, options, relativeToUrl: null, out var stale, out var error);
		if (url is null || error is not null)
			throw new InvalidOperationException(error?.LocalizedDescription ?? "Could not open the chosen folder. Pick it again.");
		if (stale)
		{
			url.StartAccessingSecurityScopedResource();
			try
			{
				var refreshed = CreateBookmark(url, out var refreshError);
				if (refreshed is not null && refreshError is null)
					Preferences.Default.Set(PrefBookmark, Convert.ToBase64String(refreshed.ToArray()));
			}
			finally
			{
				url.StopAccessingSecurityScopedResource();
			}
		}

		if (!url.StartAccessingSecurityScopedResource())
			throw new InvalidOperationException("Could not open the chosen folder. Pick it again.");
		var path = url.Path;
		if (string.IsNullOrEmpty(path))
		{
			url.StopAccessingSecurityScopedResource();
			throw new InvalidOperationException("Could not open the chosen folder.");
		}

		return new FolderAccess(url, path);
	}

	sealed class FolderAccess : IDisposable
	{
		public NSUrl Url { get; }
		public string Path { get; }
		bool _stopped;

		public FolderAccess(NSUrl url, string path)
		{
			Url = url;
			Path = path;
		}

		public void Dispose()
		{
			if (_stopped)
				return;
			_stopped = true;
			Url.StopAccessingSecurityScopedResource();
		}
	}

	sealed class PickerDelegate : UIDocumentPickerDelegate
	{
		readonly TaskCompletionSource<string?> _tcs;

		public PickerDelegate(TaskCompletionSource<string?> tcs) => _tcs = tcs;

		public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
		{
			var url = urls is { Length: > 0 } ? urls[0] : null;
			_tcs.TrySetResult(Commit(url));
		}

		public override void WasCancelled(UIDocumentPickerViewController controller)
		{
			_picker = null;
			_delegate = null;
			_tcs.TrySetResult(null);
		}
	}
}
#endif
