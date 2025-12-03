using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Svipp.Api.DTOs;
using Svipp.Domain.Assignments;
using Svipp.Infrastructure;

namespace Svipp.Api.Controllers;

[ApiController]
[Route("api/rides")]
[Authorize]
[Produces("application/json")]
public class RidesController : ControllerBase
{
    private readonly SvippDbContext _dbContext;

    public RidesController(SvippDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Henter ansvarsoverføringsdetaljer for en gitt tur.
    /// </summary>
    [HttpGet("{rideId:int}/handover-confirmation")]
    [ProducesResponseType(typeof(HandoverConfirmationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HandoverConfirmationResponse>> GetHandoverConfirmation(
        [FromRoute] int rideId,
        CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.HandoverConfirmation)
            .FirstOrDefaultAsync(b => b.BookingId == rideId, cancellationToken);

        if (booking is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = $"Fant ingen tur/booking med id={rideId}.",
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        if (booking.HandoverConfirmation is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = $"Fant ingen ansvarsoverføring for tur med id={rideId}.",
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        var response = new HandoverConfirmationResponse(
            booking.BookingId,
            booking.HandoverConfirmation.HandoverConfirmationId,
            booking.HandoverConfirmation.CustomerWillNotDrive,
            booking.HandoverConfirmation.KeysHandedOver,
            booking.HandoverConfirmation.ConfirmedAt,
            booking.HandoverConfirmation.ConfirmedByDriver
        );

        return Ok(response);
    }

    /// <summary>
    /// Registrerer eller oppdaterer digital ansvarsoverførings-sjekk før turstart.
    /// </summary>
    [HttpPost("{rideId:int}/handover-confirmation")]
    public async Task<ActionResult<HandoverConfirmationResponse>> UpsertHandoverConfirmation(
        [FromRoute] int rideId,
        [FromBody] HandoverConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var booking = await _dbContext.Bookings
            .Include(b => b.HandoverConfirmation)
            .FirstOrDefaultAsync(b => b.BookingId == rideId, cancellationToken);

        if (booking is null)
        {
            return NotFound($"Fant ingen tur/booking med id={rideId}.");
        }

        var now = DateTime.UtcNow;
        var confirmedAt = request.ConfirmedAt ?? now;

        if (booking.HandoverConfirmation is null)
        {
            booking.HandoverConfirmation = new HandoverConfirmation
            {
                BookingId = booking.BookingId,
                CustomerWillNotDrive = request.CustomerWillNotDrive,
                KeysHandedOver = request.KeysHandedOver,
                ConfirmedAt = confirmedAt,
                ConfirmedByDriver = request.ConfirmedByDriver.Trim()
            };

            _dbContext.HandoverConfirmations.Add(booking.HandoverConfirmation);
        }
        else
        {
            booking.HandoverConfirmation.CustomerWillNotDrive = request.CustomerWillNotDrive;
            booking.HandoverConfirmation.KeysHandedOver = request.KeysHandedOver;
            booking.HandoverConfirmation.ConfirmedAt = confirmedAt;
            booking.HandoverConfirmation.ConfirmedByDriver = request.ConfirmedByDriver.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new HandoverConfirmationResponse(
            booking.BookingId,
            booking.HandoverConfirmation.HandoverConfirmationId,
            booking.HandoverConfirmation.CustomerWillNotDrive,
            booking.HandoverConfirmation.KeysHandedOver,
            booking.HandoverConfirmation.ConfirmedAt,
            booking.HandoverConfirmation.ConfirmedByDriver
        );

        return Ok(response);
    }

    /// <summary>
    /// Starter en tur etter at ansvarsoverføring er bekreftet.
    /// </summary>
    [HttpPost("{rideId:int}/start")]
    [ProducesResponseType(typeof(StartRideResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StartRideResponse>> StartRide(
        [FromRoute] int rideId,
        CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.HandoverConfirmation)
            .FirstOrDefaultAsync(b => b.BookingId == rideId, cancellationToken);

        if (booking is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = $"Fant ingen tur/booking med id={rideId}.",
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        if (booking.HandoverConfirmation is null)
        {
            return Conflict(new ErrorResponse
            {
                Message = "Kan ikke starte tur uten registrert ansvarsoverføring.",
                Detail = "Bruk endepunktet /handover-confirmation før du starter turen.",
                StatusCode = StatusCodes.Status409Conflict
            });
        }

        booking.Status = "Started";

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new StartRideResponse(booking.BookingId, booking.Status);

        return Ok(response);
    }
}

public class HandoverConfirmationRequest
{
    /// <summary>
    /// True hvis kunden ikke skal kjøre selv.
    /// </summary>
    [Required]
    public bool CustomerWillNotDrive { get; set; }

    /// <summary>
    /// True hvis nøkler er overlevert.
    /// </summary>
    [Required]
    public bool KeysHandedOver { get; set; }

    /// <summary>
    /// Tidspunkt for bekreftelse. Hvis null brukes nåværende tidspunkt (UTC).
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Hvilken sjåfør som bekreftet (navn/ID).
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ConfirmedByDriver { get; set; } = null!;
}

public record HandoverConfirmationResponse(
    int BookingId,
    int HandoverConfirmationId,
    bool CustomerWillNotDrive,
    bool KeysHandedOver,
    DateTime ConfirmedAt,
    string ConfirmedByDriver
);

public record StartRideResponse(
    int BookingId,
    string Status
);
