using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Junction entity linking a <see cref="Role"/> to a <see cref="Permission"/>.
/// Each row represents "this role has this permission".
///
/// The (RoleId, PermissionId) combination is unique — a composite unique index in
/// <c>RolePermissionConfiguration</c> enforces this constraint so that the same
/// permission cannot be granted to the same role twice.
/// </summary>
public sealed class RolePermission : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RolePermission"/> class.
    /// </summary>
    /// <param name="roleId">The identifier of the role being granted the permission.</param>
    /// <param name="permissionId">The identifier of the permission being granted.</param>
    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    /// <summary>
    /// Parameterless constructor for EF Core materialisation only.
    /// </summary>
    private RolePermission() { }

    /// <summary>Gets the identifier of the role that holds this permission.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>Gets the identifier of the permission granted to the role.</summary>
    public Guid PermissionId { get; private set; }

    /// <summary>
    /// Gets the Role navigation property.
    /// Populated by EF Core when an Include clause is present; otherwise null.
    /// </summary>
    public Role? Role { get; private set; }

    /// <summary>
    /// Gets the Permission navigation property.
    /// Populated by EF Core when an Include clause is present; otherwise null.
    /// </summary>
    public Permission? Permission { get; private set; }
}
