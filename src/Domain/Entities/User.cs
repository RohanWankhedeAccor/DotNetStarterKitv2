using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a user account in the system.
/// Users are assigned roles through the UserRole junction table and can be in various
/// lifecycle states (Active, Inactive, Suspended, PendingActivation).
///
/// Email addresses are unique within the system — the Infrastructure layer enforces
/// this via a unique index on the Email column in ApplicationDbContext configuration.
/// </summary>
public sealed class User : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class with the specified credentials and details.
    /// The new user is created in PendingActivation status, requiring email confirmation before
    /// the user can authenticate.
    /// </summary>
    /// <param name="email">The user's email address (must be unique across the system).</param>
    /// <param name="fullName">The user's full name or display name.</param>
    /// <param name="passwordHash">The bcrypt-hashed password (never store plaintext). Can be null for SSO/Azure AD users.</param>
    public User(string email, string fullName, string? passwordHash)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        Status = UserStatus.PendingActivation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// This parameterless constructor is called exclusively by EF Core when materializing
    /// user records from the database via reflection. Direct invocation from application code is not possible.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private User()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets the user's email address.
    /// Email addresses are globally unique; a unique index enforces this constraint in the database.
    /// Set exclusively during user creation; never reassigned.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Gets the user's full name or display name.
    /// This is the name displayed throughout the UI (e.g., in user menus, activity feeds, role displays).
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Gets the bcrypt-hashed representation of the user's password.
    /// This is never the plaintext password; it is always the result of bcrypt.HashPassword().
    /// Comparison during authentication uses bcrypt.Verify(plaintext, this value).
    /// May be null for users created via single sign-on (SSO) / OAuth flows that don't use passwords.
    /// </summary>
    public string? PasswordHash { get; private set; }

    /// <summary>
    /// Gets or sets the current lifecycle status of the user account.
    /// Determines whether the user may authenticate and use the system.
    /// Possible values: Active, Inactive, Suspended, PendingActivation.
    /// Initial status is always PendingActivation; transitions to Active after email confirmation.
    /// </summary>
    public UserStatus Status { get; internal set; }

    /// <summary>
    /// Gets the collection of roles assigned to this user via the UserRole junction table.
    /// This collection is populated exclusively by EF Core during query materialization;
    /// it must never be assigned directly from application code.
    /// Roles determine the user's permissions and what features they can access.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    /// <summary>
    /// Gets the Azure AD object identifier (OID) for users who authenticate via Azure AD.
    /// This is the immutable unique identifier assigned by Azure AD to the user.
    /// Null for users who authenticate only via local password (email/password login).
    /// Part of Phase 12: Azure AD Integration.
    /// </summary>
    public string? AzureAdObjectId { get; internal set; }

    /// <summary>
    /// Gets the authentication source for this user account.
    /// Indicates whether the user authenticates via Local (password-based) or AzureAd.
    /// Default is 'Local' for backward compatibility.
    /// Part of Phase 12: Azure AD Integration.
    /// </summary>
    public string AuthSource { get; internal set; } = "Local";

    /// <summary>
    /// Activates the user account, changing status from PendingActivation to Active.
    /// Called after the user confirms their email address via the confirmation link.
    /// Once active, the user may authenticate and access the system.
    /// </summary>
    public void Activate()
    {
        Status = UserStatus.Active;
    }

    /// <summary>
    /// Deactivates the user account, changing status to Inactive.
    /// An inactive account cannot authenticate or access the system.
    /// Deactivation is used when a user is no longer active but data must be retained for audit history.
    /// Use Restore() to reactivate a deactivated account.
    /// </summary>
    public void Deactivate()
    {
        Status = UserStatus.Inactive;
    }

    /// <summary>
    /// Suspends the user account, changing status to Suspended.
    /// A suspended account cannot authenticate and may have additional restrictions enforced
    /// by the application (e.g., no access to certain features, no ability to create new records).
    /// Suspension is distinct from deactivation and is typically used for policy violations.
    /// Use a restore command to lift the suspension (handled in the Application layer).
    /// </summary>
    public void Suspend()
    {
        Status = UserStatus.Suspended;
    }

    /// <summary>
    /// Provisions this user for Azure AD authentication by setting the Azure AD object ID and auth source.
    /// Called during user creation from Azure AD token validation (Phase 12).
    /// </summary>
    /// <param name="azureAdObjectId">The immutable Azure AD object ID (OID) from Azure AD token.</param>
    public void ProvisionAzureAd(string azureAdObjectId)
    {
        AzureAdObjectId = azureAdObjectId;
        AuthSource = "AzureAd";
    }

    /// <summary>
    /// Updates the user's display name.
    /// Called during sync from Azure AD to keep profile information current.
    /// </summary>
    /// <param name="fullName">The new full name from Azure AD.</param>
    public void UpdateDisplayName(string fullName)
    {
        FullName = fullName;
    }
}
