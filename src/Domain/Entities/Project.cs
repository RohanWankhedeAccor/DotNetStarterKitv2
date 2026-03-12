using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Represents a project in the system.
/// Projects have a lifecycle (Draft → Active → Archived or Cancelled) and are owned
/// by a specific user who has administrative control over project settings and membership.
///
/// State machine transitions are enforced through domain methods (Activate, Archive, Cancel)
/// to ensure invalid transitions are prevented at the domain boundary, not just at the
/// Application layer. Attempting invalid transitions throws ConflictException.
/// </summary>
public sealed class Project : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Project"/> class with the specified details.
    /// The new project is created in Draft status, invisible to collaborators until activated.
    /// </summary>
    /// <param name="name">The project name or identifier (e.g., "Q4 Product Launch", "Mobile App v2.0").</param>
    /// <param name="description">A detailed description of the project's objectives, scope, and deliverables.</param>
    /// <param name="ownerId">The identifier of the user who owns and has administrative control over this project.</param>
    public Project(string name, string description, Guid ownerId)
    {
        Name = name;
        Description = description;
        OwnerId = ownerId;
        Status = ProjectStatus.Draft;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Project"/> class.
    /// This parameterless constructor is called exclusively by EF Core when materializing
    /// project records from the database via reflection. Direct invocation from application code is not possible.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Project()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets the project's name or unique identifier.
    /// Examples: "Q4 Product Launch", "Mobile App v2.0", "Customer Portal Redesign".
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets a detailed description of the project's objectives, scope, deliverables,
    /// and any other relevant context. This text is displayed throughout the UI to all
    /// project members to ensure alignment on goals and expectations.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who owns this project.
    /// The owner has administrative control over all project settings, members, and timeline.
    /// The owner is responsible for archiving or cancelling the project when complete.
    /// Set exclusively during project creation; never reassigned.
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    /// Gets or sets the current lifecycle status of the project.
    /// Initial status is Draft; transitions to Active after configuration is complete.
    /// Possible values and valid transitions:
    ///   Draft → Active (via Activate)
    ///   Active → Archived (via Archive)
    ///   Draft → Cancelled (via Cancel)
    ///   Active → Cancelled (via Cancel)
    /// </summary>
    public ProjectStatus Status { get; private set; }

    /// <summary>
    /// Gets the User entity that owns this project.
    /// This navigation property is populated exclusively by EF Core during query materialization
    /// when an Include(p => p.Owner) clause is present.
    /// It is nullable because EF Core may materialize this entity without fetching the related User.
    /// </summary>
    public User? Owner { get; private set; }

    /// <summary>
    /// Activates the project, changing status from Draft to Active.
    /// Once active, the project becomes visible to all assigned members and work can formally begin.
    /// Throws <see cref="ConflictException"/> if the project is not in Draft status.
    /// </summary>
    /// <exception cref="ConflictException">
    /// Thrown if the project status is not Draft, preventing invalid state transitions.
    /// </exception>
    public void Activate()
    {
        if (Status != ProjectStatus.Draft)
        {
            throw new ConflictException($"Project '{Name}' cannot be activated because it is currently '{Status}'.");
        }

        Status = ProjectStatus.Active;
    }

    /// <summary>
    /// Archives the project, changing status from Active to Archived.
    /// Once archived, the project is moved to read-only state and no further modifications are permitted.
    /// All project data is retained for historical reference and audit purposes.
    /// Throws <see cref="ConflictException"/> if the project is not in Active status.
    /// </summary>
    /// <exception cref="ConflictException">
    /// Thrown if the project status is not Active, preventing invalid state transitions.
    /// </exception>
    public void Archive()
    {
        if (Status != ProjectStatus.Active)
        {
            throw new ConflictException($"Project '{Name}' cannot be archived because it is currently '{Status}'.");
        }

        Status = ProjectStatus.Archived;
    }

    /// <summary>
    /// Cancels the project, changing status to Cancelled.
    /// Cancelled projects are treated as inactive but data is retained for audit history.
    /// This method can be called from Draft or Active states.
    /// Throws <see cref="ConflictException"/> if the project is already Archived or Cancelled.
    /// </summary>
    /// <exception cref="ConflictException">
    /// Thrown if the project is already in a terminal state (Archived or Cancelled),
    /// preventing invalid state transitions.
    /// </exception>
    public void Cancel()
    {
        if (Status == ProjectStatus.Archived || Status == ProjectStatus.Cancelled)
        {
            throw new ConflictException($"Project '{Name}' cannot be cancelled because it is currently '{Status}'.");
        }

        Status = ProjectStatus.Cancelled;
    }
}
