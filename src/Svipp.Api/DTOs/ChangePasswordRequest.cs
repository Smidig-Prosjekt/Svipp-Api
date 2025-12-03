using System.ComponentModel.DataAnnotations;

namespace Svipp.Api.DTOs;

/// <summary>
/// Request model for changing user password
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Nåværende passord er påkrevd")]
    public string CurrentPassword { get; set; } = default!;
    
    [Required(ErrorMessage = "Nytt passord er påkrevd")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Passord må være minst 8 tegn")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Passordet må ha minst én stor bokstav, én liten bokstav, ett tall og ett spesialtegn")]
    public string NewPassword { get; set; } = default!;
    
    [Required(ErrorMessage = "Bekreftelse av nytt passord er påkrevd")]
    [Compare("NewPassword", ErrorMessage = "Nytt passord og bekreftelse stemmer ikke overens")]
    public string ConfirmNewPassword { get; set; } = default!;
}

