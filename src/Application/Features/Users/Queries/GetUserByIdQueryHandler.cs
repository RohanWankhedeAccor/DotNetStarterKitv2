using Application.Common.Results;
using Application.Features.Users.Dtos;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Queries;

/// <summary>
/// Handler for <see cref="GetUserByIdQuery"/>. Retrieves a single user by ID including
/// their assigned roles. Uses direct <c>Select()</c> projection for efficiency.
/// Returns <see cref="Error.NotFound"/> if the user does not exist.
/// </summary>
public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="GetUserByIdQueryHandler"/> class.</summary>
    public GetUserByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.AsQueryable()
            .AsNoTracking()
            .Where(u => u.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Error.NotFound(nameof(User), request.Id);

        return user;
    }
}
