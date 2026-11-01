using System.Collections.ObjectModel;
using System.Globalization;

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
	double? _towerLatitude;
	double? _towerLongitude;
	double _towerHeightAgl;

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

	public double? TowerLatitude
	{
		get => _towerLatitude;
		set
		{
			if (Set(ref _towerLatitude, value))
				Raise(nameof(ListSummary));
		}
	}

	public double? TowerLongitude
	{
		get => _towerLongitude;
		set
		{
			if (Set(ref _towerLongitude, value))
				Raise(nameof(ListSummary));
		}
	}

	/// <summary>Height above ground in metres. 0 is the default (no dummy .toc). Pushback airports only.</summary>
	public double TowerHeightAgl
	{
		get => _towerHeightAgl;
		set
		{
			if (Set(ref _towerHeightAgl, value))
				Raise(nameof(ListSummary));
		}
	}

	public bool HasTowerPosition => TowerLatitude is not null && TowerLongitude is not null;

	public bool WritesTowerDummy =>
		Kind != AirportKind.Heliport && TowerHeightAgl != 0;

	public ObservableCollection<ParkingSlot> ParkingSlots { get; } = [];

	public string KindLabel => Kind == AirportKind.Heliport ? "Heliport" : "Pushback airport";

	public string ListSummary
	{
		get
		{
			var title = string.IsNullOrWhiteSpace(Name) ? KindLabel : Name;
			var line = $"{TmeCode}  ·  {title}  ·  {ParkingSlots.Count} pushback slot(s)";
			if (Kind != AirportKind.Heliport && HasTowerPosition && TowerHeightAgl != 0)
			{
				var height = TowerHeightAgl.ToString("0.###", CultureInfo.InvariantCulture);
				line += $"  ·  Tower shift ({height}m)";
			}

			return line;
		}
	}

	public void NotifyListSummary() => Raise(nameof(ListSummary));
}
