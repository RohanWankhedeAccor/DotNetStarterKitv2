using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a role in the system that defines a set of permissions and responsibilities.
/// Roles are assigned to users through the UserRole junction table, enabling
/// flexible many-to-many role assignments across the user base.
///
/// Role names are unique within the system — the Infrastructure layer enforces
/// this via a unique index on the Name column in ApplicationDbContext configuration.
/// </summary>
public sealed class Role : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class with the specified name and description.
    /// </summary>
    /// <param name="name">The unique identifier name for the role (e.g., "Administrator", "Editor", "Viewer").</param>
    /// <param name="description">A human-readable description of the role's purpose and responsibilities.</param>
    public Role(string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class.
    /// This parameterless constructor is called exclusively by EF Core when materializing
    /// role records from the database via reflection. Direct invocation from application code is not possible.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Role()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets the unique name of this role.
    /// Examples: "Administrator", "Editor", "Viewer", "Support".
    /// Names are case-sensitive and unique across the system.
    /// Set exclusively during role creation; never reassigned.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets a human-readable description of this role's purpose and the permissions
    /// it grants. Examples: "System administrator with full access to all features."
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the collection of users assigned to this role via the UserRole junction table.
    /// This collection is populated exclusively by EF Core during query materialization;
    /// it must never be assigned directly from application code.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];
}
