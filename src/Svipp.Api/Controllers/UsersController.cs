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

        // Check if email is already in use (unique constraint)
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(request.Email), "E-postadressen er allerede registrert.");
            return ValidationProblem(ModelState);
        }

        // Create user entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber.Trim(),
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var response = new RegisterUserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
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
    [Required(ErrorMessage = "Fornavn er påkrevd.")]
    [MaxLength(100, ErrorMessage = "Fornavn kan ikke være lengre enn 100 tegn.")]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessage = "Etternavn er påkrevd.")]
    [MaxLength(100, ErrorMessage = "Etternavn kan ikke være lengre enn 100 tegn.")]
    public string LastName { get; set; } = default!;

    [Required(ErrorMessage = "E-post er påkrevd.")]
    [EmailAddress(ErrorMessage = "E-postadressen er ikke gyldig.")]
    [MaxLength(320, ErrorMessage = "E-post kan ikke være lengre enn 320 tegn.")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Telefonnummer er påkrevd.")]
    [MaxLength(32, ErrorMessage = "Telefonnummer kan ikke være lengre enn 32 tegn.")]
    public string PhoneNumber { get; set; } = default!;

    [Required(ErrorMessage = "Passord er påkrevd.")]
    [MinLength(8, ErrorMessage = "Passord må være minst 8 tegn.")]
    public string Password { get; set; } = default!;
}

public record RegisterUserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateTime CreatedAt
);


