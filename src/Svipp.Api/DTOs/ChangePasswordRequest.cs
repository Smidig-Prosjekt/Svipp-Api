using System.ComponentModel.DataAnnotations;

namespace Svipp.Api.DTOs;

/// <summary>
/// Request model for changing user password
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = default!;
    
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character, and only contain allowed characters")]
    public string NewPassword { get; set; } = default!;
    
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
    public string ConfirmNewPassword { get; set; } = default!;
}

