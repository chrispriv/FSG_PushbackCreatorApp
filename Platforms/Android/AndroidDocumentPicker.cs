using Android.Content;
using Android.Provider;
using Uri = Android.Net.Uri;

namespace FSG_PushbackCreator;

public static class AndroidDocumentPicker
{
	public const int RequestCreateDocument = 41011;

	public static TaskCompletionSource<Uri?>? CreateDocumentTcs { get; set; }

	public static async Task<(Uri Uri, string DisplayName)?> CreateDocumentAsync(string mimeType, string fileName)
	{
		var activity = Platform.CurrentActivity;
		if (activity is null)
			return null;

		CreateDocumentTcs = new TaskCompletionSource<Uri?>();
		var intent = new Intent(Intent.ActionCreateDocument);
		intent.AddCategory(Intent.CategoryOpenable);
		intent.SetType(mimeType);
		intent.PutExtra(Intent.ExtraTitle, fileName);
		activity.StartActivityForResult(intent, RequestCreateDocument);

		var uri = await CreateDocumentTcs.Task;
		if (uri is null)
			return null;

		var display = QueryDisplayName(activity, uri) ?? fileName;
		return (uri, display);
	}

	public static string? QueryDisplayName(Android.App.Activity activity, Uri uri)
	{
		var resolver = activity.ContentResolver;
		if (resolver is null)
			return null;

		var cursor = resolver.Query(uri, [Android.Provider.IOpenableColumns.DisplayName], null, null, null);
		if (cursor is null)
			return null;

		using (cursor)
		{
			if (!cursor.MoveToFirst())
				return null;
			var index = cursor.GetColumnIndex(Android.Provider.IOpenableColumns.DisplayName);
			return index >= 0 ? cursor.GetString(index) : null;
		}
	}

	public static async Task WriteUriAsync(Uri uri, byte[] bytes)
	{
		var activity = Platform.CurrentActivity
			?? throw new InvalidOperationException("No Android activity is available.");
		var stream = activity.ContentResolver?.OpenOutputStream(uri, "rwt")
			?? throw new InvalidOperationException("Could not write the selected file.");
		await using (stream)
		{
			await stream.WriteAsync(bytes);
			await stream.FlushAsync();
		}
	}
}
