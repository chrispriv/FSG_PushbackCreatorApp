using System.Globalization;
using System.Text;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class TocWriter
{
	public const string DummyXrefName = "landrider01";
	public const string DummyScale = "0.001";

	public static bool ShouldWrite(AirportEntry airport) => airport.WritesTowerDummy;

	public static string Write(AirportEntry airport)
	{
		if (!airport.WritesTowerDummy)
			throw new InvalidOperationException($"{airport.TmeCode} does not need a .toc dummy (tower height is 0).");
		if (airport.TowerLatitude is not { } lat || airport.TowerLongitude is not { } lon)
			throw new InvalidOperationException($"{airport.TmeCode} tower dummy needs a tower position.");

		var sb = new StringBuilder();
		Line(sb, "<[file][][]");
		Line(sb, "    <[cultivation][][]");
		Line(sb, "        <[string8u][coordinate_system][lonlat]>");
		Line(sb, "        <[list_xref][xref_list][]");
		Line(sb, "            <[xref][element][0]");
		Line(sb, $"                <[string8u][name][{DummyXrefName}]>");
		Line(sb, $"                <[vector3_float64][position][{FormatLonLatHeight(lon, lat, airport.TowerHeightAgl)}]>");
		Line(sb, "                <[float32][direction][0]>");
		Line(sb, $"                <[float32][scale_factor][{DummyScale}]>");
		Line(sb, "            >");
		Line(sb, "        >");
		Line(sb, "    >");
		Line(sb, ">");
		var text = sb.ToString();
		AeroflySectionBalance.Ensure(text, "TOC");
		return text;
	}

	static void Line(StringBuilder sb, string text) => sb.Append(text).Append("\r\n");

	static string FormatLonLatHeight(double longitude, double latitude, double height) =>
		string.Create(CultureInfo.InvariantCulture, $"{longitude:0.######} {latitude:0.######} {height:0.00}");
}
