using Application.Interfaces;

namespace Infrastructure.Identity;

/// <summary>
/// Provides the current UTC wall-clock time via <see cref="IDateTimeService"/>.
/// This thin wrapper exists solely to allow unit tests to substitute a deterministic
/// clock mock via NSubstitute, making audit field assertions stable across CI runs
/// and developer machines in different time zones.
///
/// All audit timestamps in <c>ApplicationDbContext.SaveChangesAsync</c> go through
/// this service — never through <c>DateTimeOffset.UtcNow</c> directly.
/// </summary>
public sealed class DateTimeService : IDateTimeService
{
    /// <inheritdoc />
    /// <remarks>
    /// Always returns <see cref="DateTimeOffset.UtcNow"/> — the UTC offset is always +00:00.
    /// This ensures consistent timestamp ordering across Azure regions and avoids the
    /// well-known DST-induced ordering bugs that occur when <c>DateTime</c> or local-timezone
    /// <c>DateTimeOffset</c> values are compared after cross-region data replication.
    /// </remarks>
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
