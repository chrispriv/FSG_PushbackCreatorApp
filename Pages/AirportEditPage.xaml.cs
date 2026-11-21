using System.Globalization;
using System.Text.Json;
using FSG_PushbackCreator.Models;
using FSG_PushbackCreator.Services;

namespace FSG_PushbackCreator.Pages;

[QueryProperty(nameof(AirportId), "airportId")]
public partial class AirportEditPage : ContentPage, IShellBackHandler
{
	AirportEntry? _airport;
	AirportSnapshot? _snapshot;
	bool _leaving;
	bool _mapReady;
	bool _wideLayout = true;
	bool _suppressObjectSelection;
	bool _searchBusy;
	bool _highlightOnMap = true;
	string _selectedId = MapSceneBuilder.HelipadId;
	Guid? _selectedParkingId;

	public string AirportId { get; set; } = string.Empty;

	public bool AllowNavigateAway => _leaving;

	public Task HandleShellBackAsync() => OnCancelAsync();

	public AirportEditPage()
	{
		InitializeComponent();
		SizeChanged += (_, _) =>
		{
			if (Width > 0)
				ApplyLayout(Width >= 720);
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		BindAirport();
		_ = PushSceneAsync(fit: true);
	}

	protected override bool OnBackButtonPressed()
	{
		_ = OnCancelAsync();
		return true;
	}

	void ApplyLayout(bool wide)
	{
		if (wide == _wideLayout && MainGrid.ColumnDefinitions.Count > 0)
			return;
		_wideLayout = wide;
		MainGrid.ColumnDefinitions.Clear();
		MainGrid.RowDefinitions.Clear();
		if (wide)
		{
			MainGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(380)));
			MainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			MainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			Grid.SetColumn(FormPanel, 0);
			Grid.SetRow(FormPanel, 0);
			Grid.SetColumnSpan(FormPanel, 1);
			Grid.SetRowSpan(FormPanel, 1);
			Grid.SetColumn(MapPanel, 1);
			Grid.SetRow(MapPanel, 0);
			Grid.SetColumnSpan(MapPanel, 1);
			Grid.SetRowSpan(MapPanel, 1);
		}
		else
		{
			MainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			MainGrid.RowDefinitions.Add(new RowDefinition(new GridLength(0.9, GridUnitType.Star)));
			MainGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1.1, GridUnitType.Star)));
			Grid.SetColumn(FormPanel, 0);
			Grid.SetRow(FormPanel, 0);
			Grid.SetColumnSpan(FormPanel, 1);
			Grid.SetRowSpan(FormPanel, 1);
			Grid.SetColumn(MapPanel, 0);
			Grid.SetRow(MapPanel, 1);
			Grid.SetColumnSpan(MapPanel, 1);
			Grid.SetRowSpan(MapPanel, 1);
		}

		var stackIdentity = !wide || Width < 520;
		if (stackIdentity)
			SetStacked(IdentityGrid, IcaoBlock, NameBlock);
		else
			SetTwoColumn(IdentityGrid, IcaoBlock, NameBlock, 1, 1.25);
	}

	static void SetStacked(Grid grid, View first, View second)
	{
		grid.ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star) };
		grid.RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) };
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
		grid.RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto) };
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
		_selectedId = MapSceneBuilder.HelipadId;
		_selectedParkingId = null;
		KindLabel.Text = _airport.Kind == AirportKind.Heliport
			? "Heliport (4-character code)"
			: "Dummy pushback airport (ICAO + 00)";
		var isHeliport = _airport.Kind == AirportKind.Heliport;
		HeliportObjectsSection.IsVisible = isHeliport;
		NameCaptionLabel.Text = _airport.Kind == AirportKind.Heliport
			? "Heliport name (shown in map)"
			: "Airport name (shown in map)";
		IcaoEntry.Text = _airport.Icao4;
		UpdateTmeCodeLabel();
		NameEntry.Text = _airport.Name;
		HelipadPositionEntry.Text = GeoCoordinateParser.Format(_airport.HelipadLatitude, _airport.HelipadLongitude);
		HelipadHeadingEntry.Text = _airport.HelipadHeading.ToString("0.###", CultureInfo.InvariantCulture);
		var isPushback = _airport.Kind != AirportKind.Heliport;
		TowerPositionEntry.Text = isPushback
			? GeoCoordinateParser.Format(_airport.TowerLatitude, _airport.TowerLongitude)
			: string.Empty;
		TowerHeightEntry.Text = isPushback
			? _airport.TowerHeightAgl.ToString("0.###", CultureInfo.InvariantCulture)
			: "0";
		WindsockPositionEntry.Text = isPushback
			? string.Empty
			: GeoCoordinateParser.Format(_airport.WindsockLatitude, _airport.WindsockLongitude);
		DummyHelipadCheck.IsChecked = !isPushback && _airport.DummyHelipadEnabled;
		WindsockHeightEntry.Text = isPushback
			? "0"
			: _airport.WindsockHeightOffset.ToString("0.###", CultureInfo.InvariantCulture);
		HelipadHeightOffsetEntry.Text = isPushback
			? "0"
			: _airport.HelipadHeightOffset.ToString("0.###", CultureInfo.InvariantCulture);
		RefreshObjectList();
		ShowInspector();
		UpdateLocationLock();
	}

	bool HasAirportLocation() =>
		_airport?.HelipadLatitude is not null && _airport.HelipadLongitude is not null;

	void UpdateLocationLock()
	{
		if (_airport is null)
			return;

		var located = HasAirportLocation();
		var isHeliport = _airport.Kind == AirportKind.Heliport;
		SearchPanel.IsVisible = !located;
		if (located)
		{
			SearchList.IsVisible = false;
			SearchList.SelectedItem = null;
		}
		else
			SearchStatusLabel.Text = "No helipad yet. Search for the airport, then tap the map to set the airport location.";

		ParkingToolbar.IsVisible = !isHeliport && located;
		NewParkingButton.IsEnabled = !isHeliport && located;
		SortParkingButton.IsEnabled = !isHeliport && located;
		ParkingHintLabel.Text = !located
			? "Place the helipad (airport location) first. Other objects stay unavailable until then."
			: isHeliport
				? "A 4-character heliport has a helipad only."
				: "Add pushback slots, then drag them on the map. Drag the diamond on the arrow to set heading.";
		MapHintLabel.Text = located
			? "Drag a footprint to move it, or the diamond to set heading. Click empty map to release. Tap empty map only places an object that has no position yet."
			: "Search OSM if needed, then tap the map to place the helipad (airport location).";
	}

	void RefreshObjectList()
	{
		if (_airport is null)
			return;
		if (!HasAirportLocation())
			_selectedId = MapSceneBuilder.HelipadId;

		var rows = new List<MapObjectRow>
		{
			new()
			{
				Id = MapSceneBuilder.HelipadId,
				Title = TitleFor("Helipad", _airport.HelipadLatitude is not null)
			}
		};
		if (HasAirportLocation() && _airport.Kind != AirportKind.Heliport)
		{
			rows.Add(new MapObjectRow
			{
				Id = MapSceneBuilder.TowerId,
				Title = TitleFor("Tower", _airport.HasTowerPosition)
			});
			foreach (var slot in _airport.ParkingSlots)
			{
				rows.Add(new MapObjectRow
				{
					Id = MapSceneBuilder.ParkingId(slot.Id),
					Title = TitleFor(slot.Name, slot.Latitude is not null)
				});
			}
		}
		else if (HasAirportLocation())
		{
			rows.Add(new MapObjectRow
			{
				Id = MapSceneBuilder.WindsockId,
				Title = TitleFor("Windsock", _airport.HasWindsock)
			});
		}

		_suppressObjectSelection = true;
		ObjectList.ItemsSource = rows;
		ObjectList.SelectedItem = rows.FirstOrDefault(r => r.Id == _selectedId) ?? rows[0];
		_selectedId = ObjectList.SelectedItem is MapObjectRow selected ? selected.Id : MapSceneBuilder.HelipadId;
		_suppressObjectSelection = false;
		BindSelectedParkingFields();
	}

	static string TitleFor(string name, bool placed) =>
		placed ? name : $"{name}  ·  not placed";

	void ShowInspector()
	{
		HelipadInspector.IsVisible = _selectedId == MapSceneBuilder.HelipadId;
		TowerInspector.IsVisible = _selectedId == MapSceneBuilder.TowerId;
		WindsockInspector.IsVisible = _selectedId == MapSceneBuilder.WindsockId;
		ParkingInspector.IsVisible = MapSceneBuilder.TryParseParkingId(_selectedId, out var parkingId);
		_selectedParkingId = ParkingInspector.IsVisible ? parkingId : null;
		BindSelectedParkingFields();
	}

	void BindSelectedParkingFields()
	{
		if (_airport is null || _selectedParkingId is not { } id)
			return;
		var slot = _airport.ParkingSlots.FirstOrDefault(s => s.Id == id);
		if (slot is null)
			return;
		ParkingNameEntry.Text = slot.Name;
		ParkingPositionEntry.Text = GeoCoordinateParser.Format(slot.Latitude, slot.Longitude);
		ParkingHeadingEntry.Text = slot.Heading.ToString("0.###", CultureInfo.InvariantCulture);
		ParkingSizeEntry.Text = slot.Size.ToString("0.###", CultureInfo.InvariantCulture);
	}

	void OnObjectSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (_suppressObjectSelection)
			return;
		if (ObjectList.SelectedItem is not MapObjectRow row)
			return;
		ApplyLooseFields();
		_highlightOnMap = true;
		_selectedId = row.Id;
		ShowInspector();
		_ = PushSceneAsync(fit: false);
	}

	void OnMapMessage(object? sender, HybridWebViewRawMessageReceivedEventArgs e)
	{
		var raw = e.Message;
		if (string.IsNullOrWhiteSpace(raw))
			return;
		if (raw.StartsWith("__RawMessage|", StringComparison.Ordinal))
			raw = raw["__RawMessage|".Length..];

		MapJsMessage? msg;
		try
		{
			msg = JsonSerializer.Deserialize<MapJsMessage>(raw, MapSceneBuilder.JsonOptions);
		}
		catch (JsonException)
		{
			return;
		}

		if (msg?.Type is null)
			return;

		MainThread.BeginInvokeOnMainThread(() => HandleMapMessage(msg));
	}

	void HandleMapMessage(MapJsMessage msg)
	{
		switch (msg.Type)
		{
			case "ready":
				_mapReady = true;
				_ = PushSceneAsync(fit: true);
				break;
			case "select":
				if (!string.IsNullOrEmpty(msg.Id) && HasAirportLocation())
				{
					_highlightOnMap = true;
					_selectedId = msg.Id;
					RefreshObjectList();
					ShowInspector();
				}
				break;
			case "moved":
			case "mapclick":
				ApplyMapCoordinates(msg.Id, msg.Lat, msg.Lon, msg.Type == "mapclick");
				break;
			case "heading":
				ApplyMapHeading(msg.Id, msg.Heading);
				break;
			case "deselect":
				_highlightOnMap = false;
				break;
		}
	}

	bool TargetHasPosition(string target)
	{
		if (_airport is null)
			return false;
		if (target == MapSceneBuilder.HelipadId)
			return _airport.HelipadLatitude is not null && _airport.HelipadLongitude is not null;
		if (target == MapSceneBuilder.TowerId)
			return _airport.HasTowerPosition;
		if (target == MapSceneBuilder.WindsockId)
			return _airport.HasWindsock;
		if (MapSceneBuilder.TryParseParkingId(target, out var parkingId))
		{
			var slot = _airport.ParkingSlots.FirstOrDefault(s => s.Id == parkingId);
			return slot?.Latitude is not null && slot.Longitude is not null;
		}

		return false;
	}

	void ApplyMapCoordinates(string? id, double lat, double lon, bool fromClick)
	{
		if (_airport is null)
			return;
		var target = fromClick ? (string.IsNullOrEmpty(id) ? _selectedId : id) : id;
		if (string.IsNullOrEmpty(target))
			target = _selectedId;
		if (!HasAirportLocation())
			target = MapSceneBuilder.HelipadId;
		if (fromClick && TargetHasPosition(target))
			return;
		_highlightOnMap = true;

		if (target == MapSceneBuilder.HelipadId)
		{
			_airport.HelipadLatitude = lat;
			_airport.HelipadLongitude = lon;
			HelipadPositionEntry.Text = GeoCoordinateParser.Format(lat, lon);
		}
		else if (target == MapSceneBuilder.TowerId)
		{
			_airport.TowerLatitude = lat;
			_airport.TowerLongitude = lon;
			TowerPositionEntry.Text = GeoCoordinateParser.Format(lat, lon);
		}
		else if (target == MapSceneBuilder.WindsockId)
		{
			_airport.WindsockLatitude = lat;
			_airport.WindsockLongitude = lon;
			WindsockPositionEntry.Text = GeoCoordinateParser.Format(lat, lon);
		}
		else if (MapSceneBuilder.TryParseParkingId(target, out var parkingId))
		{
			var slot = _airport.ParkingSlots.FirstOrDefault(s => s.Id == parkingId);
			if (slot is null)
				return;
			slot.Latitude = lat;
			slot.Longitude = lon;
			slot.NotifyListSummary();
			if (_selectedParkingId == parkingId)
				ParkingPositionEntry.Text = GeoCoordinateParser.Format(lat, lon);
		}
		else
			return;

		_airport.NotifyListSummary();
		UpdateLocationLock();
		RefreshObjectList();
		_ = PushSceneAsync(fit: false);
	}

	void ApplyMapHeading(string? id, double heading)
	{
		if (_airport is null)
			return;
		heading = (heading % 360 + 360) % 360;
		var target = string.IsNullOrEmpty(id) ? _selectedId : id;
		if (target == MapSceneBuilder.HelipadId)
		{
			_airport.HelipadHeading = heading;
			HelipadHeadingEntry.Text = heading.ToString("0.###", CultureInfo.InvariantCulture);
		}
		else if (MapSceneBuilder.TryParseParkingId(target, out var parkingId))
		{
			var slot = _airport.ParkingSlots.FirstOrDefault(s => s.Id == parkingId);
			if (slot is null)
				return;
			slot.Heading = heading;
			slot.NotifyListSummary();
			if (_selectedParkingId == parkingId)
				ParkingHeadingEntry.Text = heading.ToString("0.###", CultureInfo.InvariantCulture);
		}
		else
			return;

		_airport.NotifyListSummary();
		_ = PushSceneAsync(fit: false);
	}

	async void OnSearchClicked(object? sender, EventArgs e) => await SearchOsmAsync();

	async void OnSearchCompleted(object? sender, EventArgs e) => await SearchOsmAsync();

	async void OnSearchSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (SearchList.SelectedItem is not NominatimHit hit)
			return;
		if (!hit.TryCoordinates(out var lat, out var lon))
			return;
		await PushSceneAsync(fit: false, new MapViewState { Lat = lat, Lon = lon, Zoom = 16 });
		SearchStatusLabel.Text = "Map moved. Tap to place the selected object.";
	}

	async Task SearchOsmAsync()
	{
		if (_searchBusy)
			return;
		var query = SearchEntry.Text;
		if (string.IsNullOrWhiteSpace(query))
		{
			SearchStatusLabel.Text = "Enter an airport name or ICAO code.";
			return;
		}

		_searchBusy = true;
		SearchStatusLabel.Text = "Searching OpenStreetMap…";
		try
		{
			var hits = await NominatimClient.SearchAsync(query);
			SearchList.ItemsSource = hits;
			SearchList.IsVisible = hits.Count > 0;
			SearchStatusLabel.Text = hits.Count == 0
				? "No OSM results. Try the ICAO code or a nearby city."
				: $"{hits.Count} result(s). Pick one to jump the map.";
		}
		catch (Exception ex)
		{
			SearchList.IsVisible = false;
			SearchStatusLabel.Text = "OSM search failed. Check the internet connection.";
			System.Diagnostics.Debug.WriteLine(ex);
		}
		finally
		{
			_searchBusy = false;
		}
	}

	async void OnZoomIn(object? sender, EventArgs e) => await EvalMapAsync("zoomBy(1)");

	async void OnZoomOut(object? sender, EventArgs e) => await EvalMapAsync("zoomBy(-1)");

	async void OnFitMap(object? sender, EventArgs e) => await EvalMapAsync("fitObjects()");

	async Task PushSceneAsync(bool fit, MapViewState? view = null)
	{
		if (!_mapReady || _airport is null)
			return;
		ApplyLooseFields();
		var json = MapSceneBuilder.ToJson(MapSceneBuilder.FromAirport(
			_airport, _highlightOnMap ? _selectedId : null, fit, view));
		await EvalMapAsync($"applyScene({json})");
	}

	async Task EvalMapAsync(string script)
	{
		if (!_mapReady)
			return;
		try
		{
			await MapView.EvaluateJavaScriptAsync(script);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine(ex);
		}
	}

	void OnHelipadFieldsChanged(object? sender, EventArgs e)
	{
		ApplyLooseFields();
		UpdateLocationLock();
		RefreshObjectList();
		_ = PushSceneAsync(fit: false);
	}

	void OnTowerFieldsChanged(object? sender, EventArgs e)
	{
		ApplyLooseFields();
		_ = PushSceneAsync(fit: false);
	}

	void OnWindsockFieldsChanged(object? sender, EventArgs e)
	{
		ApplyLooseFields();
		_ = PushSceneAsync(fit: false);
	}

	void OnParkingFieldsChanged(object? sender, EventArgs e)
	{
		ApplyLooseParking();
		RefreshObjectList();
		_ = PushSceneAsync(fit: false);
	}

	void ApplyLooseFields()
	{
		if (_airport is null)
			return;
		if (GeoCoordinateParser.TryParse(HelipadPositionEntry.Text, out var hLat, out var hLon))
		{
			_airport.HelipadLatitude = hLat;
			_airport.HelipadLongitude = hLon;
		}

		if (TryParseDouble(HelipadHeadingEntry.Text, out var heading))
			_airport.HelipadHeading = heading;

		if (string.IsNullOrWhiteSpace(TowerPositionEntry.Text))
		{
			_airport.TowerLatitude = null;
			_airport.TowerLongitude = null;
		}
		else if (GeoCoordinateParser.TryParse(TowerPositionEntry.Text, out var tLat, out var tLon))
		{
			_airport.TowerLatitude = tLat;
			_airport.TowerLongitude = tLon;
		}

		if (string.IsNullOrWhiteSpace(WindsockPositionEntry.Text))
		{
			_airport.WindsockLatitude = null;
			_airport.WindsockLongitude = null;
		}
		else if (GeoCoordinateParser.TryParse(WindsockPositionEntry.Text, out var wLat, out var wLon))
		{
			_airport.WindsockLatitude = wLat;
			_airport.WindsockLongitude = wLon;
		}

		ApplyLooseParking();
	}

	void ApplyLooseParking()
	{
		if (_airport is null || _selectedParkingId is not { } id)
			return;
		var slot = _airport.ParkingSlots.FirstOrDefault(s => s.Id == id);
		if (slot is null)
			return;
		var name = (ParkingNameEntry.Text ?? string.Empty).Trim();
		if (!string.IsNullOrWhiteSpace(name))
			slot.Name = name;
		if (string.IsNullOrWhiteSpace(ParkingPositionEntry.Text))
		{
			slot.Latitude = null;
			slot.Longitude = null;
		}
		else if (GeoCoordinateParser.TryParse(ParkingPositionEntry.Text, out var lat, out var lon))
		{
			slot.Latitude = lat;
			slot.Longitude = lon;
		}

		if (TryParseDouble(ParkingHeadingEntry.Text, out var heading))
			slot.Heading = heading;
		if (TryParseDouble(ParkingSizeEntry.Text, out var size) && size > 0)
			slot.Size = size;
		slot.NotifyListSummary();
	}

	bool TryApplyFields(out string? error)
	{
		error = null;
		if (_airport is null)
		{
			error = "Airport is missing.";
			return false;
		}

		ApplyLooseFields();

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
			error = "Helipad position is required. Search OSM or tap the map, or paste decimal lat, lon.";
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
					error = "Tower position is optional. Tap the map, paste decimal lat, lon, or leave the field empty.";
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

			foreach (var slot in _airport.ParkingSlots)
			{
				if (string.IsNullOrWhiteSpace(slot.Name))
				{
					error = "Each parking slot needs a name (for example A1).";
					return false;
				}

				if (slot.Latitude is not { } sLat || slot.Longitude is not { } sLon)
				{
					error = $"Parking '{slot.Name}' needs a position. Select it and tap the map.";
					return false;
				}

				error = ProjectValidator.ParkingMatchesTower(_airport, sLat, sLon);
				if (error is not null)
					return false;
				if (slot.Size <= 0)
				{
					error = $"Parking '{slot.Name}' size must be greater than 0.";
					return false;
				}
			}
		}

		double? windsockLat = null;
		double? windsockLon = null;
		var dummyHelipad = false;
		var windsockOffset = 0d;
		var helipadOffset = 0d;
		if (_airport.Kind == AirportKind.Heliport)
		{
			if (!TryParseOptionalPosition(
				    WindsockPositionEntry,
				    "Windsock is optional. Tap the map, paste decimal lat, lon, or leave the field empty.",
				    out windsockLat,
				    out windsockLon,
				    out error))
				return false;
			if (!TryParseOptionalOffset(WindsockHeightEntry.Text, "Windsock height offset must be a number (0 = default).", out windsockOffset, out error))
				return false;
			dummyHelipad = DummyHelipadCheck.IsChecked;
			if (!TryParseOptionalOffset(HelipadHeightOffsetEntry.Text, "Helipad height offset must be a number (0 = default).", out helipadOffset, out error))
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
		_airport.WindsockLatitude = windsockLat;
		_airport.WindsockLongitude = windsockLon;
		_airport.DummyHelipadEnabled = dummyHelipad;
		_airport.WindsockHeightOffset = windsockOffset;
		_airport.HelipadHeightOffset = helipadOffset;
		UpdateTmeCodeLabel();
		return true;
	}

	static bool TryParseOptionalPosition(
		Entry entry,
		string invalidMessage,
		out double? latitude,
		out double? longitude,
		out string? error)
	{
		latitude = null;
		longitude = null;
		error = null;
		var text = entry.Text;
		if (string.IsNullOrWhiteSpace(text))
			return true;
		if (!GeoCoordinateParser.TryParse(text, out var lat, out var lon))
		{
			error = invalidMessage;
			return false;
		}

		latitude = lat;
		longitude = lon;
		entry.Text = GeoCoordinateParser.Format(lat, lon);
		return true;
	}

	static bool TryParseOptionalOffset(string? text, string invalidMessage, out double value, out string? error)
	{
		value = 0;
		error = null;
		if (string.IsNullOrWhiteSpace(text))
			return true;
		if (TryParseDouble(text, out value))
			return true;
		error = invalidMessage;
		return false;
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
		ApplyLooseFields();
		return !_snapshot.MatchesForm(
			       IcaoEntry.Text,
			       NameEntry.Text,
			       HelipadPositionEntry.Text,
			       HelipadHeadingEntry.Text,
			       TowerPositionEntry.Text,
			       TowerHeightEntry.Text,
			       WindsockPositionEntry.Text,
			       WindsockHeightEntry.Text,
			       DummyHelipadCheck.IsChecked,
			       HelipadHeightOffsetEntry.Text) ||
		       !_snapshot.MatchesParking(_airport);
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
		TmeCodeLabel.Text = string.IsNullOrEmpty(code) ? "TME —" : code;
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
		foreach (var slot in _airport.ParkingSlots)
			slot.IsNew = false;
		if (wasNew || _snapshot is null || !_snapshot.MatchesAirport(_airport) || !_snapshot.MatchesParking(_airport))
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

	async void OnSortParking(object? sender, EventArgs e)
	{
		if (_airport is null)
			return;
		if (_airport.Kind == AirportKind.Heliport)
			return;

		ApplyLooseFields();
		var slots = _airport.ParkingSlots;
		if (slots.Count == 0)
		{
			await DisplayAlert("Sort A–Z", "No parking slots to sort.", "OK");
			return;
		}

		var ordered = slots
			.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(s => s.Id)
			.ToList();
		if (slots.Select(s => s.Id).SequenceEqual(ordered.Select(s => s.Id)))
		{
			await DisplayAlert("Sort A–Z", "Parking slots are already in name order.", "OK");
			return;
		}

		if (!await DisplayAlert(
			    "Sort A–Z",
			    "Sort parking slots A–Z by name? The list order is kept in this project and in the next TME/ZIP export.",
			    "Sort",
			    "Cancel"))
			return;

		slots.Clear();
		foreach (var slot in ordered)
			slots.Add(slot);

		_airport.NotifyListSummary();
		RefreshObjectList();
		_ = PushSceneAsync(fit: false);
	}

	async void OnNewParking(object? sender, EventArgs e)
	{
		if (_airport is null)
			return;
		if (_airport.Kind == AirportKind.Heliport)
			return;
		if (!HasAirportLocation())
		{
			await DisplayAlert("Airport", "Place the helipad (airport location) first.", "OK");
			return;
		}

		ApplyLooseFields();
		var slot = new ParkingSlot
		{
			IsNew = true,
			Name = $"A{_airport.ParkingSlots.Count + 1}"
		};
		_airport.ParkingSlots.Add(slot);
		_highlightOnMap = true;
		_selectedId = MapSceneBuilder.ParkingId(slot.Id);
		_airport.NotifyListSummary();
		RefreshObjectList();
		ShowInspector();
		_ = PushSceneAsync(fit: false);
	}

	async void OnDeleteSelectedParking(object? sender, EventArgs e)
	{
		if (_airport is null || _selectedParkingId is not { } id)
			return;
		var slot = _airport.ParkingSlots.FirstOrDefault(s => s.Id == id);
		if (slot is null)
			return;
		if (!await DisplayAlert("Delete parking", $"Remove {slot.Name}?", "Delete", "Cancel"))
			return;

		_airport.ParkingSlots.Remove(slot);
		_airport.NotifyListSummary();
		_selectedId = MapSceneBuilder.HelipadId;
		RefreshObjectList();
		ShowInspector();
		_ = PushSceneAsync(fit: false);
	}

	sealed record ParkingSnap(
		Guid Id,
		string Name,
		double? Latitude,
		double? Longitude,
		double Heading,
		double Size)
	{
		public static ParkingSnap Capture(ParkingSlot slot) =>
			new(slot.Id, slot.Name, slot.Latitude, slot.Longitude, slot.Heading, slot.Size);

		public void Restore(ParkingSlot slot)
		{
			slot.Name = Name;
			slot.Latitude = Latitude;
			slot.Longitude = Longitude;
			slot.Heading = Heading;
			slot.Size = Size;
			slot.IsNew = false;
			slot.NotifyListSummary();
		}

		public bool Matches(ParkingSlot slot) =>
			Id == slot.Id &&
			Name == slot.Name &&
			Latitude == slot.Latitude &&
			Longitude == slot.Longitude &&
			Heading == slot.Heading &&
			Size == slot.Size;
	}

	sealed record AirportSnapshot(
		string Icao4,
		string Name,
		double? HelipadLatitude,
		double? HelipadLongitude,
		double HelipadHeading,
		double? TowerLatitude,
		double? TowerLongitude,
		double TowerHeightAgl,
		double? WindsockLatitude,
		double? WindsockLongitude,
		double WindsockHeightOffset,
		bool DummyHelipadEnabled,
		double HelipadHeightOffset,
		ParkingSnap[] Parking)
	{
		public static AirportSnapshot Capture(AirportEntry airport) => new(
			airport.Icao4,
			airport.Name,
			airport.HelipadLatitude,
			airport.HelipadLongitude,
			airport.HelipadHeading,
			airport.TowerLatitude,
			airport.TowerLongitude,
			airport.TowerHeightAgl,
			airport.WindsockLatitude,
			airport.WindsockLongitude,
			airport.WindsockHeightOffset,
			airport.DummyHelipadEnabled,
			airport.HelipadHeightOffset,
			airport.ParkingSlots.Select(ParkingSnap.Capture).ToArray());

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
			airport.WindsockLatitude = WindsockLatitude;
			airport.WindsockLongitude = WindsockLongitude;
			airport.WindsockHeightOffset = WindsockHeightOffset;
			airport.DummyHelipadEnabled = DummyHelipadEnabled;
			airport.HelipadHeightOffset = HelipadHeightOffset;
			RestoreParking(airport);
		}

		public bool MatchesAirport(AirportEntry airport) =>
			Icao4 == airport.Icao4 &&
			Name == airport.Name &&
			HelipadLatitude == airport.HelipadLatitude &&
			HelipadLongitude == airport.HelipadLongitude &&
			HelipadHeading == airport.HelipadHeading &&
			TowerLatitude == airport.TowerLatitude &&
			TowerLongitude == airport.TowerLongitude &&
			TowerHeightAgl == airport.TowerHeightAgl &&
			WindsockLatitude == airport.WindsockLatitude &&
			WindsockLongitude == airport.WindsockLongitude &&
			WindsockHeightOffset == airport.WindsockHeightOffset &&
			DummyHelipadEnabled == airport.DummyHelipadEnabled &&
			HelipadHeightOffset == airport.HelipadHeightOffset;

		public bool MatchesParking(AirportEntry airport) =>
			Parking.Length == airport.ParkingSlots.Count &&
			Parking.Zip(airport.ParkingSlots, (snap, slot) => snap.Matches(slot)).All(x => x);

		public bool MatchesForm(
			string? icao, string? name, string? position, string? headingText,
			string? towerPosition, string? towerHeightText,
			string? windsockPosition, string? windsockHeightText,
			bool dummyHelipadChecked, string? helipadHeightText)
		{
			var icaoNorm = (icao ?? string.Empty).Trim().ToUpperInvariant();
			var nameNorm = ProjectValidator.ClampSname(name);
			if (Icao4 != icaoNorm || Name != nameNorm)
				return false;

			if (!SameOptionalPosition(position, HelipadLatitude, HelipadLongitude))
				return false;
			if (!SameOptionalPosition(towerPosition, TowerLatitude, TowerLongitude))
				return false;
			if (!SameOptionalPosition(windsockPosition, WindsockLatitude, WindsockLongitude))
				return false;
			if (!SameOptionalNumber(windsockHeightText, WindsockHeightOffset))
				return false;
			if (dummyHelipadChecked != DummyHelipadEnabled)
				return false;
			if (!SameOptionalNumber(helipadHeightText, HelipadHeightOffset))
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

		void RestoreParking(AirportEntry airport)
		{
			var remaining = airport.ParkingSlots.ToDictionary(s => s.Id);
			var next = new List<ParkingSlot>();
			foreach (var snap in Parking)
			{
				if (!remaining.Remove(snap.Id, out var slot))
					continue;
				snap.Restore(slot);
				next.Add(slot);
			}

			airport.ParkingSlots.Clear();
			foreach (var slot in next)
				airport.ParkingSlots.Add(slot);
		}

		static bool SameOptionalPosition(string? text, double? latitude, double? longitude)
		{
			if (string.IsNullOrWhiteSpace(text))
				return latitude is null && longitude is null;
			return GeoCoordinateParser.TryParse(text, out var parsedLat, out var parsedLon) &&
			       parsedLat == latitude && parsedLon == longitude;
		}

		static bool SameOptionalNumber(string? text, double expected)
		{
			if (string.IsNullOrWhiteSpace(text))
				return expected == 0;
			return (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
			        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) &&
			       value == expected;
		}
	}
}
