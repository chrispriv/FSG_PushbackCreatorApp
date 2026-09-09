using System.IO.Compression;
using System.Text;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

/// <summary>
/// Normal Windows ZIP: package\airports\{code}\{code}.tsc|wad for Aerofly FS4 addons\scenery.
/// </summary>
public static class Fs4ZipArchive
{
	public static byte[] Build(TmeProject project, string? packageName = null)
	{
		var root = PackageNames.Sanitize(packageName ?? project.PackageFolderName);
		project.PackageFolderName = root;

		using var output = new MemoryStream();
		using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
		{
			AddDirectory(zip, $"{root}/");
			AddDirectory(zip, $"{root}/airports/");

			foreach (var airport in project.Airports)
			{
				var folder = airport.FolderName;
				if (string.IsNullOrWhiteSpace(folder))
					throw new InvalidOperationException("Every airport needs a 4-character code.");

				AddDirectory(zip, $"{root}/airports/{folder}/");
				var basePath = $"{root}/airports/{folder}/{folder}";
				AddFile(zip, basePath + ".tsc", Encoding.UTF8.GetBytes(TscWriter.Write(airport)));
				AddFile(zip, basePath + ".wad", Encoding.UTF8.GetBytes(WadWriter.Write(airport)));
			}
		}

		return output.ToArray();
	}

	static void AddDirectory(ZipArchive zip, string path)
	{
		zip.CreateEntry(path.EndsWith('/') ? path : path + "/");
	}

	static void AddFile(ZipArchive zip, string path, byte[] data)
	{
		var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
		using var stream = entry.Open();
		stream.Write(data, 0, data.Length);
	}
}
