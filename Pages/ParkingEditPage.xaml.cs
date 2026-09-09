using System.Globalization;
using FSG_PushbackCreator.Models;
using FSG_PushbackCreator.Services;

namespace FSG_PushbackCreator.Pages;

[QueryProperty(nameof(AirportId), "airportId")]
[QueryProperty(nameof(ParkingId), "parkingId")]
public partial class ParkingEditPage : ContentPage, IShellBackHandler
{
	AirportEntry? _airport;
	ParkingSlot? _slot;
	ParkingSnapshot? _snapshot;
	bool _leaving;

	public string AirportId { get; set; } = string.Empty;
	public string ParkingId { get; set; } = string.Empty;

	public bool AllowNavigateAway => _leaving;

	public Task HandleShellBackAsync() => OnCancelAsync();

	public ParkingEditPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		BindSlot();
	}

	protected override bool OnBackButtonPressed()
	{
		_ = OnCancelAsync();
		return true;
	}

	void BindSlot()
	{
		if (!Guid.TryParse(AirportId, out var airportId) || !Guid.TryParse(ParkingId, out var parkingId))
			return;

		_airport = ProjectSession.Current.FindAirport(airportId);
		_slot = _airport?.ParkingSlots.FirstOrDefault(p => p.Id == parkingId);
		if (_slot is null)
			return;

		_snapshot = ParkingSnapshot.Capture(_slot);
		NameEntry.Text = _slot.Name;
		PositionEntry.Text = GeoCoordinateParser.Format(_slot.Latitude, _slot.Longitude);
		HeadingEntry.Text = _slot.Heading.ToString("0.###", CultureInfo.InvariantCulture);
		SizeEntry.Text = _slot.Size.ToString("0.###", CultureInfo.InvariantCulture);
	}

	bool TryApplyFields(out string? error)
	{
		error = null;
		if (_slot is null)
		{
			error = "Parking slot is missing.";
			return false;
		}

		var name = (NameEntry.Text ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			error = "Parking name is required (for example A1).";
			return false;
		}

		var positionText = PositionEntry.Text;
		if (string.IsNullOrWhiteSpace(positionText) ||
		    !GeoCoordinateParser.TryParse(positionText, out var lat, out var lon))
		{
			error = "Parking position is required. Paste decimal (lat, lon) or Google Earth DMS, with a space or a comma between the two values.";
			return false;
		}

		PositionEntry.Text = GeoCoordinateParser.Format(lat, lon);

		if (!TryParseDouble(HeadingEntry.Text, out var heading))
		{
			error = "Heading must be a number.";
			return false;
		}

		if (!TryParseDouble(SizeEntry.Text, out var size) || size <= 0)
		{
			error = "Size must be a number greater than 0.";
			return false;
		}

		_slot.Name = name;
		_slot.Latitude = lat;
		_slot.Longitude = lon;
		_slot.Heading = heading;
		_slot.Size = size;
		_slot.NotifyListSummary();
		return true;
	}

	static bool TryParseDouble(string? text, out double value)
	{
		return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
		       double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
	}

	bool IsPageDirty()
	{
		if (_slot is null || _snapshot is null)
			return false;

		return !_snapshot.MatchesForm(NameEntry.Text, PositionEntry.Text, HeadingEntry.Text, SizeEntry.Text);
	}

	async void OnSave(object? sender, EventArgs e)
	{
		if (!TryApplyFields(out var error))
		{
			await DisplayAlert("Parking slot", error, "OK");
			return;
		}

		if (_slot is null)
			return;

		var wasNew = _slot.IsNew;
		_slot.IsNew = false;
		if (wasNew || _snapshot is null || !_snapshot.MatchesSlot(_slot))
			ProjectSession.MarkDirty();

		_airport?.NotifyListSummary();
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

		if (_airport is not null && _slot is not null)
		{
			if (_slot.IsNew)
				_airport.ParkingSlots.Remove(_slot);
			else
				_snapshot?.Restore(_slot);
		}

		_leaving = true;
		await Shell.Current.GoToAsync("..");
	}

	sealed record ParkingSnapshot(
		string Name,
		double? Latitude,
		double? Longitude,
		double Heading,
		double Size)
	{
		public static ParkingSnapshot Capture(ParkingSlot slot) =>
			new(slot.Name, slot.Latitude, slot.Longitude, slot.Heading, slot.Size);

		public void Restore(ParkingSlot slot)
		{
			slot.Name = Name;
			slot.Latitude = Latitude;
			slot.Longitude = Longitude;
			slot.Heading = Heading;
			slot.Size = Size;
			slot.NotifyListSummary();
		}

		public bool MatchesSlot(ParkingSlot slot) =>
			Name == slot.Name &&
			Latitude == slot.Latitude &&
			Longitude == slot.Longitude &&
			Heading == slot.Heading &&
			Size == slot.Size;

		public bool MatchesForm(string? name, string? position, string? headingText, string? sizeText)
		{
			var nameNorm = (name ?? string.Empty).Trim();
			if (Name != nameNorm)
				return false;

			if (string.IsNullOrWhiteSpace(position))
			{
				if (Latitude is not null || Longitude is not null)
					return false;
			}
			else if (!GeoCoordinateParser.TryParse(position, out var lat, out var lon) ||
			         lat != Latitude || lon != Longitude)
				return false;

			if (!double.TryParse(headingText, NumberStyles.Float, CultureInfo.InvariantCulture, out var heading) &&
			    !double.TryParse(headingText, NumberStyles.Float, CultureInfo.CurrentCulture, out heading))
				return false;
			if (!double.TryParse(sizeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) &&
			    !double.TryParse(sizeText, NumberStyles.Float, CultureInfo.CurrentCulture, out size))
				return false;

			return heading == Heading && size == Size;
		}
	}
}
