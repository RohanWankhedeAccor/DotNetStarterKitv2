using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

/// <summary>
/// Defines the Application layer's view of the database context.
/// Handlers depend on this interface — never on the concrete <c>ApplicationDbContext</c>
/// class — so that unit tests can substitute an in-memory or NSubstitute mock without
/// standing up SQL Server or the full EF Core pipeline.
///
/// Only <c>DbSet&lt;T&gt;</c> properties and <c>SaveChangesAsync</c> are exposed here.
/// Infrastructure-specific methods (migrations, raw SQL, EnsureCreated) are intentionally
/// excluded to preserve the Application layer's infrastructure ignorance.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>Gets the <see cref="DbSet{User}"/> for querying and persisting user records.</summary>
    DbSet<User> Users { get; }

    /// <summary>Gets the <see cref="DbSet{Role}"/> for querying and persisting role records.</summary>
    DbSet<Role> Roles { get; }

    /// <summary>Gets the <see cref="DbSet{UserRole}"/> for querying and persisting user-to-role assignments.</summary>
    DbSet<UserRole> UserRoles { get; }

    /// <summary>Gets the <see cref="DbSet{Project}"/> for querying and persisting project records.</summary>
    DbSet<Project> Projects { get; }

    /// <summary>Gets the <see cref="DbSet{Permission}"/> for querying and persisting permission records.</summary>
    DbSet<Permission> Permissions { get; }

    /// <summary>Gets the <see cref="DbSet{RolePermission}"/> for querying and persisting role-permission assignments.</summary>
    DbSet<RolePermission> RolePermissions { get; }

    /// <summary>
    /// Saves all pending changes in the current unit of work to the database.
    /// The Infrastructure implementation's override automatically stamps audit fields
    /// (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy) before delegating to EF Core.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token to observe while waiting for the async operation to complete.
    /// Pass the token from the MediatR handler's <c>Handle</c> method — never
    /// create a new <c>CancellationToken.None</c> at the call site.
    /// </param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
