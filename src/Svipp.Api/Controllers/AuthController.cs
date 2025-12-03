using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Svipp.Api.DTOs;
using Svipp.Api.Services;
using Svipp.Domain.Users;
using Svipp.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Svipp.Api.Controllers;

/// <summary>
/// Authentication endpoints (login and registration)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly SvippDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly PasswordHasher _passwordHasher;

    public AuthController(
        SvippDbContext context,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        PasswordHasher passwordHasher)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration data</param>
    /// <returns>Authentication token and user data</returns>
    /// <response code="201">User registered successfully</response>
    /// <response code="400">Validation error or email/phone already in use</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
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

            // Check if email is already in use (case-insensitive)
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUserByEmail = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingUserByEmail != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", normalizedEmail);
                return BadRequest(new ErrorResponse
                {
                    Message = "Email already in use",
                    Detail = "The provided email address is already registered",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Check if phone number is already in use
            var existingUserByPhone = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber.Trim());

            if (existingUserByPhone != null)
            {
                _logger.LogWarning("Registration attempt with existing phone number");
                return BadRequest(new ErrorResponse
                {
                    Message = "Phone number already in use",
                    Detail = "The provided phone number is already registered",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(), // Preserve original casing
                PhoneNumber = request.PhoneNumber.Trim(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} registered successfully", user.Id);

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return CreatedAtAction(nameof(Register), new { id = user.Id }, new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // Token expires in 24 hours
                User = new UserResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                }
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during user registration");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Failed to register user",
                Detail = "A database error occurred",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "An unexpected error occurred",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication token and user data</returns>
    /// <response code="200">Login successful</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
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

            // Find user by email (case-insensitive)
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", normalizedEmail);
                return Unauthorized(new ErrorResponse
                {
                    Message = "Invalid credentials",
                    Detail = "Email or password is incorrect",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // Token expires in 24 hours
                User = new UserResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "An unexpected error occurred",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }

    private string GenerateJwtToken(User user)
    {
        // Priority: Environment variables > appsettings.json > default fallback
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
                       ?? _configuration["JWT_SECRET"] 
                       ?? throw new InvalidOperationException("JWT_SECRET must be configured. Set it in appsettings.json or as environment variable.");
        
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                       ?? _configuration["JWT_ISSUER"] 
                       ?? "Svipp.Api";
        
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                         ?? _configuration["JWT_AUDIENCE"] 
                         ?? "Svipp.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim("sub", user.Id.ToString()), // Standard JWT claim
            new Claim("userId", user.Id.ToString()) // Additional claim for compatibility
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}

