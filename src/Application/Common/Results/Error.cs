namespace Application.Common.Results;

/// <summary>
/// Represents a domain error with a machine-readable code and a human-readable description.
/// Use the static factory methods to create well-known error types.
/// </summary>
public sealed record Error(string Code, string Description)
{
    /// <summary>Sentinel value representing the absence of an error (success state).</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Creates a 404-class error for a resource that could not be found.</summary>
    public static Error NotFound(string resource, object id) =>
        new("NotFound", $"{resource} '{id}' was not found.");

    /// <summary>Creates a 409-class error for a uniqueness or state conflict.</summary>
    public static Error Conflict(string description) => new("Conflict", description);

    /// <summary>Creates a 401-class error for unauthenticated or invalid-credentials scenarios.</summary>
    public static Error Unauthorized(string description) => new("Unauthorized", description);

    /// <summary>Creates a 403-class error for authorised-but-not-permitted scenarios.</summary>
    public static Error Forbidden(string description) => new("Forbidden", description);
}
