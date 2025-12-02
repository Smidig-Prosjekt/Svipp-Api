using System;

namespace Svipp.Domain.Assignments;

public class Booking
{
    public int BookingId { get; set; }

    public int CustomerId { get; set; }
    public int? DriverId { get; set; }    // booking can be created before driver/vehicle is assigned
    public int? VehicleId { get; set; }

    public DateTime PickupTime { get; set; }
    public DateTime DropoffTime { get; set; }
    public string Status { get; set; } = null!;     // "active" / "completed" etc.
    public decimal EstimatedPrice { get; set; }

    public int PickupLocationId { get; set; }
    public int DropoffLocationId { get; set; }

    public DateTime OrderedAt { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
    public Driver? Driver { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Location PickupLocation { get; set; } = null!;
    public Location DropoffLocation { get; set; } = null!;
    public Payment Payment { get; set; } = null!;        // 1-1
    public Review? Review { get; set; }                  // 0-1
    public FitToDriveCheck? FitToDriveCheck { get; set; } // 0-1 pre-ride sjekkliste
}



