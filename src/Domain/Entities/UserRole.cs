using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents the junction table entry linking a User to a Role in a many-to-many relationship.
/// This entity enables flexible role assignment: a single user can have multiple roles,
/// and a single role can be assigned to multiple users.
///
/// The (UserId, RoleId) combination is unique in the system — the Infrastructure layer enforces
/// this via a unique composite index on (UserId, RoleId) in ApplicationDbContext configuration.
/// </summary>
public sealed class UserRole : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRole"/> class with the specified user and role identifiers.
    /// </summary>
    /// <param name="userId">The globally unique identifier of the user being assigned the role.</param>
    /// <param name="roleId">The globally unique identifier of the role to assign to the user.</param>
    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRole"/> class.
    /// This parameterless constructor is called exclusively by EF Core when materializing
    /// user-role assignments from the database via reflection. Direct invocation from application code is not possible.
    /// </summary>
    private UserRole()
    {
    }

    /// <summary>
    /// Gets the identifier of the user in this role assignment.
    /// This is a foreign key to the User entity.
    /// Set exclusively during assignment creation; never reassigned.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the identifier of the role assigned to the user.
    /// This is a foreign key to the Role entity.
    /// Set exclusively during assignment creation; never reassigned.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Gets the User entity to which this role is assigned.
    /// This navigation property is populated exclusively by EF Core during query materialization
    /// when an Include(ur => ur.User) clause is present.
    /// It is nullable because EF Core may materialize this entity without fetching the related User.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Gets the Role entity that is assigned to the user.
    /// This navigation property is populated exclusively by EF Core during query materialization
    /// when an Include(ur => ur.Role) clause is present.
    /// It is nullable because EF Core may materialize this entity without fetching the related Role.
    /// </summary>
    public Role? Role { get; private set; }
}
