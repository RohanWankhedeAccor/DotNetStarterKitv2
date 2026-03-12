namespace Application.Interfaces;

/// <summary>
/// Abstracts the system clock so that Application layer handlers and the
/// Infrastructure DbContext override can obtain the current UTC timestamp
/// without calling <c>DateTimeOffset.UtcNow</c> directly.
///
/// This indirection is the minimal requirement for deterministic unit tests:
/// substitute an NSubstitute mock that returns a fixed timestamp to make
/// audit field assertions stable across CI runs.
/// </summary>
public interface IDateTimeService
{
    /// <summary>
    /// Gets the current UTC date and time as a <see cref="DateTimeOffset"/>.
    /// Always UTC — never local time — to ensure consistency across Azure regions
    /// and developer machines in different time zones.
    /// </summary>
    DateTimeOffset Now { get; }
}
