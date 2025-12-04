using System;
using System.Text.RegularExpressions;
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

    // Compiled regex for performance
    private static readonly Regex WhitespaceNormalizeRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex AllowedCharactersRegex = new(@"^[\p{L}0-9 .\-]+$", RegexOptions.Compiled);

    [HttpPost("check-capacity")]
    [ProducesResponseType(typeof(CheckVehicleCapacityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<CheckVehicleCapacityResponse> CheckCapacity([FromBody] CheckVehicleCapacityRequest request)
    {
        // Normaliser mellomrom: trim start/slutt og erstatt multiple mellomrom med ett
        // Bruk lokale variabler for å unngå mutering av request-objektet
        var normalizedBrand = NormalizeWhitespace(request.Brand);
        var normalizedModel = NormalizeWhitespace(request.Model);

        // Valider brand og model
        ValidateVehicleStringField(normalizedBrand, nameof(request.Brand), 2, 50);
        ValidateVehicleStringField(normalizedModel, nameof(request.Model), 1, 100);

        // Årsmodell: 1970 - inneværende år
        var currentYear = DateTime.UtcNow.Year;
        if (request.Year is null)
        {
            ModelState.AddModelError(nameof(request.Year), "Year is required.");
        }
        else if (request.Year < 1970 || request.Year > currentYear)
        {
            ModelState.AddModelError(nameof(request.Year),
                $"Year must be between 1970 and {currentYear}.");
        }

        // Volum validering
        int effectiveTrunkVolume = request.TrunkVolumeLiters;
        if (effectiveTrunkVolume <= 0)
        {
            ModelState.AddModelError(nameof(request.TrunkVolumeLiters),
                "TrunkVolumeLiters must be greater than 0.");
        }
        else if (effectiveTrunkVolume > 5000)
        {
            ModelState.AddModelError(nameof(request.TrunkVolumeLiters),
                "TrunkVolumeLiters is unreasonably large. Max allowed is 5000 liters.");
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

    private static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        return WhitespaceNormalizeRegex.Replace(value.Trim(), " ");
    }

    private void ValidateVehicleStringField(string? value, string fieldName, int minLength, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ModelState.AddModelError(fieldName, $"{fieldName} is required.");
            return;
        }

        if (value.Length < minLength || value.Length > maxLength)
        {
            ModelState.AddModelError(fieldName,
                $"{fieldName} must be between {minLength} and {maxLength} characters.");
            return;
        }

        if (!AllowedCharactersRegex.IsMatch(value))
        {
            ModelState.AddModelError(fieldName,
                $"{fieldName} may only contain letters, numbers, spaces, dots and hyphens.");
        }
    }
}
