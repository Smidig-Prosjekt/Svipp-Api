using System.ComponentModel.DataAnnotations;

namespace Svipp.Api.DTOs;

/// <summary>
/// Request payload for checking if a folded scooter fits into a car.
/// </summary>
public class VehicleCapacityRequest
{
    /// <summary>
    /// Optional: Car make (brand), used for logging/analytics only.
    /// </summary>
    public string? Make { get; set; }

    /// <summary>
    /// Optional: Car model, used for logging/analytics only.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Optional: Model year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Internal length of the trunk in centimeters.
    /// </summary>
    [Required]
    [Range(10, 500, ErrorMessage = "Trunk length must be between 10 cm and 500 cm")]
    public decimal TrunkLengthCm { get; set; }

    /// <summary>
    /// Internal width of the trunk in centimeters.
    /// </summary>
    [Required]
    [Range(10, 300, ErrorMessage = "Trunk width must be between 10 cm and 300 cm")]
    public decimal TrunkWidthCm { get; set; }

    /// <summary>
    /// Internal height of the trunk in centimeters.
    /// </summary>
    [Required]
    [Range(10, 200, ErrorMessage = "Trunk height must be between 10 cm and 200 cm")]
    public decimal TrunkHeightCm { get; set; }
}

/// <summary>
/// Response for vehicle capacity check.
/// </summary>
public class VehicleCapacityResponse
{
    /// <summary>
    /// True if the folded scooter fits into the trunk with a safety margin.
    /// </summary>
    public bool Fits { get; set; }

    /// <summary>
    /// Human readable explanation of why it fits/does not fit.
    /// </summary>
    public string Reason { get; set; } = default!;

    /// <summary>
    /// Required volume in liters for the folded scooter (including safety margin).
    /// </summary>
    public decimal RequiredVolumeLiters { get; set; }

    /// <summary>
    /// Provided trunk volume in liters based on input.
    /// </summary>
    public decimal ProvidedVolumeLiters { get; set; }
}


