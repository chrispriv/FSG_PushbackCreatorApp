using System.Globalization;
using System.Text;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class WadWriter
{
	public static string Write(AirportEntry airport)
	{
		if (airport.HelipadLatitude is not { } lat || airport.HelipadLongitude is not { } lon)
			throw new InvalidOperationException($"{airport.TmeCode} needs a helipad position.");

		var icaoField = airport.FileIcao.ToUpperInvariant();
		var world = FormatWorld(lon, lat);
		var sb = new StringBuilder();
		Line(sb, "<[file][][]");
		Line(sb, "    <[tmworld_airport_detailed][][]");
		Line(sb, "        <[uint64][uid][0]>");
		Line(sb, $"        <[stringt8c][icao][{icaoField}]>");
		Line(sb, $"        <[vector2_float64][position][{world}]>");
		Line(sb, "");
		Line(sb, "        <[list_tmworld_airport_detailed_helipad][helipads][]");
		Line(sb, "            <[tmworld_airport_detailed_helipad][element][0]");
		Line(sb, "                <[string8][name][H01]>");
		Line(sb, $"                <[vector2_float64][position][{world}]>");
		Line(sb, $"                <[float64][radius][{FormatNumber(AirportEntry.DefaultHelipadRadius)}]>");
		Line(sb, $"                <[float64][direction][{FormatDirection(airport.HelipadHeading)}]>");
		Line(sb, "            >");
		Line(sb, "        >");
		Line(sb, "");
		Line(sb, "        <[list_tmworld_airport_detailed_parking_position][parking_positions][]");
		Line(sb, "");

		for (var i = 0; i < airport.ParkingSlots.Count; i++)
		{
			var slot = airport.ParkingSlots[i];
			if (slot.Latitude is not { } plat || slot.Longitude is not { } plon)
				throw new InvalidOperationException($"{airport.TmeCode} parking '{slot.Name}' needs a position.");

			Line(sb, $"            <[tmworld_airport_detailed_parking_position][element][{i}]");
			Line(sb, $"                <[vector2_float64][position][{FormatWorld(plon, plat)}]>");
			Line(sb, $"                <[float64][direction][{FormatDirection(slot.Heading)}]>");
			Line(sb, $"                <[float64][size][{FormatNumber(slot.Size)}]>");
			Line(sb, $"                <[string8][name][{slot.Name}]>");
			Line(sb, "                <[string8u][tags][pushback]>");
			Line(sb, "            >");
		}

		Line(sb, "        >");
		Line(sb, "");
		Line(sb, "    >");
		Line(sb, ">");
		var text = sb.ToString();
		AeroflySectionBalance.Ensure(text, "WAD");
		return text;
	}

	static void Line(StringBuilder sb, string text) => sb.Append(text).Append("\r\n");

	static string FormatWorld(double longitude, double latitude)
	{
		var x = WadCoordinateConverter.LongitudeToWorld(longitude);
		var y = WadCoordinateConverter.LatitudeToWorld(latitude);
		return string.Create(CultureInfo.InvariantCulture, $"{x:G15} {y:G15}");
	}

	static string FormatDirection(double headingDegrees) =>
		WadCoordinateConverter.HeadingToDirection(headingDegrees).ToString("G15", CultureInfo.InvariantCulture);

	static string FormatNumber(double value) =>
		value.ToString("0.###", CultureInfo.InvariantCulture);
}
