using System;

namespace Svipp.Domain.Assignments;

// Digital ansvarsoverførings-sjekk før turstart
public class HandoverConfirmation
{
    public int HandoverConfirmationId { get; set; }

    public int BookingId { get; set; }

    /// <summary>
    /// True hvis kunden ikke skal kjøre selv.
    /// </summary>
    public bool CustomerWillNotDrive { get; set; }

    /// <summary>
    /// True hvis nøkler er overlevert.
    /// </summary>
    public bool KeysHandedOver { get; set; }

    /// <summary>
    /// Tidspunkt når ansvarsoverføringen ble bekreftet.
    /// </summary>
    public DateTime ConfirmedAt { get; set; }

    /// <summary>
    /// Hvilken sjåfør som bekreftet (for eksempel navn eller ID).
    /// </summary>
    public string ConfirmedByDriver { get; set; } = null!;

    // Navigation
    public Booking Booking { get; set; } = null!;
}


