using System.Text;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class TmeStoreArchive
{
	public static byte[] Build(TmeProject project, string? packageName = null)
	{
		var root = PackageNames.Sanitize(packageName ?? project.PackageFolderName);
		project.PackageFolderName = root;

		var zip = new UnixStoreZipBuilder();
		zip.AddDirectory($"{root}/");
		zip.AddDirectory($"{root}/scenery/");
		zip.AddDirectory($"{root}/scenery/airports/");

		foreach (var airport in project.Airports)
		{
			var folder = airport.FolderName;
			if (string.IsNullOrWhiteSpace(folder))
				throw new InvalidOperationException("Every airport needs a 4-character code.");

			zip.AddDirectory($"{root}/scenery/airports/{folder}/");
			var basePath = $"{root}/scenery/airports/{folder}/{folder}";
			zip.AddFile(basePath + ".tsc", Encoding.UTF8.GetBytes(TscWriter.Write(airport)));
			zip.AddFile(basePath + ".wad", Encoding.UTF8.GetBytes(WadWriter.Write(airport)));
		}

		return zip.ToArray();
	}
}
