using System.Buffers.Binary;
using System.Text;

namespace FSG_PushbackCreator.Services;

/// <summary>
/// Info-ZIP Store archive with Unix version-made-by, ux/UT extras, and data descriptors.
/// Matches FSG-readable TMEs (Windows "ZIP / Store" and the working @test package).
/// </summary>
public sealed class UnixStoreZipBuilder
{
	readonly MemoryStream _output = new();
	readonly List<CentralEntry> _centrals = [];
	readonly DateTime _stamp;
	readonly uint _unixTime;
	readonly ushort _dosTime;
	readonly ushort _dosDate;

	public UnixStoreZipBuilder(DateTime? stamp = null)
	{
		_stamp = (stamp ?? DateTime.Now).ToLocalTime();
		_unixTime = (uint)new DateTimeOffset(_stamp.ToUniversalTime()).ToUnixTimeSeconds();
		_dosTime = (ushort)((_stamp.Second / 2) | (_stamp.Minute << 5) | (_stamp.Hour << 11));
		_dosDate = (ushort)(_stamp.Day | (_stamp.Month << 5) | ((_stamp.Year - 1980) << 9));
	}

	public void AddDirectory(string path)
	{
		var name = NormalizeDir(path);
		var extraLocal = BuildLocalExtra();
		var extraCentral = BuildCentralExtra();
		var nameBytes = Encoding.UTF8.GetBytes(name);
		var offset = (uint)_output.Position;

		WriteLocalHeader(versionNeeded: 20, flags: 0, crc: 0, size: 0, nameBytes, extraLocal);
		_centrals.Add(new CentralEntry(
			Name: nameBytes,
			IsDirectory: true,
			VersionMadeBy: 0x0314,
			VersionNeeded: 20,
			Flags: 0,
			Crc: 0,
			Size: 0,
			LocalOffset: offset,
			ExternalAttr: 0x41FF0000,
			ExtraCentral: extraCentral));
	}

	public void AddFile(string path, byte[] data)
	{
		var name = path.Replace('\\', '/').TrimStart('/');
		var nameBytes = Encoding.UTF8.GetBytes(name);
		var extraLocal = BuildLocalExtra();
		var extraCentral = BuildCentralExtra();
		var crc = Crc32Ieee.Compute(data);
		var offset = (uint)_output.Position;
		var size = (uint)data.Length;

		WriteLocalHeader(versionNeeded: 10, flags: 0x0008, crc: 0, size: 0, nameBytes, extraLocal);
		_output.Write(data, 0, data.Length);
		WriteU32(0x08074B50);
		WriteU32(crc);
		WriteU32(size);
		WriteU32(size);

		_centrals.Add(new CentralEntry(
			Name: nameBytes,
			IsDirectory: false,
			VersionMadeBy: 0x030A,
			VersionNeeded: 10,
			Flags: 0x0008,
			Crc: crc,
			Size: size,
			LocalOffset: offset,
			ExternalAttr: 0x81B60000,
			ExtraCentral: extraCentral));
	}

	public byte[] ToArray()
	{
		var cdStart = (uint)_output.Position;
		foreach (var entry in _centrals)
			WriteCentral(entry);
		var cdSize = (uint)(_output.Position - cdStart);
		var count = (ushort)_centrals.Count;

		WriteU32(0x06054B50);
		WriteU16(0);
		WriteU16(0);
		WriteU16(count);
		WriteU16(count);
		WriteU32(cdSize);
		WriteU32(cdStart);
		WriteU16(0);
		return _output.ToArray();
	}

	void WriteLocalHeader(ushort versionNeeded, ushort flags, uint crc, uint size, byte[] nameBytes, byte[] extra)
	{
		WriteU32(0x04034B50);
		WriteU16(versionNeeded);
		WriteU16(flags);
		WriteU16(0);
		WriteU16(_dosTime);
		WriteU16(_dosDate);
		WriteU32(crc);
		WriteU32(size);
		WriteU32(size);
		WriteU16((ushort)nameBytes.Length);
		WriteU16((ushort)extra.Length);
		_output.Write(nameBytes, 0, nameBytes.Length);
		_output.Write(extra, 0, extra.Length);
	}

	void WriteCentral(CentralEntry entry)
	{
		WriteU32(0x02014B50);
		WriteU16(entry.VersionMadeBy);
		WriteU16(entry.VersionNeeded);
		WriteU16(entry.Flags);
		WriteU16(0);
		WriteU16(_dosTime);
		WriteU16(_dosDate);
		WriteU32(entry.Crc);
		WriteU32(entry.Size);
		WriteU32(entry.Size);
		WriteU16((ushort)entry.Name.Length);
		WriteU16((ushort)entry.ExtraCentral.Length);
		WriteU16(0);
		WriteU16(0);
		WriteU16(0);
		WriteU32(entry.ExternalAttr);
		WriteU32(entry.LocalOffset);
		_output.Write(entry.Name, 0, entry.Name.Length);
		_output.Write(entry.ExtraCentral, 0, entry.ExtraCentral.Length);
	}

	byte[] BuildLocalExtra()
	{
		var extra = new byte[32];
		WriteUx(extra.AsSpan(0, 15));
		// UT local: mtime + atime + ctime
		BinaryPrimitives.WriteUInt16LittleEndian(extra.AsSpan(15, 2), 0x5455);
		BinaryPrimitives.WriteUInt16LittleEndian(extra.AsSpan(17, 2), 13);
		extra[19] = 0x07;
		BinaryPrimitives.WriteUInt32LittleEndian(extra.AsSpan(20, 4), _unixTime);
		BinaryPrimitives.WriteUInt32LittleEndian(extra.AsSpan(24, 4), _unixTime);
		BinaryPrimitives.WriteUInt32LittleEndian(extra.AsSpan(28, 4), _unixTime);
		return extra;
	}

	byte[] BuildCentralExtra()
	{
		var extra = new byte[24];
		WriteUx(extra.AsSpan(0, 15));
		BinaryPrimitives.WriteUInt16LittleEndian(extra.AsSpan(15, 2), 0x5455);
		BinaryPrimitives.WriteUInt16LittleEndian(extra.AsSpan(17, 2), 5);
		extra[19] = 0x01;
		BinaryPrimitives.WriteUInt32LittleEndian(extra.AsSpan(20, 4), _unixTime);
		return extra;
	}

	static void WriteUx(Span<byte> dest)
	{
		BinaryPrimitives.WriteUInt16LittleEndian(dest[..2], 0x7875);
		BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(2, 2), 11);
		dest[4] = 1;
		dest[5] = 4;
		BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(6, 4), 0);
		dest[10] = 4;
		BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(11, 4), 0);
	}

	static string NormalizeDir(string path)
	{
		var name = path.Replace('\\', '/').TrimStart('/');
		if (!name.EndsWith('/'))
			name += "/";
		return name;
	}

	void WriteU16(ushort value)
	{
		Span<byte> buf = stackalloc byte[2];
		BinaryPrimitives.WriteUInt16LittleEndian(buf, value);
		_output.Write(buf);
	}

	void WriteU32(uint value)
	{
		Span<byte> buf = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32LittleEndian(buf, value);
		_output.Write(buf);
	}

	sealed record CentralEntry(
		byte[] Name,
		bool IsDirectory,
		ushort VersionMadeBy,
		ushort VersionNeeded,
		ushort Flags,
		uint Crc,
		uint Size,
		uint LocalOffset,
		uint ExternalAttr,
		byte[] ExtraCentral);
}

static class Crc32Ieee
{
	static readonly uint[] Table = CreateTable();

	public static uint Compute(byte[] data)
	{
		var crc = 0xFFFFFFFFu;
		foreach (var b in data)
			crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
		return crc ^ 0xFFFFFFFFu;
	}

	static uint[] CreateTable()
	{
		var table = new uint[256];
		for (uint i = 0; i < 256; i++)
		{
			var crc = i;
			for (var j = 0; j < 8; j++)
				crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
			table[i] = crc;
		}

		return table;
	}
}
