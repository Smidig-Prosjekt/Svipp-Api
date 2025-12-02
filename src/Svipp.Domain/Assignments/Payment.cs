using System;

namespace Svipp.Domain.Assignments;

public class Payment
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }

    public decimal AmountPaid { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = null!;

    public Booking Booking { get; set; } = null!;
}



