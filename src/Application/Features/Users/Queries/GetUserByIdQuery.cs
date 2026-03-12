using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Queries;

/// <summary>
/// Query to retrieve a single user by ID. Returns <see cref="UserDto"/>.
/// Throws <see cref="Domain.Exceptions.NotFoundException"/> if user does not exist.
/// </summary>
public class GetUserByIdQuery : IRequest<UserDto>
{
    /// <summary>Gets or sets the user ID to retrieve.</summary>
    public required Guid Id { get; set; }
}
