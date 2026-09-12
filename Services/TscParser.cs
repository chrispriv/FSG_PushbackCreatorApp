using System.Globalization;
using System.Text.RegularExpressions;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class TscParser
{
	static readonly Regex IcaoField = new(@"\[icao\]\[([^\]]+)\]", RegexOptions.CultureInvariant);
	static readonly Regex SnameField = new(@"\[sname\]\[([^\]]+)\]", RegexOptions.CultureInvariant);
	static readonly Regex PositionField = new(@"\[vector2_float64\]\[position\]\[([^\]]+)\]", RegexOptions.CultureInvariant);
	static readonly Regex HelipadBlock = new(
		@"<\[tmsimulator_helipad\]\[element\]\[\d+\](?<body>.*?)(?:\n|\r)\s*>",
		RegexOptions.Singleline | RegexOptions.CultureInvariant);
	static readonly Regex ParkingBlock = new(
		@"<\[tmsimulator_parking_position\]\[element\]\[\d+\](?<body>.*?)(?:\n|\r)\s*>",
		RegexOptions.Singleline | RegexOptions.CultureInvariant);
	static readonly Regex HeadingField = new(@"\[float64\]\[heading\]\[([^\]]+)\]", RegexOptions.CultureInvariant);
	static readonly Regex SizeField = new(@"\[float64\]\[size\]\[([^\]]+)\]", RegexOptions.CultureInvariant);
	static readonly Regex NameField = new(@"\[string8\]\[name\]\[([^\]]+)\]", RegexOptions.CultureInvariant);
	static readonly Regex PushbackTag = new(@"\[tags\]\[pushback\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	public static AirportEntry Parse(string tsc, string? zipEntryPath = null)
	{
		var icao4 = Icao4FromPath(zipEntryPath) ?? Icao4FromTscField(tsc);
		if (!ProjectValidator.IsIcao4(icao4))
			throw new InvalidOperationException("TSC has no 4-character ICAO from folder, file name, or [icao] field.");

		var parkingMatches = ParkingBlock.Matches(tsc);
		var hasPushback = false;
		foreach (Match parking in parkingMatches)
		{
			if (PushbackTag.IsMatch(parking.Groups["body"].Value))
			{
				hasPushback = true;
				break;
			}
		}

		var airport = new AirportEntry
		{
			IsNew = false,
			Kind = hasPushback ? AirportKind.DummyPushback : AirportKind.Heliport,
			Icao4 = icao4
		};

		var snameMatch = SnameField.Match(tsc);
		if (snameMatch.Success)
		{
			var sname = ProjectValidator.ClampSname(snameMatch.Groups[1].Value);
			if (!string.IsNullOrWhiteSpace(sname) &&
			    !string.Equals(sname, icao4, StringComparison.OrdinalIgnoreCase) &&
			    !string.Equals(sname, airport.TmeCode, StringComparison.OrdinalIgnoreCase) &&
			    !string.Equals(sname, airport.FileIcao, StringComparison.OrdinalIgnoreCase))
				airport.Name = sname;
		}

		if (TryFirstPosition(tsc, out var placeLon, out var placeLat))
		{
			airport.HelipadLongitude = placeLon;
			airport.HelipadLatitude = placeLat;
		}

		var helipad = HelipadBlock.Match(tsc);
		if (helipad.Success)
		{
			var body = helipad.Groups["body"].Value;
			if (TryFirstPosition(body, out var hLon, out var hLat))
			{
				airport.HelipadLongitude = hLon;
				airport.HelipadLatitude = hLat;
			}

			if (TryNumber(HeadingField.Match(body).Groups[1].Value, out var heading))
				airport.HelipadHeading = heading;
			airport.HelipadRadius = AirportEntry.DefaultHelipadRadius;
		}

		foreach (Match parking in parkingMatches)
		{
			var body = parking.Groups["body"].Value;
			if (!PushbackTag.IsMatch(body))
				continue;
			if (!TryFirstPosition(body, out var lon, out var lat))
				continue;

			TryNumber(HeadingField.Match(body).Groups[1].Value, out var heading);
			var size = 35d;
			if (TryNumber(SizeField.Match(body).Groups[1].Value, out var parsedSize))
				size = parsedSize;

			var nameMatch = NameField.Match(body);
			var name = nameMatch.Success && !string.IsNullOrWhiteSpace(nameMatch.Groups[1].Value)
				? nameMatch.Groups[1].Value.Trim()
				: $"A{airport.ParkingSlots.Count + 1}";

			airport.ParkingSlots.Add(new ParkingSlot
			{
				IsNew = false,
				Name = name,
				Longitude = lon,
				Latitude = lat,
				Heading = heading,
				Size = size
			});
		}

		return airport;
	}

	static string? Icao4FromPath(string? zipEntryPath)
	{
		if (string.IsNullOrWhiteSpace(zipEntryPath))
			return null;

		var normalized = zipEntryPath.Replace('\\', '/').Trim('/');
		var file = Path.GetFileNameWithoutExtension(normalized);
		var folder = Path.GetFileName(Path.GetDirectoryName(normalized.Replace('/', Path.DirectorySeparatorChar)));
		foreach (var candidate in new[] { folder, file })
		{
			var four = FirstFour(candidate);
			if (four is not null)
				return four;
		}

		return null;
	}

	static string? Icao4FromTscField(string tsc)
	{
		var icaoMatch = IcaoField.Match(tsc);
		if (!icaoMatch.Success)
			return null;
		return FirstFour(icaoMatch.Groups[1].Value);
	}

	static string? FirstFour(string? text)
	{
		var trimmed = (text ?? string.Empty).Trim();
		if (trimmed.Length < 4)
			return null;
		var four = trimmed[..4].ToUpperInvariant();
		return ProjectValidator.IsIcao4(four) ? four : null;
	}

	static bool TryFirstPosition(string text, out double longitude, out double latitude)
	{
		longitude = 0;
		latitude = 0;
		var match = PositionField.Match(text);
		if (!match.Success)
			return false;

		var parts = match.Groups[1].Value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2)
			return false;
		if (!TryNumber(parts[0], out longitude) || !TryNumber(parts[1], out latitude))
			return false;
		return true;
	}

	static bool TryNumber(string? text, out double value)
	{
		value = 0;
		if (string.IsNullOrWhiteSpace(text))
			return false;
		return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}

#if DEBUG
	static TscParser()
	{
		const string sample =
			"""
			<[string8u][icao][BKPR00]>
			<[vector2_float64][position][21.031953 42.564351]>
			<[tmsimulator_helipad][element][0]
			    <[float64][radius][20]>
			    <[float64][heading][265]>
			>
			<[tmsimulator_parking_position][element][0]
			    <[vector2_float64][position][21.031285 42.574318]>
			    <[float64][heading][264]>
			    <[float64][size][23]>
			    <[string8][name][A3]>
			    <[string8u][tags][pushback]>
			>
			""";
		var parsed = Parse(sample, "pkg/scenery/airports/bkpr00/bkpr00.tsc");
		if (parsed.Kind != AirportKind.DummyPushback ||
		    parsed.TmeCode != "BKPR00" ||
		    parsed.FileIcao != "BKPR  " ||
		    parsed.ParkingSlots.Count != 1 ||
		    parsed.ParkingSlots[0].Name != "A3")
			throw new InvalidOperationException("TSC parser failed the BKPR00 sample.");
	}
#endif
}
