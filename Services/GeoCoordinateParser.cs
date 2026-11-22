using System.Globalization;

namespace FSG_PushbackCreator.Services;

public static class GeoCoordinateParser
{
#if DEBUG
	static GeoCoordinateParser()
	{
		if (!TryParse("40.4675 50.0539", out var lat, out var lon) || lat != 40.4675 || lon != 50.0539)
			throw new InvalidOperationException("Decimal space-separated parse failed.");

		if (!TryParse("40.4675, 50.0539", out lat, out lon) || lat != 40.4675 || lon != 50.0539)
			throw new InvalidOperationException("Decimal comma-separated parse failed.");

		if (!TryParse("151.2093 -33.8688", out lat, out lon) || lat != -33.8688 || lon != 151.2093)
			throw new InvalidOperationException("TSC lon-lat order parse failed.");

		if (TryParse("40°28'02.88\"N 50°03'13.86\"E", out _, out _))
			throw new InvalidOperationException("DMS paste must be rejected.");
	}
#endif

	public static bool SamePosition(double latitudeA, double longitudeA, double latitudeB, double longitudeB) =>
		Math.Round(latitudeA, 6, MidpointRounding.AwayFromZero) ==
		Math.Round(latitudeB, 6, MidpointRounding.AwayFromZero) &&
		Math.Round(longitudeA, 6, MidpointRounding.AwayFromZero) ==
		Math.Round(longitudeB, 6, MidpointRounding.AwayFromZero);

	public static string Format(double? latitude, double? longitude)
	{
		if (latitude is not { } lat || longitude is not { } lon)
			return string.Empty;

		return string.Create(CultureInfo.InvariantCulture, $"{lat:0.########}, {lon:0.########}");
	}

	/// <summary>
	/// Decimal lat, lon only (comma or space). If the first number is outside ±90 it is treated as TSC lon lat.
	/// </summary>
	public static bool TryParse(string? text, out double latitude, out double longitude)
	{
		latitude = 0;
		longitude = 0;
		if (string.IsNullOrWhiteSpace(text))
			return false;

		var parts = text.Trim()
			.Replace('\t', ' ')
			.Replace(';', ' ')
			.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
