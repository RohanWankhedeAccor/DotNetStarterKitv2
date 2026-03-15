using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Commands;

/// <summary>
/// Handles <see cref="AssignRoleCommand"/> by linking an existing user to an existing role
/// via the <see cref="UserRole"/> junction table. The operation is idempotent — if the
/// role is already assigned it returns successfully without creating a duplicate.
/// </summary>
public sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="AssignRoleCommandHandler"/>.
    /// </summary>
    public AssignRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // Verify the target user exists.
        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
            throw new NotFoundException(nameof(User), request.UserId);

        // Verify the requested role exists.
        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == request.RoleName, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleName);

        // Idempotency guard — do nothing if the assignment already exists.
        var alreadyAssigned = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.Id, cancellationToken);

        if (alreadyAssigned)
            return Unit.Value;

        _context.UserRoles.Add(new UserRole(request.UserId, role.Id));
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
