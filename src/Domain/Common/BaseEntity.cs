namespace Domain.Common;

/// <summary>
/// Abstract base class for all domain entities.
/// Provides a strongly-typed GUID identifier, a complete five-field audit trail,
/// and controlled soft-delete behaviour.
///
/// Audit fields (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy) are written
/// exclusively by the Infrastructure SaveChangesAsync override via ICurrentUserService
/// and IDateTimeService. Application or domain code must never set them directly.
///
/// Soft-delete is enforced through the <see cref="Delete"/> method and reversed
/// through <see cref="Restore"/>. The Infrastructure layer applies a global EF Core
/// query filter (HasQueryFilter(e => !e.IsDeleted)) on every BaseEntity subclass so
/// that deleted records are automatically excluded from all standard queries.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// Assigned once at object construction via Guid.NewGuid() and never reassigned.
    /// The init accessor prevents accidental reassignment after creation while still
    /// allowing EF Core to set the value during entity materialization from the database.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the UTC timestamp at which this entity was first persisted.
    /// Set automatically by the Infrastructure SaveChangesAsync override.
    /// Always stored as DateTimeOffset — never DateTime — to preserve timezone context
    /// across distributed environments and Azure SQL.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created this entity.
    /// Populated from ICurrentUserService.UserId by the Infrastructure SaveChangesAsync
    /// override. Defaults to an empty string until the first SaveChangesAsync call.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp of the most recent modification to this entity.
    /// Updated on every SaveChangesAsync call by the Infrastructure override.
    /// Always stored as DateTimeOffset — never DateTime.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last modified this entity.
    /// Populated from ICurrentUserService.UserId by the Infrastructure SaveChangesAsync
    /// override on every write operation.
    /// </summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this entity has been soft-deleted.
    /// When true, the Infrastructure global query filter excludes this entity from
    /// all standard EF Core queries. Call <see cref="Delete"/> to set this flag;
    /// never assign it directly from application or infrastructure code.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Marks this entity as soft-deleted.
    /// After this call the entity will be invisible to all queries that use the
    /// standard EF Core context (which applies HasQueryFilter(e => !e.IsDeleted)).
    /// This is the only permitted mutation point for IsDeleted — it centralises the
    /// business rule so that all deletion paths are auditable and testable.
    /// </summary>
    public void Delete()
    {
        // Use the controlled mutation method rather than a direct property set
        // so that subclasses can override and add pre-delete validation if required.
        IsDeleted = true;
    }

    /// <summary>
    /// Restores a previously soft-deleted entity, making it visible to standard queries again.
    /// Use only in administrative recovery scenarios with explicit role authorization.
    /// The Application layer command handler is responsible for verifying that the
    /// calling user has the Administrator role before invoking this method.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
    }
}
