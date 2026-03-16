using Application.Common.Results;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Commands;

/// <summary>
/// Handles <see cref="AssignRoleCommand"/> by linking an existing user to an existing role
/// via the <see cref="UserRole"/> junction table. The operation is idempotent — if the
/// role is already assigned it returns successfully without creating a duplicate.
/// </summary>
public sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of <see cref="AssignRoleCommandHandler"/>.</summary>
    public AssignRoleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // Verify the target user exists.
        var userExists = await _unitOfWork.Users.AsQueryable()
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
            return Error.NotFound(nameof(User), request.UserId);

        // Verify the requested role exists.
        var role = await _unitOfWork.Roles.AsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == request.RoleName, cancellationToken);

        if (role is null)
            return Error.NotFound(nameof(Role), request.RoleName);

        // Idempotency guard — do nothing if the assignment already exists.
        var alreadyAssigned = await _unitOfWork.UserRoles.AsQueryable()
            .AnyAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.Id, cancellationToken);

        if (alreadyAssigned)
            return Result.Success();

        _unitOfWork.UserRoles.Add(new UserRole(request.UserId, role.Id));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
