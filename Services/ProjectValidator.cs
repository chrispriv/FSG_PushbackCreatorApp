using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class ProjectValidator
{
	public static bool IsIcao4(string? code) =>
		!string.IsNullOrWhiteSpace(code) && code.Trim().Length == 4 && code.Trim().All(char.IsLetterOrDigit);

	public const int SnameMaxLength = 32;

	public static string ClampSname(string? name)
	{
		var trimmed = (name ?? string.Empty).Trim();
		return trimmed.Length <= SnameMaxLength ? trimmed : trimmed[..SnameMaxLength];
	}

	public static string? DuplicateCode(TmeProject project, AirportEntry airport)
	{
		var code = airport.TmeCode;
		if (string.IsNullOrEmpty(code))
			return "Enter a 4-character airport code.";

		if (project.Airports.Any(a => a.Id != airport.Id &&
		                              string.Equals(a.TmeCode, code, StringComparison.OrdinalIgnoreCase)))
			return $"Code {code} is already in this project.";

		return null;
	}

	public static string? TowerPlacement(
		AirportEntry airport,
		double? towerLatitude,
		double? towerLongitude,
		double towerHeightAgl,
		double helipadLatitude,
		double helipadLongitude,
		ParkingSlot? ignoreSlot = null)
	{
		if (airport.Kind == AirportKind.Heliport)
			return null;

		var hasTower = towerLatitude is not null && towerLongitude is not null;
		if (!hasTower)
		{
			if (towerHeightAgl != 0)
				return "Tower height needs a tower position. Leave height at 0 for the default.";
			return null;
		}

		if (towerHeightAgl < 0)
			return "Tower height AGL must be 0 or greater.";

		var tLat = towerLatitude!.Value;
		var tLon = towerLongitude!.Value;
		if (GeoCoordinateParser.SamePosition(tLat, tLon, helipadLatitude, helipadLongitude))
			return "Tower position must not match the helipad (no overlay).";

		foreach (var slot in airport.ParkingSlots)
		{
			if (ignoreSlot is not null && slot.Id == ignoreSlot.Id)
				continue;
			if (slot.Latitude is not { } sLat || slot.Longitude is not { } sLon)
				continue;
			if (GeoCoordinateParser.SamePosition(tLat, tLon, sLat, sLon))
				return $"Tower position must not match parking '{slot.Name}' (no overlay).";
		}

		return null;
	}

	public static string? ParkingMatchesTower(AirportEntry airport, double latitude, double longitude)
	{
		if (airport.Kind == AirportKind.Heliport || !airport.HasTowerPosition)
			return null;
		if (GeoCoordinateParser.SamePosition(
			    latitude, longitude, airport.TowerLatitude!.Value, airport.TowerLongitude!.Value))
			return "This parking position matches the tower (no overlay). Choose another point.";
		return null;
	}
}
