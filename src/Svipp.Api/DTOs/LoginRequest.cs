using System.ComponentModel.DataAnnotations;

namespace Svipp.Api.DTOs;

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "E-post er påkrevd")]
    [EmailAddress(ErrorMessage = "Ugyldig e-postformat")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Passord er påkrevd")]
    public string Password { get; set; } = default!;
}



