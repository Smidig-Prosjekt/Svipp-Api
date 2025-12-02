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
    /// Registrerer eller oppdaterer digital pre-ride sjekkliste (fit-to-drive) for en tur.
    /// </summary>
    [HttpPost("{rideId:int}/fit-to-drive-check")]
    public async Task<ActionResult<FitToDriveCheckResponse>> UpsertFitToDriveCheck(
        [FromRoute] int rideId,
        [FromBody] FitToDriveCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var booking = await _dbContext.Bookings
            .Include(b => b.FitToDriveCheck)
            .FirstOrDefaultAsync(b => b.BookingId == rideId, cancellationToken);

        if (booking is null)
        {
            return NotFound($"Fant ingen tur/booking med id={rideId}.");
        }

        var now = DateTime.UtcNow;
        var confirmedAt = request.ConfirmedAt ?? now;

        if (booking.FitToDriveCheck is null)
        {
            booking.FitToDriveCheck = new FitToDriveCheck
            {
                BookingId = booking.BookingId,
                CustomerNotFitToDrive = request.CustomerNotFitToDrive,
                KeysReceived = request.KeysReceived,
                ConfirmedAt = confirmedAt,
                ConfirmedBy = request.ConfirmedBy.Trim()
            };

            _dbContext.FitToDriveChecks.Add(booking.FitToDriveCheck);
        }
        else
        {
            booking.FitToDriveCheck.CustomerNotFitToDrive = request.CustomerNotFitToDrive;
            booking.FitToDriveCheck.KeysReceived = request.KeysReceived;
            booking.FitToDriveCheck.ConfirmedAt = confirmedAt;
            booking.FitToDriveCheck.ConfirmedBy = request.ConfirmedBy.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new FitToDriveCheckResponse(
            booking.BookingId,
            booking.FitToDriveCheck.FitToDriveCheckId,
            booking.FitToDriveCheck.CustomerNotFitToDrive,
            booking.FitToDriveCheck.KeysReceived,
            booking.FitToDriveCheck.ConfirmedAt,
            booking.FitToDriveCheck.ConfirmedBy
        );

        return Ok(response);
    }
}

public class FitToDriveCheckRequest
{
    /// <summary>
    /// True hvis kunden vurderes som ikke kjørbar.
    /// </summary>
    [Required]
    public bool CustomerNotFitToDrive { get; set; }

    /// <summary>
    /// True hvis nøkler er mottatt.
    /// </summary>
    [Required]
    public bool KeysReceived { get; set; }

    /// <summary>
    /// Tidspunkt for bekreftelse. Hvis null brukes nåværende tidspunkt (UTC).
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Hvem som bekreftet sjekken (navn/ID).
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ConfirmedBy { get; set; } = null!;
}

public record FitToDriveCheckResponse(
    int BookingId,
    int FitToDriveCheckId,
    bool CustomerNotFitToDrive,
    bool KeysReceived,
    DateTime ConfirmedAt,
    string ConfirmedBy
);


