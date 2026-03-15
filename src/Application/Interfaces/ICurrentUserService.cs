namespace Application.Interfaces;

/// <summary>
/// Abstracts the identity of the currently authenticated user so that Application
/// layer handlers can stamp audit fields and enforce resource-ownership checks
/// without any dependency on ASP.NET Core or HTTP infrastructure.
///
/// Phase 1 (current): returns a mocked Guid.Empty during development.
/// Phase 2: replaced by an MSAL / Entra ID implementation in Infrastructure.Identity
/// that reads the NameIdentifier claim from <see cref="System.Security.Claims.ClaimsPrincipal"/>.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the identifier of the authenticated user extracted from the current request's
    /// security context. Returns <see cref="Guid.Empty"/> when no authenticated user
    /// is present (anonymous requests or Phase 1 mock mode).
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the string representation of <see cref="UserId"/> used to populate
    /// the <c>CreatedBy</c> and <c>ModifiedBy</c> audit fields on <c>BaseEntity</c>.
    /// Returns an empty string when no authenticated user is present.
    /// </summary>
    string UserIdString { get; }

    /// <summary>
    /// Gets the role names of the currently authenticated user extracted from JWT claims.
    /// Returns an empty enumerable when no authenticated user is present.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Gets the permission keys of the currently authenticated user extracted from JWT claims.
    /// Returns an empty enumerable when no authenticated user is present.
    /// </summary>
    IEnumerable<string> Permissions { get; }
}
