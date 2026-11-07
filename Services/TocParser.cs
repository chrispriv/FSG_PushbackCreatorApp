using System.Globalization;
using System.Text.RegularExpressions;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class TocParser
{
	static readonly Regex XrefBlock = new(
		@"<\[xref\]\[element\]\[\d+\](?<body>.*?)(?:\n|\r)\s*>",
		RegexOptions.Singleline | RegexOptions.CultureInvariant);
	static readonly Regex NameField = new(@"\[name\]\[([^\]]+)\]", RegexOptions.CultureInvariant);
	static readonly Regex Position3 = new(
		@"\[vector3_float64\]\[position\]\[([^\]]+)\]",
		RegexOptions.CultureInvariant);

	public static void Apply(AirportEntry airport, string toc)
	{
		foreach (Match xref in XrefBlock.Matches(toc))
		{
			var body = xref.Groups["body"].Value;
			var name = NameField.Match(body);
			if (!name.Success || !TryPosition(body, out var lon, out var lat, out var height))
				continue;

			var xrefName = name.Groups[1].Value;
			if (xrefName.Equals(TocWriter.DummyXrefName, StringComparison.OrdinalIgnoreCase))
			{
				if (airport.Kind == AirportKind.Heliport)
					continue;
				if (!airport.HasTowerPosition)
				{
					airport.TowerLongitude = lon;
					airport.TowerLatitude = lat;
				}

				airport.TowerHeightAgl = height;
			}
			else if (xrefName.Equals(TocWriter.WindsockXrefName, StringComparison.OrdinalIgnoreCase))
			{
				if (airport.Kind != AirportKind.Heliport)
					continue;
				airport.WindsockLongitude = lon;
				airport.WindsockLatitude = lat;
				airport.WindsockHeightOffset = height;
			}
			else if (xrefName.Equals(TocWriter.DummyHelipadXrefName, StringComparison.OrdinalIgnoreCase))
			{
				if (airport.Kind != AirportKind.Heliport)
					continue;
				airport.DummyHelipadEnabled = true;
				airport.HelipadHeightOffset = height - AirportEntry.DummyHelipadHeightAgl;
			}
		}
	}

	static bool TryPosition(string body, out double longitude, out double latitude, out double height)
	{
		longitude = 0;
		latitude = 0;
		height = 0;
		var pos = Position3.Match(body);
		if (!pos.Success)
			return false;

		var parts = pos.Groups[1].Value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 3)
			return false;
		return TryNumber(parts[0], out longitude) &&
		       TryNumber(parts[1], out latitude) &&
		       TryNumber(parts[2], out height);
	}

	static bool TryNumber(string? text, out double value)
	{
		value = 0;
		if (string.IsNullOrWhiteSpace(text))
			return false;
		return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}

#if DEBUG
	static TocParser()
	{
		const string towerSample =
			"""
			<[file][][]
			    <[cultivation][][]
			        <[list_xref][xref_list][]
			            <[xref][element][0]
			                <[string8u][name][landrider01]>
			                <[vector3_float64][position][50.058596 40.459153 200.00]>
			            >
			        >
			    >
			>
			""";
		var tower = new AirportEntry { Kind = AirportKind.DummyPushback, Icao4 = "UBBB" };
		Apply(tower, towerSample);
		if (tower.TowerHeightAgl != 200 ||
		    tower.TowerLongitude is not 50.058596 ||
		    tower.TowerLatitude is not 40.459153)
			throw new InvalidOperationException("TOC parser failed the landrider01 sample.");

		const string heliportSample =
			"""
			<[file][][]
			    <[cultivation][][]
			        <[list_xref][xref_list][]
			            <[xref][element][0]
			                <[string8u][name][airportmast03]>
			                <[vector3_float64][position][32.864703 34.943769 -23.5]>
			            >
			            <[xref][element][1]
			                <[string8u][name][airport_windsock_large1]>
			                <[vector3_float64][position][32.864664 34.943618 0]>
			            >
			        >
			    >
			>
			""";
		var pad = new AirportEntry { Kind = AirportKind.Heliport, Icao4 = "GR12" };
		Apply(pad, heliportSample);
		if (!pad.DummyHelipadEnabled ||
		    pad.WindsockLongitude is not 32.864664 ||
		    pad.WindsockLatitude is not 34.943618)
			throw new InvalidOperationException("TOC parser failed the GR12 heliport sample.");
	}
#endif
}
