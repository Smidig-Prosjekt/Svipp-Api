namespace Svipp.Api.DTOs;

public class CheckVehicleCapacityResponse
{
    /// <summary>
    /// Om sparkesykkelen får plass i bilen.
    /// </summary>
    public bool Fits { get; set; }

    /// <summary>
    /// Kort, lesbar begrunnelse for hvorfor / hvorfor ikke.
    /// </summary>
    public string Reason { get; set; } = null!;

    /// <summary>
    /// Brukt volum (liter) for selve sparkesykkelen i vurderingen.
    /// </summary>
    public int ScooterVolumeLiters { get; set; }

    /// <summary>
    /// Hvilket volum vi tok utgangspunkt i for bilen (fra modelloppslag eller direkte input).
    /// </summary>
    public int? VehicleTrunkVolumeLiters { get; set; }
}
