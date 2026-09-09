using System.Globalization;
using System.Text;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class TscWriter
{
	public static string Write(AirportEntry airport)
	{
		if (airport.HelipadLatitude is not { } lat || airport.HelipadLongitude is not { } lon)
			throw new InvalidOperationException($"{airport.TmeCode} needs a helipad position.");

		var code = airport.TmeCode.ToUpperInvariant();
		var sname = airport.Kind == AirportKind.DummyPushback
			? code
			: string.IsNullOrWhiteSpace(airport.Name) ? code : ProjectValidator.ClampSname(airport.Name);
		var sb = new StringBuilder();
		Line(sb, "<[file][][]");
		Line(sb, "    <[tmsimulator_scenery_place][][]");
		Line(sb, "");
		Line(sb, $"        <[string8][sname][{sname}]>");
		if (airport.Kind == AirportKind.Heliport)
			Line(sb, $"        <[string8][lname][{sname}]>");
		Line(sb, $"        <[string8u][icao][{code}]>");
		Line(sb, "        <[string8u][coordinate_system][flat]>");
		Line(sb, $"        <[vector2_float64][position][{FormatLonLat(lon, lat)}]>");
		Line(sb, "        <[bool][autoheight][true]>");
		Line(sb, "");
		Line(sb, "        <[list_tmsimulator_helipad][helipads][]");
		Line(sb, "            <[tmsimulator_helipad][element][0]");
		Line(sb, "                <[string8][name][H01]>");
		Line(sb, $"                <[vector2_float64][position][{FormatLonLat(lon, lat)}]>");
		Line(sb, $"                <[float64][radius][{FormatNumber(AirportEntry.DefaultHelipadRadius)}]>");
		Line(sb, $"                <[float64][heading][{FormatNumber(airport.HelipadHeading)}]>");
		Line(sb, "            >");
		Line(sb, "        >");
		Line(sb, "");
		Line(sb, "        <[list_tmsimulator_parking_position][parking_positions][]");
		Line(sb, "");

		for (var i = 0; i < airport.ParkingSlots.Count; i++)
		{
			var slot = airport.ParkingSlots[i];
			if (slot.Latitude is not { } plat || slot.Longitude is not { } plon)
				throw new InvalidOperationException($"{code} parking '{slot.Name}' needs a position.");

			Line(sb, $"            <[tmsimulator_parking_position][element][{i}]");
			Line(sb, $"                <[vector2_float64][position][{FormatLonLat(plon, plat)}]>");
			Line(sb, $"                <[float64][heading][{FormatNumber(slot.Heading)}]>");
			Line(sb, $"                <[float64][size][{FormatNumber(slot.Size)}]>");
			Line(sb, $"                <[string8][name][{slot.Name}]>");
			Line(sb, "                <[string8u][tags][pushback]>");
			Line(sb, "	    >");
		}

		Line(sb, "");
		Line(sb, "        >");
		Line(sb, "");
		Line(sb, "    >");
		Line(sb, ">");
		return sb.ToString();
	}

	static void Line(StringBuilder sb, string text) => sb.Append(text).Append("\r\n");

	static string FormatLonLat(double longitude, double latitude) =>
		string.Create(CultureInfo.InvariantCulture, $"{longitude:0.######} {latitude:0.######}");

	static string FormatNumber(double value) =>
		value.ToString("0.###", CultureInfo.InvariantCulture);
}
