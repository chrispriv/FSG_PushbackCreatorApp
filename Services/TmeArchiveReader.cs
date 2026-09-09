using System.IO.Compression;
using System.Text;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public sealed class TmeImportResult
{
	public required TmeProject Project { get; init; }
	public int TscCount { get; init; }
	public int SkippedCount { get; init; }
}

public static class TmeArchiveReader
{
	public static TmeImportResult Read(Stream zipStream)
	{
		using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true, Encoding.UTF8);
		var project = new TmeProject();
		var skipped = 0;
		string? package = null;

		foreach (var entry in zip.Entries.OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase))
		{
			var path = entry.FullName.Replace('\\', '/');
			if (entry.Length == 0 || path.EndsWith('/'))
				continue;
			if (!path.EndsWith(".tsc", StringComparison.OrdinalIgnoreCase))
				continue;
			if (path.Contains("__MACOSX", StringComparison.OrdinalIgnoreCase) ||
			    Path.GetFileName(path).StartsWith('.'))
			{
				skipped++;
				continue;
			}

			package ??= PackageNameFromPath(path);

			using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			var tsc = reader.ReadToEnd();
			try
			{
				var airport = TscParser.Parse(tsc);
				project.Airports.Add(airport);
			}
			catch
			{
				skipped++;
			}
		}

			if (project.Airports.Count == 0)
			throw new InvalidOperationException("No airport .tsc files were found in this archive.");

		if (!string.IsNullOrWhiteSpace(package))
			project.PackageFolderName = package;

		var ordered = project.Airports.OrderBy(a => a.TmeCode, StringComparer.OrdinalIgnoreCase).ToList();
		project.Airports.Clear();
		foreach (var airport in ordered)
			project.Airports.Add(airport);

		return new TmeImportResult
		{
			Project = project,
			TscCount = project.Airports.Count,
			SkippedCount = skipped
		};
	}

	static string? PackageNameFromPath(string path)
	{
		const string tmeMarker = "/scenery/airports/";
		var tmeIndex = path.IndexOf(tmeMarker, StringComparison.OrdinalIgnoreCase);
		if (tmeIndex > 0)
			return path[..tmeIndex];

		const string fs4Marker = "/airports/";
		var fs4Index = path.IndexOf(fs4Marker, StringComparison.OrdinalIgnoreCase);
		if (fs4Index > 0)
			return path[..fs4Index];

		return null;
	}
}
