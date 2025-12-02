using System.Collections.Generic;

namespace Svipp.Domain.Assignments;

public class Driver
{
    public int DriverId { get; set; }
    public string Name { get; set; } = null!;
    public string AvailabilityStatus { get; set; } = null!; // possibly an enum later

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}



