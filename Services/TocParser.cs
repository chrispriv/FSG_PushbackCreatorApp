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

	public static void ApplyDummy(AirportEntry airport, string toc)
	{
		if (airport.Kind == AirportKind.Heliport)
			return;

		foreach (Match xref in XrefBlock.Matches(toc))
		{
			var body = xref.Groups["body"].Value;
			var name = NameField.Match(body);
			if (!name.Success ||
			    !name.Groups[1].Value.Equals(TocWriter.DummyXrefName, StringComparison.OrdinalIgnoreCase))
				continue;

			var pos = Position3.Match(body);
			if (!pos.Success)
				return;

			var parts = pos.Groups[1].Value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3)
				return;
			if (!TryNumber(parts[0], out var lon) ||
			    !TryNumber(parts[1], out var lat) ||
			    !TryNumber(parts[2], out var height))
				return;

			if (!airport.HasTowerPosition)
			{
				airport.TowerLongitude = lon;
				airport.TowerLatitude = lat;
			}

			airport.TowerHeightAgl = height;
			return;
		}
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
		const string sample =
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
		var airport = new AirportEntry { Kind = AirportKind.DummyPushback, Icao4 = "UBBB" };
		ApplyDummy(airport, sample);
		if (airport.TowerHeightAgl != 200 ||
		    airport.TowerLongitude is not 50.058596 ||
		    airport.TowerLatitude is not 40.459153)
			throw new InvalidOperationException("TOC parser failed the landrider01 sample.");
	}
#endif
}
