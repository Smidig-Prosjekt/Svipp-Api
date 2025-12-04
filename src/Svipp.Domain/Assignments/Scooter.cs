using System;
using System.Collections.Generic;

namespace Svipp.Domain.Assignments;

public class Scooter
{
    public int ScooterId { get; set; }

    public string Brand { get; set; } = null!;          // Merke, f.eks. "Xiaomi"
    public string Model { get; set; } = null!;          // Modellnavn

    public int MaxRangeKm { get; set; }                 // Oppgitt rekkevidde på full batteri
    public int BatteryLevelPercent { get; set; }        // 0-100 %

    public int? CurrentLocationId { get; set; }         // Hvor sparkesykkelen står nå (valgfritt)

    // Relasjoner
    public int DriverId { get; set; }                   // Hvilken sjåfør som disponerer sparkesykkelen
    public Driver Driver { get; set; } = null!;

    public Location? CurrentLocation { get; set; }
}


