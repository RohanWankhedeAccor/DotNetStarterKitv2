namespace Domain.Enums;

/// <summary>
/// Represents the lifecycle state of a user account.
///
/// Values are explicitly numbered starting at 1. This is a deliberate domain invariant:
/// the CLR default value for an uninitialized int (0) is never a valid status, which means
/// a User that was constructed without a status assignment will fail any status comparison
/// immediately rather than silently appearing as a valid state.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// The user account is active and the user may authenticate and use the platform.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The user account has been intentionally deactivated.
    /// Authentication attempts are rejected. Account data is preserved for audit.
    /// Reactivation is possible via an administrator action.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// The user account has been suspended, typically in response to a policy violation.
    /// Authentication attempts are rejected. Requires explicit administrator action to lift.
    /// Distinct from Inactive to allow different business rules and audit trails per state.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// The user has registered but has not yet confirmed their email address.
    /// Authentication is blocked until the user completes the activation flow.
    /// This is the initial state assigned by the User constructor.
    /// </summary>
    PendingActivation = 4
}
