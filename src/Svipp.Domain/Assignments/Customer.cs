using System;
using System.Collections.Generic;

namespace Svipp.Domain.Assignments;

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PaymentInfo { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Foreign key to the User who owns this customer profile.
    /// Nullable to support legacy customers without user accounts.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Last known position for customer (e.g., when booking is created)
    /// </summary>
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public DateTime? LastLocationUpdatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}



