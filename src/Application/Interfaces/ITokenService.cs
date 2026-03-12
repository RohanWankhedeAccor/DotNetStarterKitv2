namespace Application.Interfaces;

/// <summary>
/// Service for generating and validating authentication tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier (as string)</param>
    /// <param name="email">The user's email address</param>
    /// <param name="fullName">The user's full name</param>
    /// <param name="roles">Collection of role names assigned to the user</param>
    /// <returns>A signed JWT token as a string</returns>
    string GenerateToken(string userId, string email, string fullName, IEnumerable<string> roles);

    /// <summary>
    /// Validates a JWT token and extracts claims if valid.
    /// </summary>
    /// <param name="token">The JWT token string to validate</param>
    /// <returns>The claims principal if valid; null if invalid or expired</returns>
    System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Token validity duration in minutes (e.g. 60).
    /// Handlers use this to populate the <c>ExpiresIn</c> field in login responses.
    /// </summary>
    int ExpirationMinutes { get; }
}
