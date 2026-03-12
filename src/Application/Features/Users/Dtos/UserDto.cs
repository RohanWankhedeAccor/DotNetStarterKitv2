using Domain.Enums;

namespace Application.Features.Users.Dtos;

/// <summary>
/// DTO for returning user data. Returned by all user queries.
/// Sensitive fields (PasswordHash) are never included in this DTO.
/// </summary>
public class UserDto
{
    /// <summary>Gets or sets the unique user identifier.</summary>
    public required Guid Id { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    public required string Email { get; set; }

    /// <summary>Gets or sets the user's full name.</summary>
    public required string FullName { get; set; }

    /// <summary>Gets or sets the user's current status.</summary>
    public required UserStatus Status { get; set; }

    /// <summary>Gets or sets when the user was created (UTC).</summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets who created the user (user ID).</summary>
    public required string CreatedBy { get; set; }

    /// <summary>Gets or sets when the user was last modified (UTC).</summary>
    public required DateTimeOffset ModifiedAt { get; set; }

    /// <summary>Gets or sets who last modified the user (user ID).</summary>
    public required string ModifiedBy { get; set; }
}
