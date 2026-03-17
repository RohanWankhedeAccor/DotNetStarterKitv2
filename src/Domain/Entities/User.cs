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
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">The user's email address (must be unique across the system).</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="passwordHash">The bcrypt-hashed password. Can be null for SSO/Azure AD users.</param>
    /// <param name="username">Optional unique handle. Null for SSO/seeded users.</param>
    public User(string email, string firstName, string lastName, string? passwordHash, string? username = null)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
        Username = username;
        Status = UserStatus.PendingActivation;
    }

    /// <summary>
    /// EF Core materialisation constructor — not for application use.
    /// </summary>
#pragma warning disable CS8618
    private User() { }
#pragma warning restore CS8618

    /// <summary>Gets the user's email address. Globally unique; enforced by a unique index.</summary>
    public string Email { get; private set; }

    /// <summary>Gets the user's unique username (handle). Nullable — not required for SSO users.</summary>
    public string? Username { get; internal set; }

    /// <summary>Gets the user's first name.</summary>
    public string FirstName { get; private set; }

    /// <summary>Gets the user's last name.</summary>
    public string LastName { get; private set; }

    /// <summary>
    /// Gets the bcrypt-hashed password. Never the plaintext value.
    /// Null for SSO/Azure AD users who have no local password.
    /// </summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Gets the current lifecycle status of the user account.</summary>
    public UserStatus Status { get; internal set; }

    /// <summary>Gets the roles assigned to this user via the UserRole junction table.</summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    /// <summary>Gets the Azure AD object identifier for users who authenticate via Azure AD.</summary>
    public string? AzureAdObjectId { get; internal set; }

    /// <summary>Gets the authentication source: "Local" or "AzureAd".</summary>
    public string AuthSource { get; internal set; } = "Local";

    /// <summary>Activates the user account after email confirmation.</summary>
    public void Activate() => Status = UserStatus.Active;

    /// <summary>Deactivates the user account.</summary>
    public void Deactivate() => Status = UserStatus.Inactive;

    /// <summary>Suspends the user account.</summary>
    public void Suspend() => Status = UserStatus.Suspended;

    /// <summary>Provisions this user for Azure AD authentication.</summary>
    public void ProvisionAzureAd(string azureAdObjectId)
    {
        AzureAdObjectId = azureAdObjectId;
        AuthSource = "AzureAd";
    }

    /// <summary>Updates the user's name. Called during Azure AD sync to keep profile current.</summary>
    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}
