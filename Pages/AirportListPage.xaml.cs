using FSG_PushbackCreator.Models;
using FSG_PushbackCreator.Services;

namespace FSG_PushbackCreator.Pages;

public partial class AirportListPage : ContentPage, IShellBackHandler
{
	bool _leaving;
	bool _pausePackageDirty;

	public AirportListPage()
	{
		InitializeComponent();
	}

	public bool AllowNavigateAway => _leaving;

	public Task HandleShellBackAsync() => OnCancelAsync();

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_pausePackageDirty = true;
		PackageNameEntry.Text = ProjectSession.Current.PackageFolderName;
		_pausePackageDirty = false;
		foreach (var airport in ProjectSession.Current.Airports)
			airport.NotifyListSummary();
		AirportList.ItemsSource = null;
		AirportList.ItemsSource = ProjectSession.Current.Airports;
	}

	protected override bool OnBackButtonPressed()
	{
		_ = OnCancelAsync();
		return true;
	}

	void OnPackageNameUnfocused(object? sender, FocusEventArgs e) => ApplyPackageName();

	void ApplyPackageName()
	{
		if (_pausePackageDirty)
			return;

		var sanitized = PackageNames.Sanitize(PackageNameEntry.Text);
		if (PackageNameEntry.Text != sanitized)
			PackageNameEntry.Text = sanitized;
		if (ProjectSession.Current.PackageFolderName == sanitized)
			return;

		ProjectSession.Current.PackageFolderName = sanitized;
		ProjectSession.MarkDirty();
	}

	async void OnCancel(object? sender, EventArgs e) => await OnCancelAsync();

	async Task OnCancelAsync()
	{
		if (_leaving)
			return;

		if (ProjectSession.HasUnexportedChanges &&
		    !await UnsavedChanges.ConfirmCloseProjectAsync(this))
			return;

		_leaving = true;
		ProjectSession.Close();
		await Shell.Current.GoToAsync("//home");
	}

	async void OnNewAirport(object? sender, EventArgs e)
	{
		var choice = await DisplayActionSheet(
			"New airport",
			"Cancel",
			null,
			"Pushback airport (ICAO + 00)",
			"Heliport (4-character code)");

		AirportKind kind;
		if (choice == "Pushback airport (ICAO + 00)")
			kind = AirportKind.DummyPushback;
		else if (choice == "Heliport (4-character code)")
			kind = AirportKind.Heliport;
		else
			return;

		var airport = new AirportEntry { Kind = kind, IsNew = true };
		ProjectSession.Current.Airports.Add(airport);
		await Shell.Current.GoToAsync($"airport?airportId={airport.Id}");
	}

	async void OnAddTme(object? sender, EventArgs e) => await MergeArchiveAsync("Add TME (FSG)", ".tme");

	async void OnAddZip(object? sender, EventArgs e) => await MergeArchiveAsync("Add ZIP (FS4)", ".zip");

	async Task MergeArchiveAsync(string title, string extension)
	{
		try
		{
			await using var stream = await TmeFilePicker.PickOpenStreamAsync(extension);
			if (stream is null)
				return;

			var imported = TmeArchiveReader.Read(stream);
			var added = 0;
			var skippedExisting = 0;
			foreach (var airport in imported.Project.Airports)
			{
				if (ProjectSession.Current.Airports.Any(a =>
				        string.Equals(a.TmeCode, airport.TmeCode, StringComparison.OrdinalIgnoreCase)))
				{
					skippedExisting++;
					continue;
				}

				ProjectSession.Current.Airports.Add(airport);
				added++;
			}

			if (added > 0)
				ProjectSession.MarkDirty();

			AirportList.ItemsSource = null;
			AirportList.ItemsSource = ProjectSession.Current.Airports;

			var other = imported.SkippedCount == 0
				? string.Empty
				: $" {imported.SkippedCount} other zip entries were skipped.";
			var dup = skippedExisting == 0
				? string.Empty
				: $" {skippedExisting} airport(s) already in this project were skipped.";
			await DisplayAlert(title, $"Added {added} airport(s).{dup}{other}", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert(title, ex.Message, "OK");
		}
	}

	async void OnEdit(object? sender, EventArgs e)
	{
		if (sender is not Button { CommandParameter: AirportEntry airport })
			return;

		airport.IsNew = false;
		await Shell.Current.GoToAsync($"airport?airportId={airport.Id}");
	}

	async void OnDelete(object? sender, EventArgs e)
	{
		if (sender is not Button { CommandParameter: AirportEntry airport })
			return;

		var code = string.IsNullOrEmpty(airport.TmeCode) ? "this airport" : airport.TmeCode;
		if (!await DisplayAlert("Delete airport", $"Remove {code} from the project?", "Delete", "Cancel"))
			return;

		ProjectSession.Current.Airports.Remove(airport);
		ProjectSession.MarkDirty();
	}

	async void OnExportTme(object? sender, EventArgs e) =>
		await ExportAsync(
			"Export TME",
			"FSG TME",
			".tme",
			package => TmeStoreArchive.Build(ProjectSession.Current, package),
			"Unix Store TME written to",
			markExported: true);

	async void OnExportZip(object? sender, EventArgs e) =>
		await ExportAsync(
			"Export ZIP (FS4)",
			"FS4 ZIP",
			".zip",
			package => Fs4ZipArchive.Build(ProjectSession.Current, package),
			"Windows ZIP written to",
			markExported: true);

	async void OnExportKml(object? sender, EventArgs e) =>
		await ExportAsync(
			"Export KML",
			"KML",
			".kml",
			package => KmlWriter.Write(ProjectSession.Current, package),
			"KML written to",
			markExported: false);

	async Task ExportAsync(
		string title,
		string fileTypeName,
		string extension,
		Func<string, byte[]> build,
		string savedPrefix,
		bool markExported)
	{
		ApplyPackageName();
		if (!await ValidateForExportAsync(title))
			return;

		try
		{
			var location = await TmeFileSaver.SaveProjectArchiveAsync(
				build,
				ProjectSession.Current.PackageFolderName,
				fileTypeName,
				extension);
			if (location is null)
				return;

			RefreshPackageNameEntry();
			if (markExported)
				ProjectSession.MarkExported();
			var extra = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
				? $"\n\nUnpack into Aerofly FS 4:\naddons\\scenery\\{ProjectSession.Current.PackageFolderName}"
				: extension.Equals(".kml", StringComparison.OrdinalIgnoreCase)
					? "\n\nOpen in Google Earth to check helipad and pushback positions."
					: $"\n\nPackage folder: {ProjectSession.Current.PackageFolderName}";
			if (DeviceInfo.Current.Platform == DevicePlatform.Android)
				extra += "\n\nSaved in Downloads. A file with this name is replaced.";
			await DisplayAlert(title, $"{savedPrefix}:\n{location}{extra}", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert(title, ex.Message, "OK");
		}
	}

	void RefreshPackageNameEntry()
	{
		_pausePackageDirty = true;
		PackageNameEntry.Text = ProjectSession.Current.PackageFolderName;
		_pausePackageDirty = false;
	}

	async Task<bool> ValidateForExportAsync(string title)
	{
		foreach (var airport in ProjectSession.Current.Airports)
		{
			if (!ProjectValidator.IsIcao4(airport.Icao4))
			{
				await DisplayAlert(title, "Every airport needs a 4-character code.", "OK");
				return false;
			}

			var duplicate = ProjectValidator.DuplicateCode(ProjectSession.Current, airport);
			if (duplicate is not null)
			{
				await DisplayAlert(title, duplicate, "OK");
				return false;
			}

			if (airport.HelipadLatitude is null || airport.HelipadLongitude is null)
			{
				await DisplayAlert(
					title,
					$"{airport.TmeCode} needs a helipad position (also used as the dummy pushback airport position).",
					"OK");
				return false;
			}

			foreach (var slot in airport.ParkingSlots)
			{
				if (slot.Latitude is null || slot.Longitude is null)
				{
					await DisplayAlert(title, $"{airport.TmeCode} parking '{slot.Name}' needs a position.", "OK");
					return false;
				}
			}
		}

		return true;
	}
}
