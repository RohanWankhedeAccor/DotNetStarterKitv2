namespace Domain.Exceptions;

/// <summary>
/// Thrown by the Application layer when an authentication attempt fails
/// (e.g., invalid credentials, expired token, missing authorization header).
///
/// The global exception handler middleware in the API layer catches this exception
/// and maps it to HTTP 401 Unauthorized with a ProblemDetails response body (RFC 9457).
///
/// Usage in handlers:
/// <code>
/// if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
///     throw new UnauthorizedException("Invalid email or password.");
/// </code>
/// </summary>
public sealed class UnauthorizedException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnauthorizedException"/>.
    /// </summary>
    /// <param name="message">A descriptive message about the authentication failure.</param>
    public UnauthorizedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="UnauthorizedException"/> with a message and inner exception.
    /// </summary>
    /// <param name="message">A descriptive message about the authentication failure.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
