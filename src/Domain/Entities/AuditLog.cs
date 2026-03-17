using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Immutable record of a single change event on a domain entity.
///
/// <para>
/// Unlike other entities, <see cref="AuditLog"/> deliberately does NOT inherit
/// <see cref="Domain.Common.BaseEntity"/> for two reasons:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///       <b>Recursive audit loop prevention</b> — the Infrastructure
///       <c>SaveChangesAsync</c> override iterates <c>ChangeTracker.Entries&lt;BaseEntity&gt;()</c>.
///       If AuditLog inherited BaseEntity, each AuditLog entry would trigger another
///       AuditLog entry, causing an infinite recursion.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Schema clarity</b> — AuditLog has its own <see cref="ChangedAt"/> /
///       <see cref="ChangedBy"/> fields that carry the same semantics as
///       <c>CreatedAt</c> / <c>CreatedBy</c> without the overhead of
///       <c>ModifiedAt</c>, <c>ModifiedBy</c>, and <c>IsDeleted</c>.
///       Audit records are immutable and must never be soft-deleted.
///     </description>
///   </item>
/// </list>
///
/// All properties use <c>init</c> accessors — audit entries can be constructed
/// with object-initialiser syntax but are immutable thereafter.
/// </summary>
public sealed class AuditLog
{
    /// <summary>Unique identifier assigned once at construction.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The database table name of the entity that changed.
    /// Derived from EF Core's <c>IEntityType.GetTableName()</c>.
    /// </summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// The primary key of the changed entity serialised as a string.
    /// Storing as a string decouples the AuditLog schema from the entity's key type.
    /// </summary>
    public string EntityId { get; init; } = string.Empty;

    /// <summary>The kind of change that occurred.</summary>
    public AuditAction Action { get; init; }

    /// <summary>
    /// Identifier of the user who triggered the change.
    /// Populated from <c>ICurrentUserService.UserIdString</c> by the Infrastructure layer.
    /// Empty string when the change originates from a background job or seed operation.
    /// </summary>
    public string ChangedBy { get; init; } = string.Empty;

    /// <summary>UTC timestamp of when the change was persisted to the database.</summary>
    public DateTimeOffset ChangedAt { get; init; }

    /// <summary>
    /// JSON snapshot of the entity's property values <b>before</b> the change.
    /// <c>null</c> for <see cref="AuditAction.Created"/> entries (no prior state exists).
    /// </summary>
    public string? OldValues { get; init; }

    /// <summary>
    /// JSON snapshot of the entity's property values <b>after</b> the change.
    /// <c>null</c> for hard-deleted entities.
    /// </summary>
    public string? NewValues { get; init; }
}
