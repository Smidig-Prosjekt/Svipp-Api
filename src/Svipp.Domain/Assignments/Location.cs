using System.Collections.Generic;

namespace Svipp.Domain.Assignments;

public class Location
{
    public int LocationId { get; set; }
    public string Address { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Navigation
    public ICollection<Booking> PickupBookings { get; set; } = new List<Booking>();
    public ICollection<Booking> DropoffBookings { get; set; } = new List<Booking>();
}



