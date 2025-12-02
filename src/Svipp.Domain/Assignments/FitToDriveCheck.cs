using System;

namespace Svipp.Domain.Assignments;

public class FitToDriveCheck
{
    public int FitToDriveCheckId { get; set; }

    public int BookingId { get; set; }

    /// <summary>
    /// True hvis kunden vurderes som ikke kjørbar før turen.
    /// </summary>
    public bool CustomerNotFitToDrive { get; set; }

    /// <summary>
    /// True hvis nøkler er mottatt før turen.
    /// </summary>
    public bool KeysReceived { get; set; }

    /// <summary>
    /// Tidspunkt når sjekken ble bekreftet.
    /// </summary>
    public DateTime ConfirmedAt { get; set; }

    /// <summary>
    /// Hvem som bekreftet sjekken (for eksempel førerens navn eller ID).
    /// </summary>
    public string ConfirmedBy { get; set; } = null!;

    // Navigation
    public Booking Booking { get; set; } = null!;
}


