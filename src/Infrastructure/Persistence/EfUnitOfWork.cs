using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>.
/// Owns one <see cref="ApplicationDbContext"/> instance (scoped per HTTP request)
/// and exposes a typed <see cref="Repository{T}"/> for each aggregate root.
///
/// All repositories share the same <see cref="ApplicationDbContext"/> so their
/// ChangeTracker state is unified — changes to Users and UserRoles in the same
/// handler are committed atomically in a single <c>SaveChangesAsync</c> call.
///
/// Lifetime: <c>Scoped</c> — one instance per HTTP request, matching the lifetime
/// of <see cref="ApplicationDbContext"/>.
/// </summary>
internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new <see cref="EfUnitOfWork"/> and creates all typed repositories.
    /// </summary>
    public EfUnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        Roles = new Repository<Role>(context);
        UserRoles = new Repository<UserRole>(context);
        Products = new Repository<Product>(context);
        Projects = new Repository<Project>(context);
        Permissions = new Repository<Permission>(context);
        RolePermissions = new Repository<RolePermission>(context);
    }

    /// <inheritdoc />
    public IRepository<User> Users { get; }

    /// <inheritdoc />
    public IRepository<Role> Roles { get; }

    /// <inheritdoc />
    public IRepository<UserRole> UserRoles { get; }

    /// <inheritdoc />
    public IRepository<Product> Products { get; }

    /// <inheritdoc />
    public IRepository<Project> Projects { get; }

    /// <inheritdoc />
    public IRepository<Permission> Permissions { get; }

    /// <inheritdoc />
    public IRepository<RolePermission> RolePermissions { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Exposes the AuditLogs DbSet as a raw IQueryable for read-only queries.
    /// Writes happen automatically inside ApplicationDbContext.SaveChangesAsync.
    /// </remarks>
    public IQueryable<AuditLog> AuditLogs => _context.AuditLogs.AsNoTracking();

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="ApplicationDbContext.SaveChangesAsync(CancellationToken)"/>,
    /// which stamps audit fields (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy) before
    /// issuing SQL — application code must never set audit fields directly.
    /// </remarks>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
