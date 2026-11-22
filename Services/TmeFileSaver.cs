namespace FSG_PushbackCreator.Services;

public static class TmeFileSaver
{
	public static async Task<string?> SaveProjectArchiveAsync(
		Func<string, byte[]> build,
		string suggestedPackageName,
		string fileTypeName,
		string extension)
	{
		if (!extension.StartsWith('.'))
			extension = "." + extension;

		var suggested = PackageNames.Sanitize(suggestedPackageName);
		var suggestedFile = suggested + extension;

#if WINDOWS
		_ = fileTypeName;
		var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (window is null)
			return null;

		var picker = new Windows.Storage.Pickers.FolderPicker();
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
		WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
		picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
		picker.FileTypeFilter.Add("*");

		var folder = await picker.PickSingleFolderAsync();
		if (folder is null)
			return null;

		var path = Path.Combine(folder.Path, suggestedFile);
		if (File.Exists(path))
		{
			var page = Application.Current?.Windows.FirstOrDefault()?.Page;
			if (page is not null)
			{
				var replace = await page.DisplayAlert(
					"Replace file?",
					$"{suggestedFile} already exists in this folder. Replace it?",
					"Replace",
					"Cancel");
				if (!replace)
					return null;
			}
		}

		var package = suggested;
		var bytes = build(package);
		await File.WriteAllBytesAsync(path, bytes);
		return path;
#elif ANDROID
		_ = fileTypeName;
		var mime = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
			? "application/zip"
			: extension.Equals(".kml", StringComparison.OrdinalIgnoreCase)
				? "application/vnd.google-earth.kml+xml"
				: "application/octet-stream";
		var package = suggested;
		var bytes = build(package);
		try
		{
			return await global::FSG_PushbackCreator.AndroidDownloadsStore.SaveOverwriteAsync(
				suggestedFile, mime, bytes);
		}
		catch
		{
			var picked = await global::FSG_PushbackCreator.AndroidDocumentPicker.CreateDocumentAsync(mime, suggestedFile);
			if (picked is null)
				return null;
			await global::FSG_PushbackCreator.AndroidDocumentPicker.WriteUriAsync(picked.Value.Uri, bytes);
			return picked.Value.DisplayName;
		}
#elif MACCATALYST || IOS
		_ = fileTypeName;
		var picked = await global::FSG_PushbackCreator.AppleFolderAccess.PickAsync();
		if (picked is null)
			return null;
		var package = suggested;
		var bytes = build(package);
		return global::FSG_PushbackCreator.AppleFolderAccess.WriteFile(suggestedFile, bytes);
#else
		_ = fileTypeName;
		var path = Path.Combine(FileSystem.Current.AppDataDirectory, suggestedFile);
		var bytes = build(suggested);
		await File.WriteAllBytesAsync(path, bytes);
		return path;
#endif
	}
}
