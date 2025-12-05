using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Svipp.Api.DTOs;
using Svipp.Api.Services;
using Svipp.Domain.Assignments;
using Svipp.Infrastructure;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Svipp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LocationsController : ControllerBase
{
    private readonly SvippDbContext _dbContext;
    private readonly RoadsService _roadsService;
    private readonly DirectionsService _directionsService;
    private readonly ILogger<LocationsController> _logger;

    // Enkel in-memory cache for mock-sjåfører per område, så vi ikke trenger å kalle
    // Roads API og regenerere på hvert frontend-refresh.
    // Key: sentrum for området, rundet til 4 desimaler (ca. 10-11 meter).
    // NOTE: This static cache is only suitable for demo/development environments.
    // In scaled or production deployments, each server instance will have its own cache,
    // leading to inconsistent data. For production, use a distributed cache (e.g., Redis)
    // or move caching to a dedicated service with appropriate lifetime management.
    private static readonly ConcurrentDictionary<string, (DateTime CreatedAt, List<MockDriverCacheItem> Drivers)> _mockDriverCache = new();

    // Hvor lenge mock-sjåfører for et område skal gjenbrukes før de regenereres.
    // 300 sekunder = 5 minutter.
    private const int MockDriverCacheTtlSeconds = 300;

    private class MockDriverCacheItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Rating { get; set; }
        public double PricePerKm { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public LocationsController(SvippDbContext dbContext, RoadsService roadsService, DirectionsService directionsService, ILogger<LocationsController> logger)
    {
        _dbContext = dbContext;
        _roadsService = roadsService;
        _directionsService = directionsService;
        _logger = logger;
    }

    /// <summary>
    /// Oppdaterer sist kjente posisjon for en sjåfør.
    /// </summary>
    [HttpPut("drivers/{driverId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateDriverLocation(
        [FromRoute] int driverId,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Authorization: Verify that the authenticated user has permission to update this driver's location.
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "Ugyldig autentiseringstoken",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.DriverId == driverId, cancellationToken);

        if (driver is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = $"Fant ingen sjåfør med id={driverId}.",
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        // Authorization check: Verify that the driver belongs to the authenticated user
        if (driver.UserId.HasValue && driver.UserId.Value != userId.Value)
        {
            _logger.LogWarning(
                "Authorization denied: User {UserId} attempted to update location for driver {DriverId} owned by user {DriverUserId}",
                userId, driverId, driver.UserId);
            return Forbid();
        }

        // If driver doesn't have a UserId yet, log a warning
        if (!driver.UserId.HasValue)
        {
            _logger.LogWarning(
                "Driver {DriverId} does not have an associated UserId. Location update allowed but should be linked to a user.",
                driverId);
        }

        driver.CurrentLatitude = request.Latitude;
        driver.CurrentLongitude = request.Longitude;
        driver.LastLocationUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            driverId,
            driver.CurrentLatitude,
            driver.CurrentLongitude,
            driver.LastLocationUpdatedAt
        });
    }

    /// <summary>
    /// Oppdaterer sist kjente posisjon for en kunde.
    /// </summary>
    [HttpPut("customers/{customerId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateCustomerLocation(
        [FromRoute] int customerId,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Authorization: Verify that the authenticated user has permission to update this customer's location.
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "Ugyldig autentiseringstoken",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (customer is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = $"Fant ingen kunde med id={customerId}.",
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        // Authorization check: Verify that the customer belongs to the authenticated user
        if (customer.UserId.HasValue && customer.UserId.Value != userId.Value)
        {
            _logger.LogWarning(
                "Authorization denied: User {UserId} attempted to update location for customer {CustomerId} owned by user {CustomerUserId}",
                userId, customerId, customer.UserId);
            return Forbid();
        }

        // If customer doesn't have a UserId yet, log a warning
        if (!customer.UserId.HasValue)
        {
            _logger.LogWarning(
                "Customer {CustomerId} does not have an associated UserId. Location update allowed but should be linked to a user.",
                customerId);
        }

        customer.CurrentLatitude = request.Latitude;
        customer.CurrentLongitude = request.Longitude;
        customer.LastLocationUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            customerId,
            customer.CurrentLatitude,
            customer.CurrentLongitude,
            customer.LastLocationUpdatedAt
        });
    }

    /// <summary>
    /// Returnerer en liste med mock-sjåfører rundt en gitt posisjon.
    /// Brukes for å simulere nærliggende sjåfører på veier (kun for demo/dev).
    /// </summary>
    [HttpGet("mock-drivers")]
    [AllowAnonymous] // Midlertidig åpnet for demo - kan fjernes senere når autentisering er på plass
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<object>>> GetMockDrivers(
        [FromQuery, System.ComponentModel.DataAnnotations.Range(-90, 90)] double latitude,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(-180, 180)] double longitude,
        [FromQuery] int count = 5,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0) count = 5;
        if (count > 20) count = 20;

        var center = (lat: latitude, lng: longitude);

        // Lag cache-key basert på posisjon, rundet til 3 desimaler (ca. 100 meter presisjon).
        // Dette sikrer at brukere i samme område deler cache, mens brukere i forskjellige
        // områder får sine egne sjåfører. 3 desimaler = ca. 100m radius per cache-entry.
        var roundedLat = Math.Round(latitude, 3);
        var roundedLng = Math.Round(longitude, 3);
        var cacheKey = $"mock-drivers-{roundedLat:F3}-{roundedLng:F3}";

        var names = new[]
        {
            "Ola Nordmann",
            "Kari Hansen",
            "Erik Johansen",
            "Ingrid Olsen",
            "Lars Pedersen",
            "Mari Berg",
            "Tommy Larsen",
            "Sofie Nilsen"
        };

        // Forsøk å hente fra cache først
        if (_mockDriverCache.TryGetValue(cacheKey, out var cached))
        {
            var ageSeconds = (DateTime.UtcNow - cached.CreatedAt).TotalSeconds;
            _logger.LogInformation(
                "Cache hit for {CacheKey}. Age: {AgeSeconds:F1}s, TTL: {TtlSeconds}s, Cached drivers: {Count}, Requested: {Requested}",
                cacheKey, ageSeconds, MockDriverCacheTtlSeconds, cached.Drivers.Count, count);

            if (ageSeconds <= MockDriverCacheTtlSeconds && cached.Drivers.Count >= count)
            {
                _logger.LogInformation("Returning {Count} drivers from cache", count);
                var cachedSelection = cached.Drivers
                    .OrderBy(d => d.Id)
                    .Take(count)
                    .Select(d => new
                    {
                        id = d.Id,
                        name = d.Name,
                        rating = d.Rating,
                        pricePerKm = d.PricePerKm,
                        position = new
                        {
                            latitude = d.Latitude,
                            longitude = d.Longitude
                        }
                    })
                    .ToList();

                return Ok(cachedSelection);
            }
            else
            {
                _logger.LogWarning(
                    "Cache expired or insufficient drivers. Age: {AgeSeconds:F1}s (max {TtlSeconds}s), Cached: {CachedCount}, Requested: {Requested}",
                    ageSeconds, MockDriverCacheTtlSeconds, cached.Drivers.Count, count);
            }
        }
        else
        {
            _logger.LogInformation("Cache miss for {CacheKey}. Generating new drivers.", cacheKey);
        }

        var drivers = new List<object>(count);
        var cacheDrivers = new List<MockDriverCacheItem>(count);
        var rnd = new Random();

        // Prøv flere ganger for å få punkter som faktisk ligger på vei
        // (Roads API kan falle tilbake til rå-koordinat hvis det ikke finnes vei i nærheten)
        var maxAttempts = count * 5;
        var attempts = 0;

        while (drivers.Count < count && attempts < maxAttempts)
        {
            attempts++;

            // Generer et tilfeldig punkt 0.2-1.0 km unna kunden for å holde dem "i nærheten"
            var distanceKm = 0.2 + rnd.NextDouble() * 0.8;
            var angle = rnd.NextDouble() * 2 * Math.PI;

            // Konverter avstand/vinkel til lat/lng-offset (ca. 1 grad ≈ 111 km)
            var latOffset = (distanceKm * Math.Cos(angle)) / 111d;
            var lngOffset = (distanceKm * Math.Sin(angle)) /
                            (111d * Math.Cos(center.lat * Math.PI / 180d));

            var rawLat = center.lat + latOffset;
            var rawLng = center.lng + lngOffset;

            // Prøv å snappe til nærmeste vei via Roads API, faller tilbake til rå-koordinat hvis ikke mulig
            var (driverLat, driverLng) =
                await _roadsService.SnapToRoadAsync(rawLat, rawLng, cancellationToken);

            // Hvis vi ikke fikk noe bedre enn rå-koordinaten (innenfor en veldig liten epsilon),
            // hopper vi over dette punktet for å unngå sjåfører "midt i sjøen"
            const double epsilon = 1e-5; // ca. 1 meter
            if (Math.Abs(driverLat - rawLat) < epsilon && Math.Abs(driverLng - rawLng) < epsilon)
            {
                continue;
            }

            var name = names[rnd.Next(names.Length)];
            var rating = 3.5 + rnd.NextDouble() * 1.5;      // 3.5 - 5.0
            var pricePerKm = 15 + rnd.NextDouble() * 10;    // 15 - 25 kr/km

            // Use consistent ID based on current index
            var driverId = drivers.Count + 1;

            cacheDrivers.Add(new MockDriverCacheItem
            {
                Id = driverId,
                Name = name,
                Rating = rating,
                PricePerKm = pricePerKm,
                Latitude = driverLat,
                Longitude = driverLng
            });

            drivers.Add(new
            {
                id = driverId,
                name,
                rating,
                pricePerKm,
                position = new
                {
                    latitude = driverLat,
                    longitude = driverLng
                }
            });
        }

        // Lagre i cache for dette området
        _mockDriverCache[cacheKey] = (DateTime.UtcNow, cacheDrivers);
        _logger.LogInformation("Cached {Count} drivers with key {CacheKey}", cacheDrivers.Count, cacheKey);

        return Ok(drivers);
    }

    /// <summary>
    /// Returnerer ruteinformasjon (avstand, varighet, polyline) mellom to punkter.
    /// Brukes av frontend for å tegne rute og vise beregnet tid.
    /// </summary>
    [HttpGet("route")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetRoute(
        [FromQuery] double originLatitude,
        [FromQuery] double originLongitude,
        [FromQuery] double destinationLatitude,
        [FromQuery] double destinationLongitude,
        CancellationToken cancellationToken = default)
    {
        // Validate coordinate ranges
        if (originLatitude < -90 || originLatitude > 90)
            return BadRequest("originLatitude must be between -90 and 90.");
        if (originLongitude < -180 || originLongitude > 180)
            return BadRequest("originLongitude must be between -180 and 180.");
        if (destinationLatitude < -90 || destinationLatitude > 90)
            return BadRequest("destinationLatitude must be between -90 and 90.");
        if (destinationLongitude < -180 || destinationLongitude > 180)
            return BadRequest("destinationLongitude must be between -180 and 180.");

        var (distanceKm, durationSeconds, encodedPolyline) =
            await _directionsService.GetRouteAsync(
                originLatitude,
                originLongitude,
                destinationLatitude,
                destinationLongitude,
                cancellationToken);

        return Ok(new
        {
            distanceKm,
            durationSeconds,
            encodedPolyline
        });
    }

    private Guid? GetUserIdFromToken()
    {
        var user = HttpContext.User;
        if (user?.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                      ?? user.FindFirst("sub")
                      ?? user.FindFirst("userId")
                      ?? user.FindFirst("id");

        if (idClaim == null)
        {
            return null;
        }

        if (Guid.TryParse(idClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}


