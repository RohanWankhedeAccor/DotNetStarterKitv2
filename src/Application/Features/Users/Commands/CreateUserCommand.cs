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

    /// <summary>Gets or sets the full name for the new user.</summary>
    public required string FullName { get; set; }

    /// <summary>Gets or sets the plain-text password (will be hashed).</summary>
    public required string Password { get; set; }
}
