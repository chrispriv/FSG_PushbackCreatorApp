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
	int _fieldColumns = -1;

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

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		if (width <= 0)
			return;

		// Phone portrait (~360–430 dp) is too narrow for two field columns.
		var columns = width < 520 ? 1 : 2;
		if (columns == _fieldColumns)
			return;

		_fieldColumns = columns;
		ApplyFieldColumns(columns == 1);
	}

	void ApplyFieldColumns(bool stacked)
	{
		if (stacked)
		{
			SetStacked(IdentityGrid, IcaoBlock, NameBlock);
			SetStacked(HelipadGrid, HelipadPositionBlock, HelipadHeadingBlock);
			SetStacked(TowerGrid, TowerPositionBlock, TowerHeightBlock);
			return;
		}

		SetTwoColumn(IdentityGrid, IcaoBlock, NameBlock, starLeft: 1, starRight: 1.25);
		SetTwoColumn(HelipadGrid, HelipadPositionBlock, HelipadHeadingBlock, starLeft: 1.7, starRight: 1);
		SetTwoColumn(TowerGrid, TowerPositionBlock, TowerHeightBlock, starLeft: 1.7, starRight: 1);
	}

	static void SetStacked(Grid grid, View first, View second)
	{
		grid.ColumnDefinitions = new ColumnDefinitionCollection
		{
			new(GridLength.Star)
		};
		grid.RowDefinitions = new RowDefinitionCollection
		{
			new(GridLength.Auto),
			new(GridLength.Auto)
		};
		Grid.SetRow(first, 0);
		Grid.SetColumn(first, 0);
		Grid.SetColumnSpan(first, 1);
		Grid.SetRow(second, 1);
		Grid.SetColumn(second, 0);
		Grid.SetColumnSpan(second, 1);
	}

	static void SetTwoColumn(Grid grid, View first, View second, double starLeft, double starRight)
	{
		grid.ColumnDefinitions = new ColumnDefinitionCollection
		{
			new(new GridLength(starLeft, GridUnitType.Star)),
			new(new GridLength(starRight, GridUnitType.Star))
		};
		grid.RowDefinitions = new RowDefinitionCollection
		{
			new(GridLength.Auto)
		};
		Grid.SetRow(first, 0);
		Grid.SetColumn(first, 0);
		Grid.SetColumnSpan(first, 1);
		Grid.SetRow(second, 0);
		Grid.SetColumn(second, 1);
		Grid.SetColumnSpan(second, 1);
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
		var isPushback = _airport.Kind != AirportKind.Heliport;
		TowerGrid.IsVisible = isPushback;
		TowerPositionEntry.Text = isPushback
			? GeoCoordinateParser.Format(_airport.TowerLatitude, _airport.TowerLongitude)
			: string.Empty;
		TowerHeightEntry.Text = isPushback
			? _airport.TowerHeightAgl.ToString("0.###", CultureInfo.InvariantCulture)
			: "0";
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
			error = "Helipad position is required. Paste Google Earth decimal (lat, lon) for better results. A space or comma between the two values.";
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

		double? towerLat = null;
		double? towerLon = null;
		var towerHeight = 0d;
		if (_airport.Kind != AirportKind.Heliport)
		{
			var towerText = TowerPositionEntry.Text;
			if (!string.IsNullOrWhiteSpace(towerText))
			{
				if (!GeoCoordinateParser.TryParse(towerText, out var parsedTowerLat, out var parsedTowerLon))
				{
					error = "Tower position is optional. Paste Google Earth decimal (lat, lon), or leave the field empty.";
					return false;
				}

				towerLat = parsedTowerLat;
				towerLon = parsedTowerLon;
				TowerPositionEntry.Text = GeoCoordinateParser.Format(towerLat, towerLon);
			}

			var heightText = TowerHeightEntry.Text;
			if (string.IsNullOrWhiteSpace(heightText))
				towerHeight = 0;
			else if (!TryParseDouble(heightText, out towerHeight))
			{
				error = "Tower height AGL must be a number (0 = default).";
				return false;
			}

			error = ProjectValidator.TowerPlacement(
				_airport, towerLat, towerLon, towerHeight, lat, lon);
			if (error is not null)
				return false;
		}

		_airport.Name = ProjectValidator.ClampSname(NameEntry.Text);
		_airport.HelipadLatitude = lat;
		_airport.HelipadLongitude = lon;
		_airport.HelipadRadius = AirportEntry.DefaultHelipadRadius;
		_airport.HelipadHeading = heading;
		_airport.TowerLatitude = towerLat;
		_airport.TowerLongitude = towerLon;
		_airport.TowerHeightAgl = towerHeight;
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
			HelipadHeadingEntry.Text,
			TowerPositionEntry.Text,
			TowerHeightEntry.Text);
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
		double HelipadHeading,
		double? TowerLatitude,
		double? TowerLongitude,
		double TowerHeightAgl)
	{
		public static AirportSnapshot Capture(AirportEntry airport) => new(
			airport.Icao4,
			airport.Name,
			airport.HelipadLatitude,
			airport.HelipadLongitude,
			airport.HelipadHeading,
			airport.TowerLatitude,
			airport.TowerLongitude,
			airport.TowerHeightAgl);

		public void Restore(AirportEntry airport)
		{
			airport.Icao4 = Icao4;
			airport.Name = Name;
			airport.HelipadLatitude = HelipadLatitude;
			airport.HelipadLongitude = HelipadLongitude;
			airport.HelipadRadius = AirportEntry.DefaultHelipadRadius;
			airport.HelipadHeading = HelipadHeading;
			airport.TowerLatitude = TowerLatitude;
			airport.TowerLongitude = TowerLongitude;
			airport.TowerHeightAgl = TowerHeightAgl;
		}

		public bool MatchesAirport(AirportEntry airport) =>
			Icao4 == airport.Icao4 &&
			Name == airport.Name &&
			HelipadLatitude == airport.HelipadLatitude &&
			HelipadLongitude == airport.HelipadLongitude &&
			HelipadHeading == airport.HelipadHeading &&
			TowerLatitude == airport.TowerLatitude &&
			TowerLongitude == airport.TowerLongitude &&
			TowerHeightAgl == airport.TowerHeightAgl;

		public bool MatchesForm(
			string? icao, string? name, string? position, string? headingText,
			string? towerPosition, string? towerHeightText)
		{
			var icaoNorm = (icao ?? string.Empty).Trim().ToUpperInvariant();
			var nameNorm = ProjectValidator.ClampSname(name);
			if (Icao4 != icaoNorm || Name != nameNorm)
				return false;

			if (!SameOptionalPosition(position, HelipadLatitude, HelipadLongitude))
				return false;
			if (!SameOptionalPosition(towerPosition, TowerLatitude, TowerLongitude))
				return false;

			if (!double.TryParse(headingText, NumberStyles.Float, CultureInfo.InvariantCulture, out var heading) &&
			    !double.TryParse(headingText, NumberStyles.Float, CultureInfo.CurrentCulture, out heading))
				return false;
			if (heading != HelipadHeading)
				return false;

			if (string.IsNullOrWhiteSpace(towerHeightText))
				return TowerHeightAgl == 0;

			if (!double.TryParse(towerHeightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var towerHeight) &&
			    !double.TryParse(towerHeightText, NumberStyles.Float, CultureInfo.CurrentCulture, out towerHeight))
				return false;

			return towerHeight == TowerHeightAgl;
		}

		static bool SameOptionalPosition(string? text, double? latitude, double? longitude)
		{
			if (string.IsNullOrWhiteSpace(text))
				return latitude is null && longitude is null;
			return GeoCoordinateParser.TryParse(text, out var parsedLat, out var parsedLon) &&
			       parsedLat == latitude && parsedLon == longitude;
		}
	}
}
