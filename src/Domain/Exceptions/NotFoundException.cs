namespace Domain.Exceptions;

/// <summary>
/// Thrown by Application layer command and query handlers when a requested entity
/// cannot be located by its identifier.
///
/// The global exception handler middleware in the API layer catches this exception
/// and maps it to HTTP 404 Not Found with a ProblemDetails response body (RFC 9457).
///
/// Usage in handlers:
/// <code>
/// var project = await _context.Projects.FindAsync(id, cancellationToken)
///     ?? throw new NotFoundException(nameof(Project), id);
/// </code>
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="name">
    /// The entity type name. Use nameof(EntityClass) at the call site to avoid
    /// magic strings (e.g., nameof(Project), nameof(User)).
    /// </param>
    /// <param name="key">
    /// The identifier value that was searched for and not found.
    /// Accepts object so that both Guid and string keys are supported without overloads.
    /// </param>
    public NotFoundException(string name, object key)
        : base($"Entity '{name}' with key '{key}' was not found.")
    {
        EntityName = name;
        Key = key;
    }

    /// <summary>
    /// Gets the entity type name that was searched for.
    /// Exposed so that the exception handler middleware can include it in the
    /// ProblemDetails title without re-parsing the message string.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Gets the key value that was not found.
    /// Included in the ProblemDetails detail field by the exception handler middleware.
    /// </summary>
    public object Key { get; }
}
