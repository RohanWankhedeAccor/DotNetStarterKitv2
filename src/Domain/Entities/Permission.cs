using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a discrete action or capability in the system.
/// Permissions are assigned to <see cref="Role"/> entities via the
/// <see cref="RolePermission"/> junction table, forming the RBAC permission catalogue.
///
/// Permission names follow dot-separated resource.action notation (e.g. "users.create").
/// Names are unique within the system — a unique index in <c>PermissionConfiguration</c>
/// enforces this constraint.
/// </summary>
public sealed class Permission : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Permission"/> class.
    /// </summary>
    /// <param name="name">Unique dot-separated key (e.g. "users.create").</param>
    /// <param name="description">Human-readable description of what the permission grants.</param>
    public Permission(string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Parameterless constructor for EF Core materialisation only.
    /// </summary>
#pragma warning disable CS8618
    private Permission() { }
#pragma warning restore CS8618

    /// <summary>
    /// Gets the unique dot-separated key for this permission (e.g. "users.create").
    /// Set at creation time; never reassigned.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets a human-readable description of what this permission allows.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the roles that have been granted this permission via the RolePermissions junction table.
    /// Populated exclusively by EF Core during query materialisation when an Include clause is present.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];
}
