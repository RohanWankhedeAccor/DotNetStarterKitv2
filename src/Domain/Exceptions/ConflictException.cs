namespace Domain.Exceptions;

/// <summary>
/// Thrown when a requested operation would violate a business rule or create a data conflict.
///
/// Common scenarios where this exception is appropriate:
/// - Duplicate unique values (e.g., email already registered, role name already exists)
/// - Invalid state machine transitions (e.g., trying to Archive a Draft project)
/// - Optimistic concurrency violations caught at the SaveChangesAsync boundary
///
/// The global exception handler middleware in the API layer catches this exception
/// and maps it to HTTP 409 Conflict with a ProblemDetails response body (RFC 9457).
/// The exception message is surfaced directly in the ProblemDetails detail field,
/// so callers must provide a human-readable, actionable message — never a technical one.
///
/// This exception is also thrown from within domain entity state transition methods
/// (e.g., Project.Activate, Project.Cancel) to enforce state machine invariants at
/// the domain level. Because it is defined in Domain.Exceptions, entities can throw
/// it without introducing any external dependency.
/// </summary>
public sealed class ConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConflictException"/>.
    /// </summary>
    /// <param name="message">
    /// A human-readable description of the conflict, suitable for display in the API
    /// response ProblemDetails detail field. Must be actionable — explain what
    /// conflicted and what the caller should do to resolve it.
    /// Example: "A role named 'Administrator' already exists."
    /// Example: "Project 'Q4 Launch' cannot be archived because it is currently 'Draft'."
    /// </param>
    public ConflictException(string message)
        : base(message)
    {
    }
}
