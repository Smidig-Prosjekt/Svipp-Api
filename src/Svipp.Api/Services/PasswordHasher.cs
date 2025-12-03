using BCryptNet = BCrypt.Net.BCrypt;

namespace Svipp.Api.Services;

/// <summary>
/// Service for secure password hashing with BCrypt and pepper
/// </summary>
public class PasswordHasher
{
    private readonly string _pepper;

    public PasswordHasher(IConfiguration configuration)
    {
        // Priority: Environment variable > appsettings.json
        _pepper = Environment.GetEnvironmentVariable("PASSWORD_PEPPER")
                 ?? configuration["PASSWORD_PEPPER"]
                 ?? throw new InvalidOperationException(
                     "PASSWORD_PEPPER must be configured. " +
                     "Set it in appsettings.json, appsettings.Development.json, or as PASSWORD_PEPPER environment variable.");
    }

    /// <summary>
    /// Hash a password with BCrypt and pepper
    /// </summary>
    public string HashPassword(string password)
    {
        // Combine password with pepper before hashing
        var pepperedPassword = password + _pepper;
        return BCryptNet.HashPassword(pepperedPassword);
    }

    /// <summary>
    /// Verify a password against a hash using BCrypt and pepper
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        // Combine password with pepper before verification
        var pepperedPassword = password + _pepper;
        return BCryptNet.Verify(pepperedPassword, hash);
    }
}



