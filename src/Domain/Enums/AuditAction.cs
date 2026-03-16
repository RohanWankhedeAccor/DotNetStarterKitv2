namespace Domain.Enums;

/// <summary>
/// Describes the type of change recorded in an <see cref="Domain.Entities.AuditLog"/> entry.
/// Values start at 1 so that the default int value (0) is never silently treated as valid.
/// </summary>
public enum AuditAction
{
    /// <summary>A new entity was inserted into the database.</summary>
    Created = 1,

    /// <summary>An existing entity's scalar properties were modified.</summary>
    Updated = 2,

    /// <summary>An entity was soft-deleted (<c>IsDeleted</c> set to <c>true</c>).</summary>
    Deleted = 3,

    /// <summary>A previously soft-deleted entity was restored (<c>IsDeleted</c> set to <c>false</c>).</summary>
    Restored = 4,
}
