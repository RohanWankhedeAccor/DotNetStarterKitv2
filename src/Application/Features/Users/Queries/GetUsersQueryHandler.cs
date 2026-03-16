using Application.Common.Models;
using Application.Features.Users.Dtos;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Features.Users.Queries;

/// <summary>
/// Handler for <see cref="GetUsersQuery"/>. Retrieves a filtered, sorted, and paginated list
/// of users including their assigned roles.
///
/// <para>
/// Filtering is applied before counting so <c>TotalCount</c> reflects the filtered set,
/// not the total number of users in the database.
/// </para>
/// </summary>
public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResponse<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsersQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The Unit of Work providing repository access.</param>
    public GetUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<PagedResponse<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Users.AsQueryable().AsNoTracking();

        // ── Filtering ────────────────────────────────────────────────────────────

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        if (request.Status.HasValue)
            query = query.Where(u => u.Status == request.Status.Value);

        // ── Sorting ──────────────────────────────────────────────────────────────

        // Map the caller-supplied column name to a sort key expression.
        // Unknown column names fall back to Email (stable, unique).
        Expression<Func<User, object>> sortKey = request.SortBy?.ToLower() switch
        {
            "firstname"  => u => u.FirstName,
            "lastname"   => u => u.LastName,
            "status"     => u => u.Status,
            "createdat"  => u => u.CreatedAt,
            _            => u => u.Email,
        };

        query = request.SortDescending
            ? query.OrderByDescending(sortKey)
            : query.OrderBy(sortKey);

        // ── Pagination ───────────────────────────────────────────────────────────

        // Count after filtering so TotalCount reflects the filtered result set.
        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (request.PageNumber - 1) * request.PageSize;
        var users = await query
            .Skip(offset)
            .Take(request.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                CreatedBy = u.CreatedBy,
                ModifiedAt = u.ModifiedAt,
                ModifiedBy = u.ModifiedBy,
                Roles = u.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserDto>
        {
            Items = users,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
