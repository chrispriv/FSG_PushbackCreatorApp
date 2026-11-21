using System.Text.Json;
using System.Text.Json.Serialization;
using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class MapSceneBuilder
{
	public const string HelipadId = "helipad";
	public const string TowerId = "tower";
	public const string WindsockId = "windsock";

	public static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static string ParkingId(Guid id) => $"parking:{id:N}";

	public static bool TryParseParkingId(string? id, out Guid parkingId)
	{
		parkingId = Guid.Empty;
		if (string.IsNullOrEmpty(id) || !id.StartsWith("parking:", StringComparison.Ordinal))
			return false;
		return Guid.TryParse(id["parking:".Length..], out parkingId);
	}

	public static MapScene FromAirport(AirportEntry airport, string? selectedId, bool fit, MapViewState? view = null)
	{
		var scene = new MapScene
		{
			SelectedId = selectedId,
			Fit = fit,
			View = view,
			AirportLocated = airport.HelipadLatitude is not null && airport.HelipadLongitude is not null
		};

		scene.Items.Add(new MapItem
		{
			Id = HelipadId,
			Kind = "helipad",
			Label = "Helipad",
			Lat = airport.HelipadLatitude,
			Lon = airport.HelipadLongitude,
			Heading = airport.HelipadHeading,
			RadiusM = AirportEntry.DefaultHelipadRadius
		});

		if (!scene.AirportLocated)
			return scene;

		if (airport.Kind != AirportKind.Heliport)
		{
			scene.Items.Add(new MapItem
			{
				Id = TowerId,
				Kind = "tower",
				Label = "Tower",
				Lat = airport.TowerLatitude,
				Lon = airport.TowerLongitude,
				Heading = airport.HelipadHeading,
				LengthM = 8,
				WidthM = 8
			});

			foreach (var slot in airport.ParkingSlots)
			{
				scene.Items.Add(new MapItem
				{
					Id = ParkingId(slot.Id),
					Kind = "parking",
					Label = string.IsNullOrWhiteSpace(slot.Name) ? "Parking" : slot.Name,
					Lat = slot.Latitude,
					Lon = slot.Longitude,
					Heading = slot.Heading,
					LengthM = slot.Size,
					WidthM = Math.Max(8, slot.Size * 0.45)
				});
			}
		}
		else
		{
			scene.Items.Add(new MapItem
			{
				Id = WindsockId,
				Kind = "windsock",
				Label = "Windsock",
				Lat = airport.WindsockLatitude,
				Lon = airport.WindsockLongitude,
				Heading = airport.HelipadHeading,
				LengthM = 6,
				WidthM = 6
			});
		}

		return scene;
	}

	public static string ToJson(MapScene scene) => JsonSerializer.Serialize(scene, JsonOptions);
}

public sealed class MapScene
{
	public string? SelectedId { get; set; }
	public bool Fit { get; set; }
	public bool AirportLocated { get; set; }
	public MapViewState? View { get; set; }
	public List<MapItem> Items { get; set; } = [];
}

public sealed class MapViewState
{
	public double Lat { get; set; }
	public double Lon { get; set; }
	public int Zoom { get; set; } = 16;
}

public sealed class MapItem
{
	public string Id { get; set; } = string.Empty;
	public string Kind { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;
	public double? Lat { get; set; }
	public double? Lon { get; set; }
	public double Heading { get; set; }
	public double RadiusM { get; set; }
	public double LengthM { get; set; }
	public double WidthM { get; set; }
}

public sealed class MapObjectRow
{
	public string Id { get; init; } = string.Empty;
	public string Title { get; init; } = string.Empty;
}

public sealed class MapJsMessage
{
	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("id")]
	public string? Id { get; set; }

	[JsonPropertyName("lat")]
	public double Lat { get; set; }

	[JsonPropertyName("lon")]
	public double Lon { get; set; }

	[JsonPropertyName("heading")]
	public double Heading { get; set; }
}
