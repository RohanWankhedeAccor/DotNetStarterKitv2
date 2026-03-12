using Application.Common.Models;
using Application.Features.Users.Dtos;
using Application.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Queries;

/// <summary>
/// Handler for <see cref="GetUsersQuery"/>. Retrieves a paginated list of users,
/// applying offset/limit based on <see cref="PagedRequest.PageNumber"/> and
/// <see cref="PagedRequest.PageSize"/>. Uses <c>AsNoTracking()</c> for performance.
/// </summary>
public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResponse<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsersQueryHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public GetUsersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<PagedResponse<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // Calculate offset from page number and page size.
        var offset = (request.PageNumber - 1) * request.PageSize;

        // Fetch total count for pagination metadata.
        var totalCount = await _context.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // Fetch the requested page of users.
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Skip(offset)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userDtos = _mapper.Map<List<UserDto>>(users);

        return new PagedResponse<UserDto>
        {
            Items = userDtos,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
