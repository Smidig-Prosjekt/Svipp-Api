using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Svipp.Api.DTOs;
using Svipp.Api.Services;
using Svipp.Domain.Users;
using Svipp.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Svipp.Api.Controllers;

/// <summary>
/// User profile management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly SvippDbContext _context;
    private readonly ILogger<UsersController> _logger;
    private readonly PasswordHasher _passwordHasher;

    public UsersController(SvippDbContext context, ILogger<UsersController> logger, PasswordHasher passwordHasher)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>User profile data</returns>
    /// <response code="200">Returns the user profile</response>
    /// <response code="401">Unauthorized - Invalid or missing JWT token</response>
    /// <response code="404">User not found</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                _logger.LogWarning("Failed to extract user ID from token");
                return Unauthorized(new ErrorResponse
                {
                    Message = "Ugyldig autentiseringstoken",
                    Detail = "Kunne ikke identifisere bruker fra token",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found in database", userId);
                return NotFound(new ErrorResponse
                {
                    Message = "Bruker ikke funnet",
                    Detail = "Den autentiserte brukeren finnes ikke i systemet",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("User {UserId} retrieved profile successfully", userId);

            return Ok(new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Det oppstod en feil ved henting av brukerprofil",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    /// <param name="request">Updated user data</param>
    /// <returns>Updated user profile</returns>
    /// <response code="200">User profile updated successfully</response>
    /// <response code="400">Validation error in request data</response>
    /// <response code="401">Unauthorized - Invalid or missing JWT token</response>
    /// <response code="404">User not found</response>
    /// <response code="409">Email already in use by another user</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new ErrorResponse
                {
                    Message = "Validering feilet",
                    Detail = "Ett eller flere felt inneholder ugyldige data",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Errors = errors
                });
            }

            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                _logger.LogWarning("Failed to extract user ID from token when updating profile");
                return Unauthorized(new ErrorResponse
                {
                    Message = "Ugyldig autentiseringstoken",
                    Detail = "Kunne ikke identifisere bruker fra token",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            // Sanitize input
            var sanitized = SanitizeUpdateRequest(request);

            // Re-validate sanitized data to ensure sanitization didn't break validity
            var validationErrors = ValidateSanitizedRequest(sanitized);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("Sanitization invalidated input for user {UserId}", userId);
                return BadRequest(new ErrorResponse
                {
                    Message = "Validation failed",
                    Detail = "Input contains invalid characters that cannot be safely processed",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Errors = validationErrors
                });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when updating profile", userId);
                return NotFound(new ErrorResponse
                {
                    Message = "Bruker ikke funnet",
                    Detail = "Den autentiserte brukeren finnes ikke i systemet",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Check if email is already used by another user (case-insensitive comparison)
            var normalizedEmail = sanitized.Email.ToLowerInvariant();
            var emailOwner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.Id != userId);

            if (emailOwner != null)
            {
                _logger.LogWarning("Email conflict when updating profile for user {UserId}", userId);
                return Conflict(new ErrorResponse
                {
                    Message = "E-postadressen er allerede i bruk",
                    Detail = "Den oppgitte e-postadressen er allerede registrert av en annen bruker",
                    StatusCode = StatusCodes.Status409Conflict
                });
            }

            user.FirstName = sanitized.FirstName;
            user.LastName = sanitized.LastName;
            user.Email = sanitized.Email;
            user.PhoneNumber = sanitized.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated profile successfully", userId);

            return Ok(new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Kunne ikke oppdatere brukerprofil",
                Detail = "Det oppstod en databasefeil",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Kunne ikke oppdatere brukerprofil",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Change password for the current user
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Validation error or incorrect current password</response>
    /// <response code="401">Unauthorized - Invalid or missing JWT token</response>
    /// <response code="404">User not found</response>
    [HttpPut("me/password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new ErrorResponse
                {
                    Message = "Validering feilet",
                    Detail = "Ett eller flere felt inneholder ugyldige data",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Errors = errors
                });
            }

            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                _logger.LogWarning("Failed to extract user ID from token when changing password");
                return Unauthorized(new ErrorResponse
                {
                    Message = "Ugyldig autentiseringstoken",
                    Detail = "Kunne ikke identifisere bruker fra token",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when changing password", userId);
                return NotFound(new ErrorResponse
                {
                    Message = "Bruker ikke funnet",
                    Detail = "Den autentiserte brukeren finnes ikke i systemet",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Incorrect current password for user {UserId}", userId);
                return BadRequest(new ErrorResponse
                {
                    Message = "Feil passord",
                    Detail = "Det nåværende passordet er feil",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} changed password successfully", userId);

            return Ok(new
            {
                message = "Passord endret",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while changing password");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "En uventet feil oppstod",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }

    #region Helpers

    private Guid? GetUserIdFromToken()
    {
        var user = HttpContext.User;
        if (user?.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                      ?? user.FindFirst("sub")
                      ?? user.FindFirst("userId")
                      ?? user.FindFirst("id");

        if (idClaim == null)
        {
            return null;
        }

        return Guid.TryParse(idClaim.Value, out var userId) ? userId : null;
    }

    private static UpdateUserRequest SanitizeUpdateRequest(UpdateUserRequest request)
    {
        return new UpdateUserRequest
        {
            FirstName = SanitizeString(request.FirstName),
            LastName = SanitizeString(request.LastName),
            Email = SanitizeString(request.Email),
            PhoneNumber = SanitizeString(request.PhoneNumber)
        };
    }

    private static Dictionary<string, string[]> ValidateSanitizedRequest(UpdateUserRequest request)
    {
        var errors = new Dictionary<string, List<string>>();
        var validationContext = new ValidationContext(request, serviceProvider: null, items: null);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            foreach (var result in validationResults)
            {
                var memberNames = result.MemberNames.ToList();
                if (memberNames.Count == 0)
                {
                    memberNames.Add(""); // Add empty key for general errors
                }

                foreach (var memberName in memberNames)
                {
                    if (!errors.ContainsKey(memberName))
                    {
                        errors[memberName] = new List<string>();
                    }
                    errors[memberName].Add(result.ErrorMessage ?? "Validation failed");
                }
            }
        }

        return errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
    }

    private static string SanitizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim();

        // Remove dangerous characters
        var dangerousChars = new[] { '<', '>', '&', '"', '\'', '/', '\\' };
        foreach (var c in dangerousChars)
        {
            trimmed = trimmed.Replace(c.ToString(), string.Empty);
        }

        // Normalize whitespace
        while (trimmed.Contains("  "))
        {
            trimmed = trimmed.Replace("  ", " ");
        }

        return trimmed;
    }

    #endregion
}

