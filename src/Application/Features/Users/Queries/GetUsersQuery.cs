using Application.Common.Models;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Queries;

/// <summary>
/// Query to retrieve a paginated list of users. Inherits pagination parameters
/// from <see cref="PagedRequest"/> (<see cref="PagedRequest.PageNumber"/>, <see cref="PagedRequest.PageSize"/>).
/// Returns <see cref="PagedResponse{T}"/> containing a page of <see cref="UserDto"/>.
/// </summary>
public class GetUsersQuery : PagedRequest, IRequest<PagedResponse<UserDto>>
{
}
