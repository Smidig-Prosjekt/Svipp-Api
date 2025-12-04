namespace Svipp.Api.Services;

/// <summary>
/// Result of a capacity check.
/// </summary>
public record VehicleCapacityResult(
    bool Fits,
    string Reason,
    decimal RequiredVolumeLiters,
    decimal ProvidedVolumeLiters);

/// <summary>
/// Configuration for the folded scooter dimensions.
/// This intentionally lives in the API layer for now and does not touch the domain model.
/// </summary>
public static class ScooterCapacityConfig
{
    // Folded scooter dimensions in centimeters (example values).
    public const decimal FoldedLengthCm = 120m;
    public const decimal FoldedWidthCm = 20m;
    public const decimal FoldedHeightCm = 50m;

    // Safety factor to avoid edge cases where it technically fits but is very tight.
    public const decimal SafetyMarginFactor = 1.1m;

    public static decimal RequiredVolumeLiters =>
        System.Math.Round(FoldedLengthCm * FoldedWidthCm * FoldedHeightCm / 1000m * SafetyMarginFactor, 1);
}

public interface IVehicleCapacityService
{
    VehicleCapacityResult CheckCapacity(
        decimal trunkLengthCm,
        decimal trunkWidthCm,
        decimal trunkHeightCm);
}

/// <summary>
/// Simple capacity check implementation that compares trunk dimensions
/// against a folded scooter with a safety margin.
/// </summary>
public class VehicleCapacityService : IVehicleCapacityService
{
    public VehicleCapacityResult CheckCapacity(
        decimal trunkLengthCm,
        decimal trunkWidthCm,
        decimal trunkHeightCm)
    {
        var requiredLength = ScooterCapacityConfig.FoldedLengthCm * ScooterCapacityConfig.SafetyMarginFactor;
        var requiredWidth = ScooterCapacityConfig.FoldedWidthCm * ScooterCapacityConfig.SafetyMarginFactor;
        var requiredHeight = ScooterCapacityConfig.FoldedHeightCm * ScooterCapacityConfig.SafetyMarginFactor;

        var trunkVolumeLiters = System.Math.Round(trunkLengthCm * trunkWidthCm * trunkHeightCm / 1000m, 1);
        var requiredVolumeLiters = ScooterCapacityConfig.RequiredVolumeLiters;

        // Check each dimension individually so we can give a good explanation.
        if (trunkLengthCm < requiredLength)
        {
            return new VehicleCapacityResult(
                Fits: false,
                Reason: $"Bagasjerommet er for kort. Minst {requiredLength:F0} cm lengde anbefales.",
                RequiredVolumeLiters: requiredVolumeLiters,
                ProvidedVolumeLiters: trunkVolumeLiters);
        }

        if (trunkWidthCm < requiredWidth)
        {
            return new VehicleCapacityResult(
                Fits: false,
                Reason: $"Bagasjerommet er for smalt. Minst {requiredWidth:F0} cm bredde anbefales.",
                RequiredVolumeLiters: requiredVolumeLiters,
                ProvidedVolumeLiters: trunkVolumeLiters);
        }

        if (trunkHeightCm < requiredHeight)
        {
            return new VehicleCapacityResult(
                Fits: false,
                Reason: $"Bagasjerommet er for lavt. Minst {requiredHeight:F0} cm høyde anbefales.",
                RequiredVolumeLiters: requiredVolumeLiters,
                ProvidedVolumeLiters: trunkVolumeLiters);
        }

        // If all dimensions pass, we say it fits.
        return new VehicleCapacityResult(
            Fits: true,
            Reason: "Sparkesykkelen bør få plass i bagasjerommet med litt sikkerhetsmargin.",
            RequiredVolumeLiters: requiredVolumeLiters,
            ProvidedVolumeLiters: trunkVolumeLiters);
    }
}


