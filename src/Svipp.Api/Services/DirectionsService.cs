using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Svipp.Api.Services;

/// <summary>
/// Wrapper rundt Google Directions API for å hente rute, avstand og varighet.
/// </summary>
public class DirectionsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<DirectionsService> _logger;

    public DirectionsService(HttpClient httpClient, IConfiguration configuration, ILogger<DirectionsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey =
            Environment.GetEnvironmentVariable("GOOGLE_MAPS_API") ??
            configuration["GoogleMaps:ApiKey"] ??
            throw new InvalidOperationException(
                "Google Maps API key mangler for DirectionsService. Sett GOOGLE_MAPS_API env eller GoogleMaps:ApiKey i appsettings.");
    }

    public async Task<(double distanceKm, int durationSeconds, string encodedPolyline)> GetRouteAsync(
        double originLat,
        double originLng,
        double destLat,
        double destLng,
        CancellationToken cancellationToken = default)
    {
        var oLat = originLat.ToString(CultureInfo.InvariantCulture);
        var oLng = originLng.ToString(CultureInfo.InvariantCulture);
        var dLat = destLat.ToString(CultureInfo.InvariantCulture);
        var dLng = destLng.ToString(CultureInfo.InvariantCulture);

        // WARNING: The API key is included in the URL. Logging this URL may expose the API key in log files.
        // Ensure that HTTP client logging and error logs do not record this URL, or redact the 'key' query parameter if logging is necessary.
        var url =
            $"https://maps.googleapis.com/maps/api/directions/json?origin={oLat},{oLng}&destination={dLat},{dLng}&mode=driving&key={_apiKey}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Directions API returnerte {StatusCode} for rute ({OriginLat}, {OriginLng}) -> ({DestLat}, {DestLng}): {Error}",
                response.StatusCode,
                originLat,
                originLng,
                destLat,
                destLng,
                errorContent);
            throw new InvalidOperationException(
                $"Directions API returnerte {response.StatusCode}. Sjekk at Directions API er aktivert i Google Cloud Console.");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (json.ValueKind == JsonValueKind.Undefined || json.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException("Directions API returnerte ugyldig respons");
        }

        if (!json.TryGetProperty("status", out var statusElement) || statusElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Directions API-respons mangler 'status' property.");
        }
        var status = statusElement.GetString();
        if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = json.TryGetProperty("error_message", out var errorMsg)
                ? errorMsg.GetString()
                : "Ukjent feil";
            _logger.LogWarning(
                "Directions API returnerte status '{Status}' for rute ({OriginLat}, {OriginLng}) -> ({DestLat}, {DestLng}): {ErrorMessage}",
                status,
                originLat,
                originLng,
                destLat,
                destLng,
                errorMessage);
            throw new InvalidOperationException(
                $"Directions API returnerte status '{status}': {errorMessage}");
        }

        var routes = json.GetProperty("routes");
        if (routes.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Directions API returnerte ingen ruter.");
        }

        var route = routes[0];
        var legs = route.GetProperty("legs");
        if (legs.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Directions API mangler legs i ruten.");
        }

        var leg = legs[0];

        var distance = leg.GetProperty("distance").GetProperty("value").GetDouble(); // meter
        var duration = leg.GetProperty("duration").GetProperty("value").GetInt32();  // sekunder

        var overviewPolyline = route.GetProperty("overview_polyline").GetProperty("points").GetString()
                               ?? throw new InvalidOperationException("Directions API mangler overview_polyline.");

        return (distance / 1000.0, duration, overviewPolyline);
    }
}


