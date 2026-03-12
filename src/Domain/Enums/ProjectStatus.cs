namespace Domain.Enums;

/// <summary>
/// Represents the lifecycle state of a project.
///
/// The valid state machine transitions are:
///   Draft → Active      (via Project.Activate)
///   Active → Archived   (via Project.Archive)
///   Draft → Cancelled   (via Project.Cancel)
///   Active → Cancelled  (via Project.Cancel)
///
/// Archived and Cancelled are terminal states — no further transitions are permitted.
/// These invariants are enforced in the Project entity's domain methods.
///
/// Values are explicitly numbered starting at 1 so that an uninitialized int (0)
/// is never silently treated as a valid project status.
/// </summary>
public enum ProjectStatus
{
    /// <summary>
    /// The project is being defined. It is not yet visible to collaborators and
    /// no work has formally begun. This is the initial state assigned by the Project constructor.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// The project is in progress and fully visible to all assigned members.
    /// Work items, deadlines, and resources are actively managed in this state.
    /// </summary>
    Active = 2,

    /// <summary>
    /// The project has been completed and moved to read-only archive.
    /// No further modifications are permitted. All data is retained for historical reference.
    /// This is a terminal state.
    /// </summary>
    Archived = 3,

    /// <summary>
    /// The project was cancelled before completion.
    /// All data is retained for audit history but the project is treated as inactive.
    /// This is a terminal state.
    /// </summary>
    Cancelled = 4
}
