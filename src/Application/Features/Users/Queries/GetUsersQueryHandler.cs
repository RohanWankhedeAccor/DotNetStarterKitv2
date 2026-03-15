using Application.Common.Models;
using Application.Features.Users.Dtos;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Queries;

/// <summary>
/// Handler for <see cref="GetUsersQuery"/>. Retrieves a paginated list of users including
/// their assigned roles. Uses direct <c>Select()</c> projection for efficiency — no full
/// entity materialisation needed. Uses <c>AsNoTracking()</c> for read-only performance.
/// </summary>
public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResponse<UserDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsersQueryHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PagedResponse<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var offset = (request.PageNumber - 1) * request.PageSize;

        var totalCount = await _context.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
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
