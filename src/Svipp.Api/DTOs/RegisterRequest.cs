using System.ComponentModel.DataAnnotations;

namespace Svipp.Api.DTOs;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Fornavn er påkrevd")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Fornavn må være mellom 1 og 100 tegn")]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessage = "Etternavn er påkrevd")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Etternavn må være mellom 1 og 100 tegn")]
    public string LastName { get; set; } = default!;

    [Required(ErrorMessage = "E-post er påkrevd")]
    [EmailAddress(ErrorMessage = "Ugyldig e-postformat")]
    [StringLength(320, ErrorMessage = "E-post kan ikke være lengre enn 320 tegn")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Telefonnummer er påkrevd")]
    [Phone(ErrorMessage = "Telefonnummer må være på rett format")]
    [StringLength(32, MinimumLength = 8, ErrorMessage = "Telefonnummer må være mellom 8 og 32 tegn")]
    public string PhoneNumber { get; set; } = default!;

    [Required(ErrorMessage = "Passord er påkrevd")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Passord må være minst 8 tegn")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Passordet må ha minst én stor bokstav, én liten bokstav, ett tall og ett spesialtegn")]
    public string Password { get; set; } = default!;
}



