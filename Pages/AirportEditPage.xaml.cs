using System.Globalization;
using FSG_PushbackCreator.Models;
using FSG_PushbackCreator.Services;

namespace FSG_PushbackCreator.Pages;

[QueryProperty(nameof(AirportId), "airportId")]
public partial class AirportEditPage : ContentPage, IShellBackHandler
{
	AirportEntry? _airport;
	AirportSnapshot? _snapshot;
	bool _pauseRebind;
	bool _leaving;

	public string AirportId { get; set; } = string.Empty;

	public bool AllowNavigateAway => _leaving;

	public Task HandleShellBackAsync() => OnCancelAsync();

	public AirportEditPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (_pauseRebind)
		{
			_pauseRebind = false;
			RefreshParkingList();
			return;
		}

		BindAirport();
	}

	protected override bool OnBackButtonPressed()
	{
		_ = OnCancelAsync();
		return true;
	}

	void BindAirport()
	{
		if (!Guid.TryParse(AirportId, out var id))
			return;

		_airport = ProjectSession.Current.FindAirport(id);
		if (_airport is null)
			return;

		_snapshot = AirportSnapshot.Capture(_airport);
		KindLabel.Text = _airport.Kind == AirportKind.Heliport
			? "Heliport — TME code stays 4 characters. Parking slots are not used. The name appears on the Change Location map above the ICAO code."
			: "Dummy pushback airport — folder/file names are ICAO + 00. TSC/WAD [icao] uses ICAO plus two spaces. Helipad position is also the airport position.";
		NewParkingButton.IsEnabled = _airport.Kind != AirportKind.Heliport;
		ParkingHintLabel.Text = _airport.Kind == AirportKind.Heliport
			? "A 4-character heliport has a helipad only."
			: "A dummy pushback airport should add at least one pushback slot.";
		NameCaptionLabel.Text = _airport.Kind == AirportKind.Heliport
			? "Heliport name (shown in Location map, max. 32, optional)"
			: "Airport name [sname] (shown in Location map, max. 32, optional)";
		IcaoEntry.Text = _airport.Icao4;
		UpdateTmeCodeLabel();
		NameEntry.Text = _airport.Name;
		HelipadPositionEntry.Text = GeoCoordinateParser.Format(_airport.HelipadLatitude, _airport.HelipadLongitude);
		HelipadHeadingEntry.Text = _airport.HelipadHeading.ToString("0.###", CultureInfo.InvariantCulture);
		RefreshParkingList();
	}

	void RefreshParkingList()
	{
		if (_airport is null)
			return;

		foreach (var slot in _airport.ParkingSlots)
			slot.NotifyListSummary();
		ParkingList.ItemsSource = null;
		ParkingList.ItemsSource = _airport.ParkingSlots;
	}

	bool TryApplyFields(out string? error)
	{
		error = null;
		if (_airport is null)
		{
			error = "Airport is missing.";
			return false;
		}

		var icao = IcaoEntry.Text ?? string.Empty;
		if (!ProjectValidator.IsIcao4(icao))
		{
			error = "ICAO must be exactly 4 letters or digits.";
			return false;
		}

		var positionText = HelipadPositionEntry.Text;
		if (string.IsNullOrWhiteSpace(positionText) ||
		    !GeoCoordinateParser.TryParse(positionText, out var lat, out var lon))
		{
			error = "Helipad position is required. Paste decimal (lat, lon) or Google Earth DMS, with a space or a comma between the two values.";
			return false;
		}

		HelipadPositionEntry.Text = GeoCoordinateParser.Format(lat, lon);

		if (!TryParseDouble(HelipadHeadingEntry.Text, out var heading))
		{
			error = "Helipad heading must be a number.";
			return false;
		}

		var previousIcao = _airport.Icao4;
		_airport.Icao4 = icao.Trim().ToUpperInvariant();
		var duplicate = ProjectValidator.DuplicateCode(ProjectSession.Current, _airport);
		if (duplicate is not null)
		{
			_airport.Icao4 = previousIcao;
			error = duplicate;
			return false;
		}

		_airport.Name = ProjectValidator.ClampSname(NameEntry.Text);
		_airport.HelipadLatitude = lat;
		_airport.HelipadLongitude = lon;
		_airport.HelipadRadius = AirportEntry.DefaultHelipadRadius;
		_airport.HelipadHeading = heading;
		UpdateTmeCodeLabel();
		return true;
	}

	static bool TryParseDouble(string? text, out double value)
	{
		return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
		       double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
	}

	bool IsPageDirty()
	{
		if (_airport is null || _snapshot is null)
			return false;

		return !_snapshot.MatchesForm(
			IcaoEntry.Text,
			NameEntry.Text,
			HelipadPositionEntry.Text,
			HelipadHeadingEntry.Text);
	}

	void OnIcaoChanged(object? sender, TextChangedEventArgs e) => UpdateTmeCodeLabel();

	void UpdateTmeCodeLabel()
	{
		if (_airport is null)
			return;

		var icao = (IcaoEntry.Text ?? string.Empty).Trim().ToUpperInvariant();
		var code = _airport.Kind == AirportKind.Heliport
			? icao
			: string.IsNullOrEmpty(icao) ? string.Empty : icao + "00";
		TmeCodeLabel.Text = string.IsNullOrEmpty(code) ? "TME code: (enter ICAO)" : $"TME code: {code}";
	}

	async void OnSave(object? sender, EventArgs e)
	{
		if (!TryApplyFields(out var error))
		{
			await DisplayAlert("Airport", error, "OK");
			return;
		}

		if (_airport is null)
			return;

		var wasNew = _airport.IsNew;
		_airport.IsNew = false;
		if (wasNew || _snapshot is null || !_snapshot.MatchesAirport(_airport))
			ProjectSession.MarkDirty();

		_leaving = true;
		await Shell.Current.GoToAsync("..");
	}

	async void OnCancel(object? sender, EventArgs e) => await OnCancelAsync();

	async Task OnCancelAsync()
	{
		if (_leaving)
			return;

		if (IsPageDirty() && !await UnsavedChanges.ConfirmDiscardPageAsync(this))
			return;

		if (_airport is not null)
		{
			if (_airport.IsNew)
				ProjectSession.Current.Airports.Remove(_airport);
			else
				_snapshot?.Restore(_airport);
		}

		_leaving = true;
		await Shell.Current.GoToAsync("..");
	}

	async void OnNewParking(object? sender, EventArgs e)
	{
		if (!TryApplyFields(out var error))
		{
			await DisplayAlert("Airport", error, "OK");
			return;
		}

		if (_airport is null)
			return;
		if (_airport.Kind == AirportKind.Heliport)
			return;

		var slot = new ParkingSlot
		{
			IsNew = true,
			Name = $"A{_airport.ParkingSlots.Count + 1}"
		};
		_airport.ParkingSlots.Add(slot);
		_pauseRebind = true;
		await Shell.Current.GoToAsync($"parking?airportId={_airport.Id}&parkingId={slot.Id}");
	}

	async void OnEditParking(object? sender, EventArgs e)
	{
		if (!TryApplyFields(out var error))
		{
			await DisplayAlert("Airport", error, "OK");
			return;
		}

		if (_airport is null)
			return;
		if (sender is not Button { CommandParameter: ParkingSlot slot })
			return;

		slot.IsNew = false;
		_pauseRebind = true;
		await Shell.Current.GoToAsync($"parking?airportId={_airport.Id}&parkingId={slot.Id}");
	}

	async void OnDeleteParking(object? sender, EventArgs e)
	{
		if (_airport is null)
			return;
		if (sender is not Button { CommandParameter: ParkingSlot slot })
			return;

		if (!await DisplayAlert("Delete parking", $"Remove {slot.Name}?", "Delete", "Cancel"))
			return;

		_airport.ParkingSlots.Remove(slot);
		_airport.NotifyListSummary();
		ProjectSession.MarkDirty();
	}

	sealed record AirportSnapshot(
		string Icao4,
		string Name,
		double? HelipadLatitude,
		double? HelipadLongitude,
		double HelipadHeading)
	{
		public static AirportSnapshot Capture(AirportEntry airport) => new(
			airport.Icao4,
			airport.Name,
			airport.HelipadLatitude,
			airport.HelipadLongitude,
			airport.HelipadHeading);

		public void Restore(AirportEntry airport)
		{
			airport.Icao4 = Icao4;
			airport.Name = Name;
			airport.HelipadLatitude = HelipadLatitude;
			airport.HelipadLongitude = HelipadLongitude;
			airport.HelipadRadius = AirportEntry.DefaultHelipadRadius;
			airport.HelipadHeading = HelipadHeading;
		}

		public bool MatchesAirport(AirportEntry airport) =>
			Icao4 == airport.Icao4 &&
			Name == airport.Name &&
			HelipadLatitude == airport.HelipadLatitude &&
			HelipadLongitude == airport.HelipadLongitude &&
			HelipadHeading == airport.HelipadHeading;

		public bool MatchesForm(
			string? icao, string? name, string? position, string? headingText)
		{
			var icaoNorm = (icao ?? string.Empty).Trim().ToUpperInvariant();
			var nameNorm = ProjectValidator.ClampSname(name);
			if (Icao4 != icaoNorm || Name != nameNorm)
				return false;

			double? lat = HelipadLatitude;
			double? lon = HelipadLongitude;
			if (string.IsNullOrWhiteSpace(position))
			{
				if (lat is not null || lon is not null)
					return false;
			}
			else if (!GeoCoordinateParser.TryParse(position, out var parsedLat, out var parsedLon) ||
			         parsedLat != lat || parsedLon != lon)
				return false;

			if (!double.TryParse(headingText, NumberStyles.Float, CultureInfo.InvariantCulture, out var heading) &&
			    !double.TryParse(headingText, NumberStyles.Float, CultureInfo.CurrentCulture, out heading))
				return false;

			return heading == HelipadHeading;
		}
	}
}
