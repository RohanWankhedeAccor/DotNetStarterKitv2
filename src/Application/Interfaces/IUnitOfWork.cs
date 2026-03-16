using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Unit of Work abstraction that coordinates multiple repository operations in a single
/// database transaction.
///
/// Application layer handlers depend on this interface — never on EF Core or the
/// concrete <c>ApplicationDbContext</c>. All mutation operations across repositories
/// are batched and persisted in a single call to <see cref="SaveChangesAsync"/>.
///
/// Usage pattern in a handler:
/// <code>
/// _unitOfWork.Users.Add(newUser);
/// _unitOfWork.UserRoles.Add(new UserRole(userId, roleId));
/// await _unitOfWork.SaveChangesAsync(cancellationToken);
/// </code>
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Gets the repository for <see cref="User"/> entities.</summary>
    IRepository<User> Users { get; }

    /// <summary>Gets the repository for <see cref="Role"/> entities.</summary>
    IRepository<Role> Roles { get; }

    /// <summary>Gets the repository for <see cref="UserRole"/> junction entities.</summary>
    IRepository<UserRole> UserRoles { get; }

    /// <summary>Gets the repository for <see cref="Project"/> entities.</summary>
    IRepository<Project> Projects { get; }

    /// <summary>Gets the repository for <see cref="Permission"/> entities.</summary>
    IRepository<Permission> Permissions { get; }

    /// <summary>Gets the repository for <see cref="RolePermission"/> junction entities.</summary>
    IRepository<RolePermission> RolePermissions { get; }

    /// <summary>
    /// Returns a composable query root for audit log entries.
    /// Exposed for read-only queries — audit entries are written automatically by
    /// the Infrastructure <c>ApplicationDbContext.SaveChangesAsync</c> override
    /// whenever a <see cref="Domain.Common.BaseEntity"/> is added, modified, or deleted.
    /// </summary>
    IQueryable<AuditLog> AuditLogs { get; }

    /// <summary>
    /// Persists all pending changes tracked across all repositories to the database
    /// in a single atomic operation.
    /// Audit fields (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy) are stamped by
    /// the Infrastructure <c>ApplicationDbContext.SaveChangesAsync</c> override before
    /// the SQL is issued — application code must never set them directly.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token to observe while waiting for the async operation to complete.
    /// Pass the token from the MediatR handler's <c>Handle</c> method.
    /// </param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
