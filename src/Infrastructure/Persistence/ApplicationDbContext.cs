using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
    public DbSet<Project> Projects => Set<Project>();

    /// <inheritdoc />
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <inheritdoc />
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <inheritdoc />
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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

        // Step 1: Stamp audit fields on all tracked BaseEntity subclasses.
        // This runs BEFORE BuildAuditEntries so that NewValues snapshots include
        // the correctly stamped CreatedAt / ModifiedAt timestamps.
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

                // EntityState.Deleted, Detached, Unchanged: no stamp required.
                // Soft-delete flows through BaseEntity.Delete() + EntityState.Modified,
                // which is caught by the Modified case above.
            }
        }

        // Step 2: Capture one AuditLog entry per changed entity.
        // Runs after stamping so that NewValues contains the final persisted values.
        // AuditLog does NOT inherit BaseEntity, so these entries are invisible to the
        // stamp loop above — no recursive audit-of-audit loop can occur.
        var auditEntries = BuildAuditEntries(now, userId);
        if (auditEntries.Count > 0)
            AuditLogs.AddRange(auditEntries);

        return await base.SaveChangesAsync(cancellationToken);
    }

    // ── Audit capture helpers ────────────────────────────────────────────────────

    // Reuse a single JsonSerializerOptions instance — creating options objects is expensive.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Builds one <see cref="AuditLog"/> entry per dirty <see cref="BaseEntity"/> in the
    /// ChangeTracker. Call this AFTER stamping audit fields so that <c>NewValues</c>
    /// captures the final in-memory state (including <c>CreatedAt</c> / <c>ModifiedAt</c>).
    /// </summary>
    private List<AuditLog> BuildAuditEntries(DateTimeOffset changedAt, string changedBy)
    {
        var entries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            var action = DetermineAction(entry);
            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            var entityId = entry.Entity.Id.ToString();

            // OldValues: null for new entities (no prior state in the database).
            string? oldValues = entry.State != EntityState.Added
                ? SerializePropertyValues(entry.OriginalValues)
                : null;

            // NewValues: null for hard-deleted entities (the row is being removed).
            string? newValues = entry.State != EntityState.Deleted
                ? SerializePropertyValues(entry.CurrentValues)
                : null;

            entries.Add(new AuditLog
            {
                TableName = tableName,
                EntityId = entityId,
                Action = action,
                ChangedBy = changedBy,
                ChangedAt = changedAt,
                OldValues = oldValues,
                NewValues = newValues,
            });
        }

        return entries;
    }

    /// <summary>
    /// Determines the <see cref="AuditAction"/> for a ChangeTracker entry.
    /// Distinguishes soft-delete from a regular update by comparing the <em>original</em>
    /// and <em>current</em> values of <c>IsDeleted</c>, not just whether the property
    /// was marked as modified — calling <c>DbSet.Update(entity)</c> marks ALL properties
    /// as modified regardless of whether their values actually changed.
    /// </summary>
    private static AuditAction DetermineAction(EntityEntry<BaseEntity> entry)
    {
        if (entry.State == EntityState.Added)
            return AuditAction.Created;

        if (entry.State == EntityState.Deleted)
            return AuditAction.Deleted;

        // EntityState.Modified — check whether IsDeleted VALUE actually changed.
        // IsModified alone is not sufficient: DbSet.Update() marks every property as
        // modified, so we compare original vs current to detect a real state transition.
        var originalIsDeleted = entry.OriginalValues[nameof(BaseEntity.IsDeleted)] is bool b && b;
        if (originalIsDeleted != entry.Entity.IsDeleted)
            return entry.Entity.IsDeleted ? AuditAction.Deleted : AuditAction.Restored;

        return AuditAction.Updated;
    }

    /// <summary>
    /// Serialises an EF Core <see cref="PropertyValues"/> bag to a JSON string.
    /// Navigation properties are excluded — only scalar columns are captured.
    /// </summary>
    private static string SerializePropertyValues(PropertyValues values)
    {
        var dict = values.Properties
            .ToDictionary(p => p.Name, p => values[p]);
        return JsonSerializer.Serialize(dict, JsonOptions);
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
