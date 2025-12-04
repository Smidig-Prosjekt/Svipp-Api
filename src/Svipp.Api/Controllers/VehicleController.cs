using Microsoft.AspNetCore.Mvc;
using Svipp.Api.DTOs;
using Svipp.Api.Services;

namespace Svipp.Api.Controllers;

/// <summary>
/// Vehicle capacity related endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VehicleController : ControllerBase
{
    private readonly IVehicleCapacityService _capacityService;
    private readonly ILogger<VehicleController> _logger;

    public VehicleController(
        IVehicleCapacityService capacityService,
        ILogger<VehicleController> logger)
    {
        _capacityService = capacityService;
        _logger = logger;
    }

    /// <summary>
    /// Check if the folded Svipp scooter fits into a car trunk.
    /// </summary>
    /// <param name="request">Trunk dimensions and optional car model information.</param>
    /// <returns>Whether the scooter fits and an explanation.</returns>
    /// <response code="200">Capacity successfully evaluated</response>
    /// <response code="400">Validation error in the request payload</response>
    [HttpPost("check-capacity")]
    [ProducesResponseType(typeof(VehicleCapacityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<VehicleCapacityResponse> CheckCapacity([FromBody] VehicleCapacityRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                Detail = "One or more fields contain invalid data",
                StatusCode = StatusCodes.Status400BadRequest,
                Errors = errors
            });
        }

        _logger.LogInformation(
            "Checking vehicle capacity for {Make} {Model} {Year} with trunk {Length}x{Width}x{Height} cm",
            request.Make ?? "Unknown make",
            request.Model ?? "Unknown model",
            request.Year?.ToString() ?? "Unknown year",
            request.TrunkLengthCm,
            request.TrunkWidthCm,
            request.TrunkHeightCm);

        var result = _capacityService.CheckCapacity(
            request.TrunkLengthCm,
            request.TrunkWidthCm,
            request.TrunkHeightCm);

        var response = new VehicleCapacityResponse
        {
            Fits = result.Fits,
            Reason = result.Reason,
            RequiredVolumeLiters = result.RequiredVolumeLiters,
            ProvidedVolumeLiters = result.ProvidedVolumeLiters
        };

        return Ok(response);
    }
}


