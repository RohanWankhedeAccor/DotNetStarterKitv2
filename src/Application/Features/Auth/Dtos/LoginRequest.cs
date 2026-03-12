namespace Application.Features.Auth.Dtos;

/// <summary>
/// Request DTO for user login.
/// Contains credentials (email and password) required for authentication.
/// </summary>
public class LoginRequest
{
    /// <summary>Gets or sets the user's email address.</summary>
    public required string Email { get; set; }

    /// <summary>Gets or sets the user's plaintext password.</summary>
    public required string Password { get; set; }
}
