namespace FSG_PushbackCreator.Models;

public sealed class ParkingSlot : ObservableEntity
{
	string _name = "A1";
	double? _latitude;
	double? _longitude;
	double _heading;
	double _size = 35;

	public Guid Id { get; } = Guid.NewGuid();

	public bool IsNew { get; set; }

	public string Name
	{
		get => _name;
		set => Set(ref _name, value);
	}

	public double? Latitude
	{
		get => _latitude;
		set => Set(ref _latitude, value);
	}

	public double? Longitude
	{
		get => _longitude;
		set => Set(ref _longitude, value);
	}

	public double Heading
	{
		get => _heading;
		set => Set(ref _heading, value);
	}

	public double Size
	{
		get => _size;
		set => Set(ref _size, value);
	}

	public string ListSummary
	{
		get
		{
			var pos = Latitude is { } lat && Longitude is { } lon
				? $"{lat:0.######}, {lon:0.######}"
				: "no position";
			return $"{Name}  ·  {pos}  ·  hdg {Heading:0}  ·  size {Size:0}";
		}
	}

	public void NotifyListSummary() => Raise(nameof(ListSummary));
}
