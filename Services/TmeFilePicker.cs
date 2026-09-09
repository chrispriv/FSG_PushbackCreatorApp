namespace FSG_PushbackCreator.Services;

public static class TmeFilePicker
{
	public static async Task<Stream?> PickOpenStreamAsync(string extension = ".tme")
	{
#if WINDOWS
		var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (window is null)
			return null;

		var picker = new Windows.Storage.Pickers.FileOpenPicker();
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
		WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
		picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
		picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
		picker.FileTypeFilter.Add(extension.StartsWith('.') ? extension : "." + extension);

		var file = await picker.PickSingleFileAsync();
		if (file is null)
			return null;

		await using var source = await file.OpenStreamForReadAsync();
		var copy = new MemoryStream();
		await source.CopyToAsync(copy);
		copy.Position = 0;
		return copy;
#else
		var types = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
		{
			{ DevicePlatform.Android, new[] { "*/*", "application/octet-stream", "application/zip", "application/x-zip-compressed" } },
			{ DevicePlatform.iOS, new[] { "public.data", "public.zip-archive" } },
			{ DevicePlatform.macOS, new[] { "tme", "zip" } }
		});
		var result = await FilePicker.Default.PickAsync(new PickOptions
		{
			PickerTitle = extension.Contains("zip", StringComparison.OrdinalIgnoreCase) ? "Open ZIP (FS4)" : "Open TME (FSG)",
			FileTypes = types
		});
		if (result is null)
			return null;

		await using var picked = await result.OpenReadAsync();
		var copy = new MemoryStream();
		await picked.CopyToAsync(copy);
		copy.Position = 0;
		return copy;
#endif
	}
}
