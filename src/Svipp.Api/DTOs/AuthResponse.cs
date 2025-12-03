namespace Svipp.Api.DTOs;

/// <summary>
/// Response model for authentication (login/register)
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = default!;
}



