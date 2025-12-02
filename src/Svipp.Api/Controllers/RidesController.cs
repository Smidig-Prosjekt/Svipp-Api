using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Svipp.Domain.Assignments;
using Svipp.Infrastructure;

namespace Svipp.Api.Controllers;

[ApiController]
[Route("api/rides")]
public class RidesController : ControllerBase
{
    private readonly SvippDbContext _dbContext;

    public RidesController(SvippDbContext dbContext)
    {
        _dbContext = dbContext;
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


