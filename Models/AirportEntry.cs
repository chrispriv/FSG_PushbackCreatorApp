using System.Collections.ObjectModel;

namespace FSG_PushbackCreator.Models;

public sealed class AirportEntry : ObservableEntity
{
	AirportKind _kind = AirportKind.DummyPushback;
	string _icao4 = string.Empty;
	string _name = string.Empty;
	double? _helipadLatitude;
	double? _helipadLongitude;
	double _helipadRadius = DefaultHelipadRadius;
	double _helipadHeading;

	public const double DefaultHelipadRadius = 20;

	public Guid Id { get; } = Guid.NewGuid();

	public bool IsNew { get; set; }

	public AirportKind Kind
	{
		get => _kind;
		set
		{
			if (Set(ref _kind, value))
			{
				Raise(nameof(TmeCode));
				Raise(nameof(FileIcao));
				Raise(nameof(KindLabel));
				Raise(nameof(ListSummary));
			}
		}
	}

	public string Icao4
	{
		get => _icao4;
		set
		{
			var next = (value ?? string.Empty).Trim().ToUpperInvariant();
			if (Set(ref _icao4, next))
			{
				Raise(nameof(TmeCode));
				Raise(nameof(FileIcao));
				Raise(nameof(ListSummary));
			}
		}
	}

	/// <summary>4 characters for a heliport, 6 characters (ICAO + 00) for folder and file names of a dummy pushback airport.</summary>
	public string TmeCode => Kind == AirportKind.Heliport
		? Icao4
		: string.IsNullOrEmpty(Icao4) ? string.Empty : Icao4 + "00";

	/// <summary>ICAO written into TSC/WAD. Pushback airports use two trailing spaces instead of 00.</summary>
	public string FileIcao => Kind == AirportKind.Heliport || string.IsNullOrEmpty(Icao4)
		? Icao4
		: Icao4 + "  ";

	public string FolderName => TmeCode.ToLowerInvariant();

	public string Name
	{
		get => _name;
		set
		{
			if (Set(ref _name, value ?? string.Empty))
				Raise(nameof(ListSummary));
		}
	}

	public double? HelipadLatitude
	{
		get => _helipadLatitude;
		set => Set(ref _helipadLatitude, value);
	}

	public double? HelipadLongitude
	{
		get => _helipadLongitude;
		set => Set(ref _helipadLongitude, value);
	}

	public double HelipadRadius
	{
		get => _helipadRadius;
		set => Set(ref _helipadRadius, value);
	}

	public double HelipadHeading
	{
		get => _helipadHeading;
		set => Set(ref _helipadHeading, value);
	}

	public ObservableCollection<ParkingSlot> ParkingSlots { get; } = [];

	public string KindLabel => Kind == AirportKind.Heliport ? "Heliport" : "Pushback airport";

	public string ListSummary
	{
		get
		{
			var title = string.IsNullOrWhiteSpace(Name) ? KindLabel : Name;
			return $"{TmeCode}  ·  {title}  ·  {ParkingSlots.Count} pushback slot(s)";
		}
	}

	public void NotifyListSummary() => Raise(nameof(ListSummary));
}
