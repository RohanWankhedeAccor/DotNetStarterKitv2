using Application.Common.Models;
using Application.Features.Users.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Queries;

/// <summary>
/// Query to retrieve a paginated, filtered, and sorted list of users.
/// Inherits pagination and sorting parameters from <see cref="PagedRequest"/>.
///
/// <para><b>Filtering</b></para>
/// <list type="bullet">
///   <item><see cref="Search"/> — case-insensitive substring match on email, first name, or last name.</item>
///   <item><see cref="Status"/> — exact match on <see cref="UserStatus"/>; <c>null</c> returns all statuses.</item>
/// </list>
///
/// <para><b>Sorting</b> (via <see cref="PagedRequest.SortBy"/>)</para>
/// Valid column names: <c>email</c>, <c>firstName</c>, <c>lastName</c>, <c>status</c>, <c>createdAt</c>.
/// Unknown values fall back to <c>email</c> ascending.
/// </summary>
public class GetUsersQuery : PagedRequest, IRequest<PagedResponse<UserDto>>
{
    /// <summary>
    /// Gets or sets an optional free-text search term.
    /// Matched case-insensitively against email, first name, and last name.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Gets or sets an optional status filter. When <c>null</c>, all statuses are returned.
    /// </summary>
    public UserStatus? Status { get; set; }
}
