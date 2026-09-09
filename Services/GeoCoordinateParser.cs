using System.Globalization;
using System.Text.RegularExpressions;

namespace FSG_PushbackCreator.Services;

public static class GeoCoordinateParser
{
#if DEBUG
	static GeoCoordinateParser()
	{
		if (!TryParse("40°28'02.88\"N 50°03'13.86\"E", out var lat, out var lon) ||
		    Math.Abs(lat - (40 + 28 / 60d + 2.88 / 3600d)) > 1e-8 ||
		    Math.Abs(lon - (50 + 3 / 60d + 13.86 / 3600d)) > 1e-8)
			throw new InvalidOperationException("DMS space-separated parse failed.");

		if (!TryParse("40°28'02.88\"N, 50°03'13.86\"E", out lat, out lon) ||
		    Math.Abs(lat - (40 + 28 / 60d + 2.88 / 3600d)) > 1e-8)
			throw new InvalidOperationException("DMS comma-separated parse failed.");

		if (!TryParse("40.4675 50.0539", out lat, out lon) || lat != 40.4675 || lon != 50.0539)
			throw new InvalidOperationException("Decimal space-separated parse failed.");
	}
#endif
	static readonly Regex DmsToken = new(
		@"(?<deg>\d{1,3}(?:[.,]\d+)?)\s*°\s*(?<min>\d{1,2}(?:[.,]\d+)?)?\s*['′]?\s*(?<sec>\d{1,2}(?:[.,]\d+)?)?\s*[""″]?\s*(?<hem>[NSEWnsew])",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	static readonly Regex DecimalHemToken = new(
		@"(?<hemPrefix>[NSEWnsew])\s*(?<val>[+-]?\d+(?:[.,]\d+)?)|(?<val>[+-]?\d+(?:[.,]\d+)?)\s*(?<hemSuffix>[NSEWnsew])",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static string Format(double? latitude, double? longitude)
	{
		if (latitude is not { } lat || longitude is not { } lon)
			return string.Empty;

		return string.Create(CultureInfo.InvariantCulture, $"{lat:0.########}, {lon:0.########}");
	}

	/// <summary>
	/// Accepts Google Earth DMS (e.g. 40°28'02.88"N 50°03'13.86"E), decimal with comma or space,
	/// and TSC "lon lat" when the first number is outside ±90.
	/// </summary>
	public static bool TryParse(string? text, out double latitude, out double longitude)
	{
		latitude = 0;
		longitude = 0;
		if (string.IsNullOrWhiteSpace(text))
			return false;

		var cleaned = Normalize(text);
		if (TryParseDmsPair(cleaned, out latitude, out longitude))
			return true;
		if (TryParseDecimalWithHemisphere(cleaned, out latitude, out longitude))
			return true;
		return TryParseDecimalPair(cleaned, out latitude, out longitude);
	}

	static string Normalize(string text)
	{
		return text.Trim()
			.Replace('\u00BA', '°')
			.Replace('\u00B0', '°')
			.Replace('\u2032', '\'')
			.Replace('\u2033', '"')
			.Replace('\u2019', '\'')
			.Replace('\u201D', '"')
			.Replace('´', '\'')
			.Replace('\t', ' ')
			.Replace(';', ' ');
	}

	static bool TryParseDmsPair(string text, out double latitude, out double longitude)
	{
		latitude = 0;
		longitude = 0;
		var matches = DmsToken.Matches(text);
		if (matches.Count < 2)
			return false;

		if (!TryDmsMatch(matches[0], out var first, out var firstHem))
			return false;
		if (!TryDmsMatch(matches[1], out var second, out var secondHem))
			return false;

		return AssignByHemisphere(first, firstHem, second, secondHem, out latitude, out longitude);
	}

	static bool TryDmsMatch(Match match, out double value, out char hem)
	{
		value = 0;
		hem = char.ToUpperInvariant(match.Groups["hem"].Value[0]);
		if (!TryNumber(match.Groups["deg"].Value, out var deg))
			return false;

		TryNumber(match.Groups["min"].Value, out var min);
		TryNumber(match.Groups["sec"].Value, out var sec);
		if (min is < 0 or >= 60 || sec is < 0 or >= 60)
			return false;

		value = deg + (min / 60d) + (sec / 3600d);
		if (hem is 'S' or 'W')
			value = -value;
		return true;
	}

	static bool TryParseDecimalWithHemisphere(string text, out double latitude, out double longitude)
	{
		latitude = 0;
		longitude = 0;
		var matches = DecimalHemToken.Matches(text);
		if (matches.Count < 2)
			return false;

		if (!TryDecimalHem(matches[0], out var first, out var firstHem))
			return false;
		if (!TryDecimalHem(matches[1], out var second, out var secondHem))
			return false;

		return AssignByHemisphere(first, firstHem, second, secondHem, out latitude, out longitude);
	}

	static bool TryDecimalHem(Match match, out double value, out char hem)
	{
		value = 0;
		var hemText = match.Groups["hemPrefix"].Success
			? match.Groups["hemPrefix"].Value
			: match.Groups["hemSuffix"].Value;
		hem = char.ToUpperInvariant(hemText[0]);
		if (!TryNumber(match.Groups["val"].Value, out value))
			return false;
		if (hem is 'S' or 'W')
			value = -Math.Abs(value);
		else
			value = Math.Abs(value);
		return true;
	}

	static bool AssignByHemisphere(
		double first, char firstHem, double second, char secondHem,
		out double latitude, out double longitude)
	{
		latitude = 0;
		longitude = 0;
		var firstNs = firstHem is 'N' or 'S';
		var firstEw = firstHem is 'E' or 'W';
		var secondNs = secondHem is 'N' or 'S';
		var secondEw = secondHem is 'E' or 'W';
		if (firstNs && secondEw)
		{
			latitude = first;
			longitude = second;
		}
		else if (firstEw && secondNs)
		{
			longitude = first;
			latitude = second;
		}
		else
			return false;

		return Math.Abs(latitude) <= 90 && Math.Abs(longitude) <= 180;
	}

	static bool TryParseDecimalPair(string text, out double latitude, out double longitude)
	{
		latitude = 0;
		longitude = 0;
		var parts = text.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length < 2)
			return false;
		if (!TryNumber(parts[0], out var a) || !TryNumber(parts[1], out var b))
			return false;

		if (Math.Abs(a) > 90 && Math.Abs(b) <= 90)
		{
			longitude = a;
			latitude = b;
		}
		else
		{
			latitude = a;
			longitude = b;
		}

		return Math.Abs(latitude) <= 90 && Math.Abs(longitude) <= 180;
	}

	static bool TryNumber(string? text, out double value)
	{
		value = 0;
		if (string.IsNullOrWhiteSpace(text))
			return false;

		var normalized = text.Trim().Replace(',', '.');
		return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}
}
