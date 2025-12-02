using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Svipp.Api.DTOs;
using Svipp.Infrastructure;
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
            // Validate model state
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
                _logger.LogWarning("Failed to extract user ID from token during update");
                return Unauthorized(new ErrorResponse
                {
                    Message = "Invalid authentication token",
                    Detail = "Unable to identify user from provided token",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found during update", userId);
                return NotFound(new ErrorResponse
                {
                    Message = "User not found",
                    Detail = "The authenticated user does not exist in the system",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Sanitize and normalize input
            var sanitizedEmail = request.Email.Trim().ToLowerInvariant();
            var sanitizedFullName = SanitizeInput(request.FullName.Trim());
            var sanitizedPhoneNumber = SanitizeInput(request.PhoneNumber.Trim());

            // Validate sanitized input lengths (after sanitization to prevent bypass)
            var postSanitizationErrors = new Dictionary<string, string[]>();
            
            if (sanitizedFullName.Length < 2)
            {
                postSanitizationErrors.Add("FullName", new[] { "Full name must be at least 2 characters after sanitization" });
            }
            
            if (sanitizedPhoneNumber.Length < 8)
            {
                postSanitizationErrors.Add("PhoneNumber", new[] { "Phone number must be at least 8 characters after sanitization" });
            }

            if (postSanitizationErrors.Any())
            {
                _logger.LogWarning("User {UserId} provided data that became too short after sanitization", userId);
                return BadRequest(new ErrorResponse
                {
                    Message = "Validation failed",
                    Detail = "One or more fields are too short after removing invalid characters",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Errors = postSanitizationErrors
                });
            }

            // Check if email is already in use by another user
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLowerInvariant() == sanitizedEmail && u.Id != userId);
            
            if (emailExists)
            {
                _logger.LogWarning("User {UserId} attempted to use email already in use: {Email}", userId, sanitizedEmail);
                return Conflict(new ErrorResponse
                {
                    Message = "Email already in use",
                    Detail = "The provided email address is already registered to another user",
                    StatusCode = StatusCodes.Status409Conflict
                });
            }

            // Update user data
            user.FullName = sanitizedFullName;
            user.Email = sanitizedEmail;
            user.PhoneNumber = sanitizedPhoneNumber;
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
                Message = "An unexpected error occurred",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Extract user ID from JWT token claims
    /// </summary>
    /// <returns>User ID or null if not found</returns>
    private Guid? GetUserIdFromToken()
    {
        // Try multiple claim types for compatibility
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst("userId")?.Value
                       ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            _logger.LogWarning("No user ID claim found in token. Available claims: {Claims}", 
                string.Join(", ", User.Claims.Select(c => c.Type)));
            return null;
        }

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        _logger.LogWarning("Failed to parse user ID claim as GUID: {Claim}", userIdClaim);
        return null;
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Success message</returns>
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
            // Validate model state
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
                _logger.LogWarning("Failed to extract user ID from token during password change");
                return Unauthorized(new ErrorResponse
                {
                    Message = "Invalid authentication token",
                    Detail = "Unable to identify user from provided token",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found during password change", userId);
                return NotFound(new ErrorResponse
                {
                    Message = "User not found",
                    Detail = "The authenticated user does not exist in the system",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("User {UserId} provided incorrect current password", userId);
                return BadRequest(new ErrorResponse
                {
                    Message = "Incorrect password",
                    Detail = "The current password provided is incorrect",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Hash and update new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
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

    /// <summary>
    /// Sanitize user input to prevent XSS and injection attacks
    /// </summary>
    /// <param name="input">Raw input string</param>
    /// <returns>Sanitized string</returns>
    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Remove potentially dangerous characters
        var sanitized = input
            .Replace("<", "")
            .Replace(">", "")
            .Replace("&", "")
            .Replace("\"", "")
            .Replace("'", "")
            .Replace("/", "")
            .Replace("\\", "");

        // Normalize whitespace
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", " ");

        return sanitized.Trim();
    }
}
