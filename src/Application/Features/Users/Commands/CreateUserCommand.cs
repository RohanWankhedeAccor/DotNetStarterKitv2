using Application.Common;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to create a new user. Implements <see cref="IRequest{TResponse}"/>
/// with response type <see cref="UserDto"/>.
/// </summary>
public class CreateUserCommand : IRequest<UserDto>
{
    /// <summary>Gets or sets the email address for the new user.</summary>
    public required string Email { get; set; }

    /// <summary>Gets or sets the first name for the new user.</summary>
    public required string FirstName { get; set; }

    /// <summary>Gets or sets the last name for the new user.</summary>
    public required string LastName { get; set; }

    /// <summary>Gets or sets the plain-text password (will be hashed). Masked in logs.</summary>
    [Sensitive]
    public required string Password { get; set; }
}
