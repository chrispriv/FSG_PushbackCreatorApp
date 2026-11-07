using System.Globalization;
using System.Text;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class TocWriter
{
	public const string DummyXrefName = "landrider01";
	public const string DummyScale = "0.001";
	public const string WindsockXrefName = "airport_windsock_large1";
	public const string DummyHelipadXrefName = "airportmast03";

	public static bool ShouldWrite(AirportEntry airport) => airport.WritesToc;

	public static string Write(AirportEntry airport)
	{
		if (!airport.WritesToc)
			throw new InvalidOperationException($"{airport.TmeCode} does not need a .toc file.");

		var sb = new StringBuilder();
		Line(sb, "<[file][][]");
		Line(sb, "    <[cultivation][][]");
		Line(sb, "        <[string8u][coordinate_system][lonlat]>");
		Line(sb, "        <[list_xref][xref_list][]");

		var index = 0;
		if (airport.WritesTowerDummy)
		{
			if (airport.TowerLatitude is not { } tLat || airport.TowerLongitude is not { } tLon)
				throw new InvalidOperationException($"{airport.TmeCode} tower dummy needs a tower position.");
			WriteXref(sb, index++, DummyXrefName, tLon, tLat, airport.TowerHeightAgl, direction: 0, DummyScale);
		}

		if (airport.HasDummyHelipad)
		{
			if (airport.HelipadLatitude is not { } padLat || airport.HelipadLongitude is not { } padLon)
				throw new InvalidOperationException($"{airport.TmeCode} dummy helipad needs the helipad position.");
			WriteXref(
				sb,
				index++,
				DummyHelipadXrefName,
				padLon,
				padLat,
				airport.HelipadTocHeight,
				HeadingToXrefDirection(airport.HelipadHeading),
				AirportEntry.DummyHelipadScale.ToString("0.#", CultureInfo.InvariantCulture));
		}

		if (airport.HasWindsock)
		{
			WriteXref(
				sb,
				index++,
				WindsockXrefName,
				airport.WindsockLongitude!.Value,
				airport.WindsockLatitude!.Value,
				airport.WindsockTocHeight,
				HeadingToXrefDirection(airport.HelipadHeading),
				"1");
		}

		Line(sb, "        >");
		Line(sb, "    >");
		Line(sb, ">");
		var text = sb.ToString();
		AeroflySectionBalance.Ensure(text, "TOC");
		return text;
	}

	/// <summary>
	/// XREF [direction] uses the same heading wrap as WAD, but in degrees (TOC files store degrees).
	/// Thumb-up / CCW: direction 0 is heading 90 (east). Heading 80 → 10, heading 120 → 330.
	/// </summary>
	public static double HeadingToXrefDirection(double headingDegrees)
	{
		var radians = WadCoordinateConverter.HeadingToDirection(headingDegrees);
		var degrees = radians * 180.0 / Math.PI;
		degrees %= 360.0;
		if (degrees < 0)
			degrees += 360.0;
		return degrees;
	}

	static void WriteXref(
		StringBuilder sb,
		int index,
		string name,
		double longitude,
		double latitude,
		double height,
		double direction,
		string scale)
	{
		Line(sb, $"            <[xref][element][{index}]");
		Line(sb, $"                <[string8u][name][{name}]>");
		Line(sb, $"                <[vector3_float64][position][{FormatLonLatHeight(longitude, latitude, height)}]>");
		Line(sb, $"                <[float32][direction][{direction.ToString("0.###", CultureInfo.InvariantCulture)}]>");
		Line(sb, $"                <[float32][scale_factor][{scale}]>");
		Line(sb, "            >");
	}

	static void Line(StringBuilder sb, string text) => sb.Append(text).Append("\r\n");

	static string FormatLonLatHeight(double longitude, double latitude, double height) =>
		string.Create(CultureInfo.InvariantCulture, $"{longitude:0.######} {latitude:0.######} {height:0.##}");

#if DEBUG
	static TocWriter()
	{
		// Helipad heading 80° → direction 10; 120° → 330.
		if (Math.Abs(HeadingToXrefDirection(80) - 10) > 1e-9 ||
		    Math.Abs(HeadingToXrefDirection(120) - 330) > 1e-9)
			throw new InvalidOperationException("XREF direction does not match helipad heading (80→10, 120→330).");
	}
#endif
}
