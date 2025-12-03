using System.ComponentModel.DataAnnotations;

namespace Svipp.Api.DTOs;

/// <summary>
/// Request model for updating user profile
/// </summary>
public class UpdateUserRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters")]
    public string FullName { get; set; } = default!;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(320, ErrorMessage = "Email must not exceed 320 characters")]
    public string Email { get; set; } = default!;
    
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(32, MinimumLength = 8, ErrorMessage = "Phone number must be between 8 and 32 characters")]
    public string PhoneNumber { get; set; } = default!;
}

