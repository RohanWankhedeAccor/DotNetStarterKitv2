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
    /// <param name="firstName">The user's first name</param>
    /// <param name="lastName">The user's last name</param>
    /// <param name="roles">Collection of role names assigned to the user</param>
    /// <param name="permissions">Collection of permission keys derived from the user's roles (e.g. "users.create")</param>
    /// <returns>A signed JWT token as a string</returns>
    string GenerateToken(string userId, string email, string firstName, string lastName, IEnumerable<string> roles, IEnumerable<string> permissions);

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
