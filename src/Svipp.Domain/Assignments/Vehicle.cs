using System.Collections.Generic;

namespace Svipp.Domain.Assignments;

public class Vehicle
{
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = null!;
    public string VehicleType { get; set; } = null!;
    public string Color { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}



