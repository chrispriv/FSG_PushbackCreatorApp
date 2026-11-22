using System.Globalization;
using System.Reflection;
using System.Text;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

/// <summary>
/// XREF objects as a scenery POI so autoheight uses terrain, not airport ground mesh
/// (heliport windsock / dummy helipad / tower dummy, and pushback tower dummy).
/// </summary>
public static class PoiWriter
{
	public const string NameMarker = "pca_";
	public const string AnchorGeometry = "pct_anchor";
	const string AnchorResourceTmb = "poi.pct_anchor.tmb";
	const string AnchorResourceTtx = "poi.pct_anchor.ttx";

	public static bool ShouldWrite(AirportEntry airport) => airport.WritesToc;

	public static string FolderName(AirportEntry airport)
	{
		if (airport.HelipadLatitude is not { } lat || airport.HelipadLongitude is not { } lon)
			throw new InvalidOperationException($"{airport.TmeCode} POI needs the helipad position.");

		return $"{GridToken(lat, lon)}_{NameMarker}{airport.FolderName}";
	}

	public static string GridToken(double latitude, double longitude)
	{
		var lonHem = longitude >= 0 ? 'e' : 'w';
		var latHem = latitude >= 0 ? 'n' : 's';
		var lonCenti = (int)Math.Round(Math.Abs(longitude) * 100.0, MidpointRounding.AwayFromZero);
		var latCenti = (int)Math.Round(Math.Abs(latitude) * 100.0, MidpointRounding.AwayFromZero);
		return string.Create(CultureInfo.InvariantCulture, $"{lonHem}{lonCenti:00000}{latHem}{latCenti:0000}");
	}

	public static IReadOnlyList<(string RelativePath, byte[] Data)> Files(AirportEntry airport)
	{
		if (!ShouldWrite(airport))
			throw new InvalidOperationException($"{airport.TmeCode} does not need a POI.");

		var folder = FolderName(airport);
		var stem = airport.FolderName;
		return
		[
			($"{folder}/{stem}.toc", Encoding.UTF8.GetBytes(TocWriter.Write(airport))),
			($"{folder}/{stem}.tsl", Encoding.UTF8.GetBytes(WriteTsl(airport, stem))),
			($"{folder}/{AnchorGeometry}.tmb", LoadResource(AnchorResourceTmb)),
			($"{folder}/{AnchorGeometry}.ttx", LoadResource(AnchorResourceTtx))
		];
	}

	public static bool IsPoiTocPath(string path)
	{
		var normalized = path.Replace('\\', '/');
		return normalized.Contains("/poi/", StringComparison.OrdinalIgnoreCase);
	}

	static string WriteTsl(AirportEntry airport, string stem)
	{
		if (airport.HelipadLatitude is not { } lat || airport.HelipadLongitude is not { } lon)
			throw new InvalidOperationException($"{airport.TmeCode} POI needs the helipad position.");

		var pos = string.Create(CultureInfo.InvariantCulture, $"{lon:0.#######} {lat:0.#######} -1.0");
		var sb = new StringBuilder();
		Line(sb, "<[file][][]");
		Line(sb, "    <[tmsimulator_scenery_place_simple][][]");
		Line(sb, "        <[string8u][coordinate_system][lonlat]>");
		Line(sb, "        <[bool][autoheight][true]>");
		Line(sb, $"        <[string8u][cultivation][{stem}]>");
		Line(sb, "        <[list_tmsimulator_scenery_object][objects][]");
		Line(sb, "            <[tmsimulator_scenery_object][element][0]");
		Line(sb, "                <[string8u][type][object]>");
		Line(sb, $"                <[string8u][geometry][{AnchorGeometry}]>");
		Line(sb, $"                <[vector3_float64][position][{pos}]>");
		Line(sb, "                <[int32][autoheight_override][-1]>");
		Line(sb, "            >");
		Line(sb, "        >");
		Line(sb, "    >");
		Line(sb, ">");
		var text = sb.ToString();
		AeroflySectionBalance.Ensure(text, "TSL");
		return text;
	}

	static byte[] LoadResource(string logicalName)
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream(logicalName)
			?? throw new InvalidOperationException($"Missing embedded POI anchor '{logicalName}'.");
		using var copy = new MemoryStream();
		stream.CopyTo(copy);
		return copy.ToArray();
	}

	static void Line(StringBuilder sb, string text) => sb.Append(text).Append("\r\n");

#if DEBUG
	static PoiWriter()
	{
		if (GridToken(46.7397577, 7.6330948) != "e00763n4674")
			throw new InvalidOperationException("POI grid token does not match e00763n4674.");
	}
#endif
}
