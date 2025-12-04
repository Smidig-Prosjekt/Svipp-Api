using System;
using Microsoft.AspNetCore.Mvc;
using Svipp.Api.DTOs;

namespace Svipp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    // Antatt volum (i liter) som trengs for én el‑sparkesykkel.
    // Holdt bevisst konservativ for sikkerhetsmargin.
    private const int ScooterVolumeLiters = 150;

    [HttpPost("check-capacity")]
    [ProducesResponseType(typeof(CheckVehicleCapacityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<CheckVehicleCapacityResponse> CheckCapacity([FromBody] CheckVehicleCapacityRequest request)
    {
        // Normaliser mellomrom: trim start/slutt og erstatt multiple mellomrom med ett
        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            request.Brand = System.Text.RegularExpressions.Regex.Replace(request.Brand.Trim(), @"\s+", " ");
        }

        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            request.Model = System.Text.RegularExpressions.Regex.Replace(request.Model.Trim(), @"\s+", " ");
        }

        // Grunnleggende validering av påkrevde felter
        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            ModelState.AddModelError(nameof(request.Brand), "Brand is required.");
        }
        else if (request.Brand.Length is < 2 or > 50)
        {
            ModelState.AddModelError(nameof(request.Brand), "Brand must be between 2 and 50 characters.");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Brand, "^[\\p{L}0-9 .\\-]+$"))
        {
            ModelState.AddModelError(nameof(request.Brand),
                "Brand may only contain letters, numbers, spaces, dots and hyphens.");
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            ModelState.AddModelError(nameof(request.Model), "Model is required.");
        }
        else if (request.Model.Length is < 1 or > 100)
        {
            ModelState.AddModelError(nameof(request.Model), "Model must be between 1 and 100 characters.");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Model, "^[\\p{L}0-9 .\\-]+$"))
        {
            ModelState.AddModelError(nameof(request.Model),
                "Model may only contain letters, numbers, spaces, dots and hyphens.");
        }

        // Årsmodell: 1970 - inneværende år
        var currentYear = DateTime.UtcNow.Year;
        if (request.Year < 1970 || request.Year > currentYear)
        {
            ModelState.AddModelError(nameof(request.Year),
                $"Year must be between 1970 and {currentYear}.");
        }

        // Volum må alltid oppgis eksplisitt nå
        int? effectiveTrunkVolume = null;
        if (request.TrunkVolumeLiters is null)
        {
            ModelState.AddModelError(nameof(request.TrunkVolumeLiters),
                "TrunkVolumeLiters is required.");
        }
        else if (request.TrunkVolumeLiters <= 0)
        {
            ModelState.AddModelError(nameof(request.TrunkVolumeLiters),
                "TrunkVolumeLiters must be greater than 0.");
        }
        else if (request.TrunkVolumeLiters > 5000)
        {
            // Harde grenser for å beskytte mot urimelig input.
            ModelState.AddModelError(nameof(request.TrunkVolumeLiters),
                "TrunkVolumeLiters is unreasonably large. Max allowed is 5000 liters.");
        }
        else
        {
            effectiveTrunkVolume = request.TrunkVolumeLiters.Value;
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // På dette tidspunktet har vi et gyldig volum å forholde oss til.
        var fits = effectiveTrunkVolume >= ScooterVolumeLiters;

        var reason = fits
            ? $"Sparkesykkelen får plass: estimert nødvendig volum er {ScooterVolumeLiters} L " +
              $"og bilen har minst {effectiveTrunkVolume} L tilgjengelig."
            : $"Sparkesykkelen får trolig ikke plass: estimert nødvendig volum er {ScooterVolumeLiters} L " +
              $"mens bilen kun har ca. {effectiveTrunkVolume} L tilgjengelig.";

        var response = new CheckVehicleCapacityResponse
        {
            Fits = fits,
            Reason = reason,
            ScooterVolumeLiters = ScooterVolumeLiters,
            VehicleTrunkVolumeLiters = effectiveTrunkVolume
        };

        return Ok(response);
    }
}


