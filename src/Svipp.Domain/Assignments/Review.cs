using System;

namespace Svipp.Domain.Assignments;

public class Review
{
    public int ReviewId { get; set; }
    public int BookingId { get; set; }
    public int CustomerId { get; set; }

    public int Rating { get; set; }     // 1–5 etc.
    public DateTime Date { get; set; }
    public string? Comment { get; set; }

    public Booking Booking { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}



