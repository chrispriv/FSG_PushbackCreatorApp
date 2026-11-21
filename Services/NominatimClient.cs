using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSG_PushbackCreator.Services;

public static class NominatimClient
{
	static readonly HttpClient Http = CreateClient();
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	static HttpClient CreateClient()
	{
		var http = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15)
		};
		http.DefaultRequestHeaders.UserAgent.ParseAdd(
			"PushbackCreatorApp/2.0 (https://github.com/chrispriv/FSG_PushbackCreatorApp)");
		http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		return http;
	}

	public static async Task<IReadOnlyList<NominatimHit>> SearchAsync(string query, CancellationToken cancellationToken = default)
	{
		var q = (query ?? string.Empty).Trim();
		if (q.Length < 2)
			return [];

		var url =
			"https://nominatim.openstreetmap.org/search?format=jsonv2&limit=8&addressdetails=0&q=" +
			Uri.EscapeDataString(q);
		using var response = await Http.GetAsync(url, cancellationToken).ConfigureAwait(true);
		response.EnsureSuccessStatusCode();
		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(true);
		var hits = await JsonSerializer.DeserializeAsync<List<NominatimHit>>(stream, JsonOptions, cancellationToken)
			.ConfigureAwait(true);
		return hits ?? [];
	}
}

public sealed class NominatimHit
{
	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; } = string.Empty;

	[JsonPropertyName("lat")]
	public string Lat { get; set; } = string.Empty;

	[JsonPropertyName("lon")]
	public string Lon { get; set; } = string.Empty;

	public bool TryCoordinates(out double latitude, out double longitude)
	{
		latitude = 0;
		longitude = 0;
		return double.TryParse(Lat, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out latitude) &&
		       double.TryParse(Lon, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out longitude);
	}

	public string ListSummary => DisplayName;
}
