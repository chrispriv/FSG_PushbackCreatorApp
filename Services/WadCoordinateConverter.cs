namespace FSG_PushbackCreator.Services;

/// <summary>
/// TSC geographic degrees to Aerofly WAD world units.
/// Matches CalcES in RAD mode: lon/lat scale 65536, heading as direction in radians.
/// West longitude and south latitude are negative.
/// </summary>
public static class WadCoordinateConverter
{
	public const double LatitudeTanScale = 2.3311223704144;
	public const double WorldExtent = 65536;

	public static double LongitudeToWorld(double longitudeDegrees) =>
		WorldExtent * (0.5 + 0.5 * longitudeDegrees / 180.0);

	public static double LatitudeToWorld(double latitudeDegrees)
	{
		var tanArg = LatitudeTanScale * latitudeDegrees / 180.0;
		return WorldExtent * (0.5 + 0.5 * (Math.Tan(tanArg) / LatitudeTanScale));
	}

	public static double HeadingToDirection(double headingDegrees)
	{
		var hdg = NormalizeHeading(headingDegrees);
		var offset = hdg <= 90 ? 90 - hdg : 450 - hdg;
		return offset * Math.PI / 180.0;
	}

	public static double NormalizeHeading(double headingDegrees)
	{
		var hdg = headingDegrees % 360.0;
		if (hdg < 0)
			hdg += 360.0;
		return hdg;
	}

#if DEBUG
	static WadCoordinateConverter()
	{
		var x = LongitudeToWorld(21.031953);
		var y = LatitudeToWorld(42.564351);
		if (Math.Abs(x - 36596.7501994667) > 1e-8 || Math.Abs(y - 41410.2135779192) > 1e-8)
			throw new InvalidOperationException("WAD lon/lat conversion does not match the BKPR00 sample.");

		var dir = HeadingToDirection(265);
		if (Math.Abs(dir - 3.22885911618951) > 1e-12)
			throw new InvalidOperationException("WAD heading conversion does not match the BKPR00 sample.");
	}
#endif
}
