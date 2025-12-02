using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Svipp.Domain.Users;
using Svipp.Infrastructure;

namespace Svipp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly SvippDbContext _dbContext;

    public UsersController(SvippDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResponse>> Register([FromBody] RegisterUserRequest request)
    {
        // Server-side validation of model
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kvp => kvp.Value is not null && kvp.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(new ValidationProblemDetails(errors));
        }

        // Check if email is already in use (unique constraint), case-insensitive, uten å endre innsendt verdi
        var emailLookup = request.Email.ToLowerInvariant();
        var existingUserByEmail = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLookup);

        if (existingUserByEmail is not null)
        {
            ModelState.AddModelError(nameof(request.Email), "E-postadressen er allerede registrert.");
            return ValidationProblem(ModelState);
        }

        // Check if phone number is already in use (unique constraint)
        var existingUserByPhone = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (existingUserByPhone is not null)
        {
            ModelState.AddModelError(nameof(request.PhoneNumber), "Telefonnummeret er allerede registrert på en annen bruker.");
            return ValidationProblem(ModelState);
        }

        // Create user entity – lagrer verdiene slik de kom inn i requesten
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var response = new RegisterUserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.CreatedAt
        );

        return CreatedAtAction(nameof(Register), new { id = user.Id }, response);
    }

    private static string HashPassword(string password)
    {
        // Simple SHA256 hash for now – *not* production-grade, but OK for learning purposes.
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = sha.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
}

public class RegisterUserRequest
{
    // Fullt navn: minst to ord, kun bokstaver (inkl. norske) og mellomrom, ingen trimming
    [Required(ErrorMessage = "Fullt navn er påkrevd.")]
    [MaxLength(200, ErrorMessage = "Fullt navn kan ikke være lengre enn 200 tegn.")]
    [RegularExpression(
        @"^(?=.{1,200}$)(?=.*\s)[A-Za-zÆØÅæøå ]+$",
        ErrorMessage = "Fullt navn må bestå av minst to ord og bare inneholde bokstaver og mellomrom."
    )]
    public string FullName { get; set; } = default!;

    [Required(ErrorMessage = "E-post er påkrevd.")]
    [MaxLength(320, ErrorMessage = "E-post kan ikke være lengre enn 320 tegn.")]
    [RegularExpression(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "E-postadressen må ha et gyldig toppdomene (f.eks. .no eller .com)."
    )]
    public string Email { get; set; } = default!;

    // Norsk telefonnummer: nøyaktig 8 sifre, ingen trimming
    [Required(ErrorMessage = "Telefonnummer er påkrevd.")]
    [RegularExpression(
        @"^\d{8}$",
        ErrorMessage = "Telefonnummer må være et norsk nummer med nøyaktig 8 sifre."
    )]
    public string PhoneNumber { get; set; } = default!;

    // Passord: 8–64 tegn, minst én liten, én stor, ett spesialtegn, ingen trimming
    [Required(ErrorMessage = "Passord er påkrevd.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9]).{8,64}$",
        ErrorMessage = "Passord må være 8–64 tegn og inneholde minst én liten bokstav, én stor bokstav og ett spesialtegn."
    )]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;
}

public record RegisterUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    DateTime CreatedAt
);


