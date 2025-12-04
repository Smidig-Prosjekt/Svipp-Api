namespace Svipp.Api.DTOs;

public class CheckVehicleCapacityRequest
{
    /// <summary>
    /// Bilmerke, f.eks. \"Tesla\". Kalles \"brand\" (ikke \"make\") i API-et.
    /// </summary>
    public string Brand { get; set; } = null!;

    /// <summary>
    /// Bilmodell, f.eks. \"Model 3\".
    /// </summary>
    public string Model { get; set; } = null!;

    /// <summary>
    /// Årsmodell for bilen. Må være mellom 1970 og inneværende år.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Tilgjengelig bagasjeromsvolum i liter for bilen (heltall). Påkrevd.
    /// </summary>
    public int TrunkVolumeLiters { get; set; }
}
