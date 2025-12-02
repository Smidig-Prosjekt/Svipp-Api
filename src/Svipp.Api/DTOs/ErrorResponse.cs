namespace Svipp.Api.DTOs;

/// <summary>
/// Standardized error response model
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = default!;
    public string? Detail { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string[]>? Errors { get; set; }
}

