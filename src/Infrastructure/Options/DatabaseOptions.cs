using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for EF Core / SQL Server connection behaviour.
/// Bound from the <c>Database</c> section in appsettings.json.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Number of automatic retries on transient SQL failures (e.g. connection timeouts).
    /// Defaults to 3. Set to 0 to disable retries.
    /// </summary>
    [Range(0, 10, ErrorMessage = "Database:MaxRetryCount must be between 0 and 10.")]
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>
    /// Maximum delay between retries in seconds.
    /// Defaults to 5 seconds.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Database:MaxRetryDelaySeconds must be between 1 and 60.")]
    public int MaxRetryDelaySeconds { get; init; } = 5;
}
