using System.ComponentModel.DataAnnotations;

namespace Svipp.Api.DTOs;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    public string LastName { get; set; } = default!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(320, ErrorMessage = "Email must not exceed 320 characters")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(32, MinimumLength = 8, ErrorMessage = "Phone number must be between 8 and 32 characters")]
    public string PhoneNumber { get; set; } = default!;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character, and only contain allowed characters")]
    public string Password { get; set; } = default!;
}



