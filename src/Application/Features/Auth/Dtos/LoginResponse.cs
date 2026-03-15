namespace Application.Features.Auth.Dtos;

/// <summary>
/// Response DTO for successful user login.
/// Contains the JWT bearer token and basic user information.
/// </summary>
public class LoginResponse
{
    /// <summary>Gets or sets the JWT bearer token for subsequent API requests.</summary>
    public required string Token { get; set; }

    /// <summary>Gets or sets the user's unique identifier.</summary>
    public required Guid UserId { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    public required string Email { get; set; }

    /// <summary>Gets or sets the user's first name.</summary>
    public required string FirstName { get; set; }

    /// <summary>Gets or sets the user's last name.</summary>
    public required string LastName { get; set; }

    /// <summary>Gets or sets the roles assigned to the user.</summary>
    public required IEnumerable<string> Roles { get; set; }

    /// <summary>Gets or sets the token expiration time in seconds (from issue time).</summary>
    public int ExpiresIn { get; set; } = 3600; // 60 minutes
}
