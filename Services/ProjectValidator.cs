using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class ProjectValidator
{
	public static bool IsIcao4(string? code) =>
		!string.IsNullOrWhiteSpace(code) && code.Trim().Length == 4 && code.Trim().All(char.IsLetterOrDigit);

	public const int SnameMaxLength = 32;

	public static string ClampSname(string? name)
	{
		var trimmed = (name ?? string.Empty).Trim();
		return trimmed.Length <= SnameMaxLength ? trimmed : trimmed[..SnameMaxLength];
	}

	public static string? DuplicateCode(TmeProject project, AirportEntry airport)
	{
		var code = airport.TmeCode;
		if (string.IsNullOrEmpty(code))
			return "Enter a 4-character airport code.";

		if (project.Airports.Any(a => a.Id != airport.Id &&
		                              string.Equals(a.TmeCode, code, StringComparison.OrdinalIgnoreCase)))
			return $"Code {code} is already in this project.";

		return null;
	}
}
