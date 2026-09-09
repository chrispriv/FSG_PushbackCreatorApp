using FSG_PushbackCreator.Services;

namespace FSG_PushbackCreator.Pages;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
	}

	async void OnNewProject(object? sender, EventArgs e)
	{
		ProjectSession.StartNew();
		await Shell.Current.GoToAsync("airports");
	}

	async void OnOpenTme(object? sender, EventArgs e) =>
		await OpenArchiveAsync("Open TME", ".tme");

	async void OnOpenZip(object? sender, EventArgs e) =>
		await OpenArchiveAsync("Open ZIP (FS4)", ".zip");

	async Task OpenArchiveAsync(string title, string extension)
	{
		try
		{
			await using var stream = await TmeFilePicker.PickOpenStreamAsync(extension);
			if (stream is null)
				return;

			var imported = TmeArchiveReader.Read(stream);
			ProjectSession.Open(imported.Project);
			await Shell.Current.GoToAsync("airports");
			var skip = imported.SkippedCount == 0
				? string.Empty
				: $" {imported.SkippedCount} other zip entries were skipped.";
			await DisplayAlert(
				title,
				$"Loaded {imported.TscCount} airport(s) from {imported.Project.PackageFolderName}.{skip}",
				"OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert(title, ex.Message, "OK");
		}
	}
}
