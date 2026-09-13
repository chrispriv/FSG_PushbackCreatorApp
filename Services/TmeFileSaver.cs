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
		var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (window is null)
			return null;

		var picker = new Windows.Storage.Pickers.FileSavePicker();
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
		WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
		picker.SuggestedFileName = suggested;
		picker.FileTypeChoices.Add(fileTypeName, [extension]);
		picker.DefaultFileExtension = extension;

		var file = await picker.PickSaveFileAsync();
		if (file is null)
			return null;

		var package = PackageNames.FromFileName(file.Name);
		var bytes = build(package);
		await File.WriteAllBytesAsync(file.Path, bytes);
		return file.Path;
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
#else
		_ = fileTypeName;
		var path = Path.Combine(FileSystem.Current.AppDataDirectory, suggestedFile);
		var bytes = build(suggested);
		await File.WriteAllBytesAsync(path, bytes);
		return path;
#endif
	}
}
