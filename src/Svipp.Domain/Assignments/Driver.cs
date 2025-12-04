using System.Collections.Generic;

namespace Svipp.Domain.Assignments;

public class Driver
{
    public int DriverId { get; set; }
    public string Name { get; set; } = null!;
    public string AvailabilityStatus { get; set; } = null!; // possibly an enum later

    /// <summary>
    /// Real-time or last known position for matching and map display.
    /// </summary>
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public DateTime? LastLocationUpdatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    // Hvilke sparkesykler sjåføren disponerer
    public ICollection<Scooter> Scooters { get; set; } = new List<Scooter>();
}



