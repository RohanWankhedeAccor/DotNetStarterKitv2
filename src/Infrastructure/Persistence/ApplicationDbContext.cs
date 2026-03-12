using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// The primary EF Core database context for the application.
/// Implements <see cref="IApplicationDbContext"/> so that Application layer handlers
/// depend on the interface abstraction — never on this concrete class directly.
///
/// Responsibilities:
/// 1. Exposes <see cref="DbSet{T}"/> properties for all domain entities.
/// 2. Applies all <c>IEntityTypeConfiguration&lt;T&gt;</c> classes from this assembly
///    via <c>ApplyConfigurationsFromAssembly</c> in <see cref="OnModelCreating"/>.
/// 3. Overrides <see cref="SaveChangesAsync(CancellationToken)"/> to stamp audit fields
///    (<c>CreatedAt</c>, <c>CreatedBy</c>, <c>ModifiedAt</c>, <c>ModifiedBy</c>) using
///    <see cref="ICurrentUserService"/> and <see cref="IDateTimeService"/> — application
///    code must never set these fields directly.
/// 4. Sets the default column type for all <see cref="DateTimeOffset"/> properties to
///    <c>datetimeoffset(7)</c> via <see cref="ConfigureConventions"/>.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationDbContext"/>.
    /// </summary>
    /// <param name="options">
    /// EF Core context options including the SQL Server connection string.
    /// Supplied by the DI container via <c>AddDbContext</c> in the Infrastructure DI setup.
    /// </param>
    /// <param name="currentUserService">
    /// Service that resolves the current authenticated user's identifier.
    /// Used exclusively in <see cref="SaveChangesAsync(CancellationToken)"/> to
    /// populate audit fields — never used in queries.
    /// </param>
    /// <param name="dateTimeService">
    /// Service that provides the current UTC timestamp.
    /// Used exclusively in <see cref="SaveChangesAsync(CancellationToken)"/> to
    /// populate audit fields — never used in queries.
    /// </param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
        : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    /// <inheritdoc />
    public DbSet<User> Users => Set<User>();

    /// <inheritdoc />
    public DbSet<Role> Roles => Set<Role>();

    /// <inheritdoc />
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <inheritdoc />
    public DbSet<Product> Products => Set<Product>();

    /// <inheritdoc />
    public DbSet<Project> Projects => Set<Project>();

    /// <inheritdoc />
    /// <remarks>
    /// Before delegating to the base EF Core implementation, this override iterates
    /// the EF Core ChangeTracker entries of type <see cref="BaseEntity"/> and
    /// stamps the following audit fields:
    ///
    /// For <c>EntityState.Added</c>:
    ///   - <c>CreatedAt</c> = current UTC time
    ///   - <c>CreatedBy</c> = current user ID string
    ///   - <c>ModifiedAt</c> = current UTC time
    ///   - <c>ModifiedBy</c> = current user ID string
    ///
    /// For <c>EntityState.Modified</c>:
    ///   - <c>ModifiedAt</c> = current UTC time
    ///   - <c>ModifiedBy</c> = current user ID string
    ///
    /// <c>CreatedAt</c> and <c>CreatedBy</c> are intentionally NOT updated on Modified
    /// entities — they represent immutable creation metadata.
    /// </remarks>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeService.Now;
        var userId = _currentUserService.UserIdString;

        // Stamp audit fields on all tracked BaseEntity subclasses before persisting.
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // CreatedAt/CreatedBy are set only once — at initial insertion.
                    // They are never overwritten on subsequent saves.
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    // ModifiedAt/ModifiedBy are also set on insert so that the
                    // first record of modification is always the creation itself.
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = userId;
                    break;

                case EntityState.Modified:
                    // Only update the modification trail — creation metadata is immutable.
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = userId;
                    break;

                // EntityState.Deleted, Detached, Unchanged: no audit action required.
                // Soft-delete flows through BaseEntity.Delete() + EntityState.Modified,
                // which is caught by the Modified case above.
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Sets the default column type for all <see cref="DateTimeOffset"/> properties to
    /// <c>datetimeoffset(7)</c> (SQL Server's highest precision, 100-nanosecond ticks).
    /// This applies globally so individual entity configurations do not need to repeat it.
    ///
    /// No SQL Server-specific types are used in Domain or Application — this convention
    /// is the single location where the SQL Server storage type is declared.
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Set SQL Server datetimeoffset column type for all DateTimeOffset properties.
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveColumnType("datetimeoffset(7)");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Discovers and applies all <see cref="IEntityTypeConfiguration{T}"/> implementations
    /// in this assembly (the Infrastructure assembly) using
    /// <c>ApplyConfigurationsFromAssembly</c>. Adding a new entity configuration file
    /// to the <c>Configurations/</c> folder is sufficient — no manual registration needed.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Discover all IEntityTypeConfiguration<T> implementations in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
