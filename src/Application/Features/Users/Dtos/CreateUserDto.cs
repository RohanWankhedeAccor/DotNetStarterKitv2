namespace Application.Features.Users.Dtos;

/// <summary>
/// DTO for creating a new user. Used as input to <see cref="Commands.CreateUserCommand"/>.
/// </summary>
public class CreateUserDto
{
    /// <summary>Gets or sets the user's email address (unique).</summary>
    public required string Email { get; set; }

    /// <summary>Gets or sets the user's full name.</summary>
    public required string FullName { get; set; }

    /// <summary>Gets or sets the user's password (plain text). Will be hashed before storage.</summary>
    public required string Password { get; set; }
}
