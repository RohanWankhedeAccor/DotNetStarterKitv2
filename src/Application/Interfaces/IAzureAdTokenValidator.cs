using System.Security.Claims;

namespace Application.Interfaces;

/// <summary>
/// Service to validate JWT tokens issued by Azure AD.
/// Used in Phase 12: Azure AD Integration to validate tokens from MSAL.js.
///
/// The validator checks:
/// - Token signature validity using Azure AD public keys
/// - Issuer matches expected Azure AD tenant
/// - Audience matches API Application ID
/// - Token hasn't expired
/// - Token was issued for the correct client
/// </summary>
public interface IAzureAdTokenValidator
{
    /// <summary>
    /// Validates an Azure AD JWT token and extracts claims.
    /// </summary>
    /// <param name="token">The JWT token from the Authorization header (without "Bearer " prefix).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// ClaimsPrincipal containing validated claims if token is valid.
    /// Throws AzureAdTokenValidationException if validation fails.
    /// </returns>
    Task<ClaimsPrincipal> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts specific claim values from a validated ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal from ValidateTokenAsync.</param>
    /// <param name="claimType">The claim type to extract (e.g., ClaimTypes.NameIdentifier).</param>
    /// <returns>The claim value, or null if not found.</returns>
    string? GetClaimValue(ClaimsPrincipal principal, string claimType);
}
