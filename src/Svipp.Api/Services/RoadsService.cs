using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Svipp.Api.Services;

/// <summary>
/// Wrapper rundt Google Roads API (Snap to Roads) for å "snappe" koordinater til nærmeste vei.
/// </summary>
public class RoadsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<RoadsService> _logger;

    // En veldig enkel in-memory cache for å redusere antall kall til Roads API.
    // Key: "lat:lng" rundet til 5 desimaler (ca. 1 meter presisjon).
    // Value: snappet (lat, lng).
    private readonly ConcurrentDictionary<string, (double lat, double lng)> _cache = new();

    public RoadsService(HttpClient httpClient, IConfiguration configuration, ILogger<RoadsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Les nøkkel fra env eller config. Må være samme prosjekt/nøkkel som brukt i frontend.
        _apiKey =
            Environment.GetEnvironmentVariable("GOOGLE_MAPS_API") ??
            configuration["GoogleMaps:ApiKey"] ??
            throw new InvalidOperationException(
                "Google Maps API key mangler for RoadsService. Sett GOOGLE_MAPS_API env eller GoogleMaps:ApiKey i appsettings.");
    }

    /// <summary>
    /// Snapper et punkt til nærmeste vei ved hjelp av Google Roads API.
    /// </summary>
    public async Task<(double lat, double lng)> SnapToRoadAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        // Først: sjekk enkel cache for å unngå unødvendige kall.
        var roundedLat = Math.Round(latitude, 5);
        var roundedLng = Math.Round(longitude, 5);
        var cacheKey = $"{roundedLat}:{roundedLng}";

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var latStr = latitude.ToString(CultureInfo.InvariantCulture);
        var lngStr = longitude.ToString(CultureInfo.InvariantCulture);

        // Google Roads API forventer formatet: path=lat,lng eller path=lat1,lng1|lat2,lng2
        var url = $"https://roads.googleapis.com/v1/snapToRoads?path={latStr},{lngStr}&key={_apiKey}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Les feilmelding fra Google API for debugging
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Roads API returnerte {StatusCode} for koordinat ({Lat}, {Lng}): {Error}",
                response.StatusCode,
                latitude,
                longitude,
                errorContent);

            // Hvis Roads API gir 4xx/5xx (f.eks. manglende rettigheter/kvoter),
            // faller vi stille tilbake til original posisjon i stedet for å kaste.
            return (latitude, longitude);
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (!json.TryGetProperty("snappedPoints", out var snappedPoints) ||
            snappedPoints.GetArrayLength() == 0)
        {
            // Fallback: returner original posisjon hvis Roads API ikke gir noe
            return (latitude, longitude);
        }

        var location = snappedPoints[0].GetProperty("location");
        var snappedLat = location.GetProperty("latitude").GetDouble();
        var snappedLng = location.GetProperty("longitude").GetDouble();

        var snapped = (snappedLat, snappedLng);
        _cache[cacheKey] = snapped;

        return snapped;
    }
}


