namespace Domain.Exceptions;

/// <summary>
/// Thrown when Azure AD token validation fails.
/// Maps to HTTP 401 Unauthorized in the exception handler middleware.
/// Part of Phase 12: Azure AD Integration.
/// </summary>
public sealed class AzureAdTokenValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdTokenValidationException"/> class.
    /// </summary>
    /// <param name="message">Descriptive message about the validation failure.</param>
    public AzureAdTokenValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdTokenValidationException"/> class.
    /// </summary>
    /// <param name="message">Descriptive message about the validation failure.</param>
    /// <param name="innerException">The underlying exception that caused the validation failure.</param>
    public AzureAdTokenValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
