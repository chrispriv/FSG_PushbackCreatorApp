using System.Text.RegularExpressions;

namespace FSG_PushbackCreator.Services;

public static class PackageNames
{
	public const string Default = "pca_scenery_pushback";
	public const int MaxLength = 64;
	static readonly Regex CopySuffix = new(@"\s+\(\d+\)$", RegexOptions.CultureInvariant);

	public static string Sanitize(string? name)
	{
		var trimmed = (name ?? string.Empty).Trim();
		trimmed = trimmed.Replace('\\', '/').Trim('/');
		if (trimmed.Contains('/'))
			trimmed = trimmed[(trimmed.LastIndexOf('/') + 1)..];

		trimmed = CopySuffix.Replace(trimmed, string.Empty);

		foreach (var c in Path.GetInvalidFileNameChars())
			trimmed = trimmed.Replace(c, '_');

		trimmed = trimmed.Trim('.', ' ');
		if (string.IsNullOrWhiteSpace(trimmed))
			return Default;

		return trimmed.Length <= MaxLength ? trimmed : trimmed[..MaxLength];
	}

	public static string FromFileName(string? fileName) =>
		Sanitize(Path.GetFileNameWithoutExtension(fileName ?? string.Empty));
}
