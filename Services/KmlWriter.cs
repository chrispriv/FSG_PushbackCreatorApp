using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class KmlWriter
{
	static readonly XNamespace KmlNs = "http://www.opengis.net/kml/2.2";

	public static byte[] Write(TmeProject project, string? documentName = null)
	{
		var title = PackageNames.Sanitize(documentName ?? project.PackageFolderName);
		var folders = project.Airports
			.OrderBy(a => a.TmeCode, StringComparer.OrdinalIgnoreCase)
			.Select(AirportFolder);

		XNamespace ns = KmlNs;
		var doc = new XDocument(
			new XDeclaration("1.0", "UTF-8", "yes"),
			new XElement(ns + "kml",
				new XElement(ns + "Document",
					new XElement(ns + "name", title),
					new XElement(ns + "description", "Helipad and pushback positions from Pushback Creator App."),
					folders)));

		var settings = new XmlWriterSettings
		{
			Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
			Indent = true,
			IndentChars = "  ",
			NewLineChars = "\r\n",
			OmitXmlDeclaration = false
		};
		using var stream = new MemoryStream();
		using (var writer = XmlWriter.Create(stream, settings))
			doc.Save(writer);
		return stream.ToArray();
	}

	static XElement AirportFolder(AirportEntry airport)
	{
		XNamespace ns = KmlNs;
		var folderName = string.IsNullOrWhiteSpace(airport.Name)
			? airport.TmeCode
			: $"{airport.TmeCode} — {airport.Name}";

		var children = new List<XElement>
		{
			new(ns + "name", folderName),
			new(ns + "description", airport.KindLabel)
		};

		if (airport.HelipadLatitude is { } hlat && airport.HelipadLongitude is { } hlon)
		{
			children.Add(Placemark(
				"Helipad H01",
				$"ICAO {airport.Icao4}\nTME {airport.TmeCode}\nHeading {FormatNumber(airport.HelipadHeading)} deg\nRadius {FormatNumber(AirportEntry.DefaultHelipadRadius)} m",
				hlon,
				hlat));
		}

		foreach (var slot in airport.ParkingSlots)
		{
			if (slot.Latitude is not { } plat || slot.Longitude is not { } plon)
				continue;

			var label = string.IsNullOrWhiteSpace(slot.Name) ? "Pushback" : $"Pushback {slot.Name}";
			children.Add(Placemark(
				label,
				$"ICAO {airport.Icao4}\nTME {airport.TmeCode}\nHeading {FormatNumber(slot.Heading)} deg\nSize {FormatNumber(slot.Size)} m",
				plon,
				plat));
		}

		return new XElement(ns + "Folder", children);
	}

	static XElement Placemark(string name, string description, double longitude, double latitude)
	{
		XNamespace ns = KmlNs;
		var coordinates = string.Create(CultureInfo.InvariantCulture, $"{longitude:0.########},{latitude:0.########},0");
		return new XElement(ns + "Placemark",
			new XElement(ns + "name", name),
			new XElement(ns + "description", description),
			new XElement(ns + "Point",
				new XElement(ns + "coordinates", coordinates)));
	}

	static string FormatNumber(double value) =>
		value.ToString("0.###", CultureInfo.InvariantCulture);
}
