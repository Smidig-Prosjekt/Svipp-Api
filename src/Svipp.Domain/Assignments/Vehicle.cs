using System.Collections.Generic;

namespace Svipp.Domain.Assignments;

public class Vehicle
{
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = null!;
    public string VehicleType { get; set; } = null!;
    public string Color { get; set; } = null!;

    // Nytt: detaljer om bilen
    public string Make { get; set; } = null!;           // Merke, f.eks. "Tesla"
    public string Model { get; set; } = null!;          // Modell, f.eks. "Model 3"
    public int? Year { get; set; }                      // Årsmodell (valgfri)
    public int? TrunkVolumeLiters { get; set; }         // Bagasjeromsvolum i liter (valgfri)
    public string? TrunkDimensions { get; set; }        // F.eks. "100 x 80 x 50 cm" (valgfri)

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
