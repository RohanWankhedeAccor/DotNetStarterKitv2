namespace Domain.Exceptions;

/// <summary>
/// Thrown when the authenticated user attempts an operation they are not
/// authorized to perform based on role or resource ownership checks.
///
/// The global exception handler middleware in the API layer catches this exception
/// and maps it to HTTP 403 Forbidden with a ProblemDetails response body (RFC 9457).
///
/// The exception message is intentionally fixed and non-descriptive. Returning
/// detailed authorization failure reasons would leak information about the system's
/// permission structure to potentially unauthorized callers — this is prohibited by
/// Security Rules in DEVELOPMENT_RULES.md Section 7.
///
/// Usage in handlers:
/// <code>
/// if (project.OwnerId != _currentUserService.UserId)
///     throw new ForbiddenException();
/// </code>
/// </summary>
public sealed class ForbiddenException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForbiddenException"/>.
    /// The message is fixed and intentionally vague for security reasons —
    /// do not add an overload that accepts a caller-supplied message.
    /// </summary>
    public ForbiddenException()
        : base("You do not have permission to perform this action.")
    {
    }
}
