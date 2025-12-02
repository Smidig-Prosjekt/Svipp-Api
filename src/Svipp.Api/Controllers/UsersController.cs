using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Svipp.Api.DTOs;
using Svipp.Domain.Users;
using Svipp.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BCryptNet = BCrypt.Net.BCrypt;

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

    public UsersController(SvippDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
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
                    Message = "Invalid authentication token",
                    Detail = "Unable to identify user from provided token",
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
                    Message = "User not found",
                    Detail = "The authenticated user does not exist in the system",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("User {UserId} retrieved profile successfully", userId);

            return Ok(new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
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
                Message = "An error occurred while retrieving user profile",
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
                    Message = "Validation failed",
                    Detail = "One or more fields contain invalid data",
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
                    Message = "Invalid authentication token",
                    Detail = "Unable to identify user from provided token",
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
                    Message = "User not found",
                    Detail = "The authenticated user does not exist in the system",
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
                    Message = "Email already in use",
                    Detail = "The provided email address is already registered to another user",
                    StatusCode = StatusCodes.Status409Conflict
                });
            }

            user.FullName = sanitized.FullName;
            user.Email = sanitized.Email;
            user.PhoneNumber = sanitized.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated profile successfully", userId);

            return Ok(new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
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
                Message = "Failed to update user profile",
                Detail = "A database error occurred",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Failed to update user profile",
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
                    Message = "Validation failed",
                    Detail = "One or more fields contain invalid data",
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
                    Message = "Invalid authentication token",
                    Detail = "Unable to identify user from provided token",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when changing password", userId);
                return NotFound(new ErrorResponse
                {
                    Message = "User not found",
                    Detail = "The authenticated user does not exist in the system",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Incorrect current password for user {UserId}", userId);
                return BadRequest(new ErrorResponse
                {
                    Message = "Incorrect password",
                    Detail = "The current password provided is incorrect",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} changed password successfully", userId);

            return Ok(new
            {
                message = "Password changed successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while changing password");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "An unexpected error occurred",
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
            FullName = SanitizeString(request.FullName),
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

    private static string HashPassword(string password)
    {
        // Use BCrypt for secure password hashing
        return BCryptNet.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        // Verify password using BCrypt
        return BCryptNet.Verify(password, hash);
    }

    #endregion
}
