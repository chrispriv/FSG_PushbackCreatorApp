using Android.Content;
using Android.OS;
using Android.Provider;
using Uri = Android.Net.Uri;

namespace FSG_PushbackCreator;

/// <summary>
/// Writes to the public Downloads folder and replaces a file with the same name.
/// ACTION_CREATE_DOCUMENT cannot overwrite and would create "name (1).tme".
/// </summary>
public static class AndroidDownloadsStore
{
	public static async Task<string> SaveOverwriteAsync(string fileName, string mime, byte[] bytes)
	{
		var activity = Platform.CurrentActivity
			?? throw new InvalidOperationException("No Android activity is available.");

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
			return await SaveWithMediaStoreAsync(activity, fileName, mime, bytes);

		var dir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)
			?? throw new InvalidOperationException("Downloads folder is not available.");
		dir.Mkdirs();
		var path = Path.Combine(dir.AbsolutePath, fileName);
		await File.WriteAllBytesAsync(path, bytes);
		return path;
	}

	[System.Runtime.Versioning.SupportedOSPlatform("android29.0")]
	static async Task<string> SaveWithMediaStoreAsync(
		Android.App.Activity activity, string fileName, string mime, byte[] bytes)
	{
		var resolver = activity.ContentResolver
			?? throw new InvalidOperationException("No Android content resolver is available.");
		var collection = MediaStore.Downloads.ExternalContentUri;
		var existing = FindDownload(resolver, collection, fileName);
		Uri uri;

		if (existing is not null)
		{
			uri = existing;
		}
		else
		{
			var values = new ContentValues();
			values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
			values.Put(MediaStore.IMediaColumns.MimeType, mime);
			values.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);
			uri = resolver.Insert(collection, values)
				?? throw new InvalidOperationException("Could not create the file in Downloads.");
		}

		var stream = resolver.OpenOutputStream(uri, "rwt")
			?? throw new InvalidOperationException("Could not write the file in Downloads.");
		await using (stream)
		{
			await stream.WriteAsync(bytes);
			await stream.FlushAsync();
		}

		return Path.Combine("/storage/emulated/0/Download", fileName);
	}

	[System.Runtime.Versioning.SupportedOSPlatform("android29.0")]
	static Uri? FindDownload(ContentResolver resolver, Uri collection, string fileName)
	{
		string[] projection = ["_id"];
		using var cursor = resolver.Query(
			collection,
			projection,
			$"{MediaStore.IMediaColumns.DisplayName}=?",
			[fileName],
			null);
		if (cursor is null || !cursor.MoveToFirst())
			return null;

		var idIndex = cursor.GetColumnIndex("_id");
		if (idIndex < 0)
			return null;

		return ContentUris.WithAppendedId(collection, cursor.GetLong(idIndex));
	}
}
