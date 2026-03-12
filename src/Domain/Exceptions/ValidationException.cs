namespace Domain.Exceptions;

/// <summary>
/// Thrown by the MediatR ValidationBehavior pipeline in the Application layer when
/// one or more FluentValidation rules fail for an incoming command or query.
///
/// This exception is defined in the Domain layer (not Application) so that the
/// global exception handler middleware in the API layer can reference it without
/// introducing a dependency on FluentValidation in the API project.
///
/// The Errors dictionary shape matches FluentValidation's ValidationResult.ToDictionary()
/// output and is consumed directly by the frontend's typed ApiError.errors field,
/// enabling field-level error display in React Hook Form without additional mapping.
///
/// The global exception handler middleware maps this to HTTP 400 Bad Request with a
/// ProblemDetails response body (RFC 9457) whose 'errors' extension property contains
/// the field-level messages.
///
/// Usage in ValidationBehavior:
/// <code>
/// var failures = await validator.ValidateAsync(request, cancellationToken);
/// if (!failures.IsValid)
///     throw new ValidationException(failures.ToDictionary());
/// </code>
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> with
    /// a populated errors dictionary from FluentValidation.
    /// </summary>
    /// <param name="errors">
    /// A dictionary where each key is the property name (camelCase, matching the DTO field
    /// as returned by FluentValidation's ToDictionary()) and each value is a non-empty
    /// array of validation error messages for that property.
    /// Example: { "email": ["Email is required.", "Email must be a valid address."] }
    /// </param>
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>
    /// Gets the validation errors grouped by property name.
    /// Keys are property names in camelCase; values are non-empty arrays of error messages.
    /// This dictionary is serialized directly into the ProblemDetails 'errors' extension
    /// field by the global exception handler middleware.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }
}
