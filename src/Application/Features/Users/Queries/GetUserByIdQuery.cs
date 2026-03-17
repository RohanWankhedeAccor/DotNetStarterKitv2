using Application.Common.Results;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Queries;

/// <summary>
/// Query to retrieve a single user by ID.
/// Returns <see cref="Result{T}"/> — callers must inspect <c>IsSuccess</c> rather than catching exceptions.
/// </summary>
public class GetUserByIdQuery : IRequest<Result<UserDto>>
{
    /// <summary>Gets or sets the user ID to retrieve.</summary>
    public required Guid Id { get; set; }
}
