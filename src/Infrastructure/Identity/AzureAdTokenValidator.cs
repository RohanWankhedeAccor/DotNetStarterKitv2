using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

/// <summary>
/// Validates JWT tokens issued by Azure AD.
/// Implements <see cref="IAzureAdTokenValidator"/>.
/// Part of Phase 12: Azure AD Integration.
///
/// Uses Microsoft.IdentityModel library to:
/// 1. Retrieve Azure AD's public keys from the OpenID Connect metadata endpoint
/// 2. Validate token signature using those keys
/// 3. Verify issuer, audience, and expiration
/// 4. Return ClaimsPrincipal with validated claims
/// </summary>
internal sealed class AzureAdTokenValidator : IAzureAdTokenValidator
{
    private readonly string _tenantId;
    private readonly string _apiClientId;
    private readonly string _spaClientId;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdTokenValidator"/> class.
    /// </summary>
    /// <param name="tenantId">Azure AD tenant ID (UUID).</param>
    /// <param name="apiClientId">API Application (Client) ID from Azure AD app registration.</param>
    /// <param name="spaClientId">SPA Application (Client) ID — audience of ID tokens issued to the frontend.</param>
    public AzureAdTokenValidator(string tenantId, string apiClientId, string spaClientId)
    {
        _tenantId = tenantId;
        _apiClientId = apiClientId;
        _spaClientId = spaClientId;
        // MapInboundClaims = false keeps original JWT claim names (e.g. "oid", "email",
        // "preferred_username") instead of mapping them to long .NET CLR URI strings.
        _tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        // Fetch signing keys from the v2.0 OpenID Connect metadata endpoint
        var metadataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
        var documentRetriever = new HttpDocumentRetriever { RequireHttps = true };
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            documentRetriever);
    }

    /// <inheritdoc />
    public async Task<ClaimsPrincipal> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Retrieve Azure AD configuration (includes public keys)
            var config = await _configurationManager.GetConfigurationAsync(cancellationToken);

            // Configure token validation parameters.
            // ValidIssuers covers both v2.0 tokens (login.microsoftonline.com) and
            // v1.0 legacy tokens (sts.windows.net) — mirrors the guide's recommendation.
            // ValidAudiences accepts both custom API scope and Microsoft Graph scope:
            // - api://{clientId} for custom API access tokens
            // - https://graph.microsoft.com for Microsoft Graph access tokens
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuers = new[]
                {
                    $"https://login.microsoftonline.com/{_tenantId}/v2.0",
                    $"https://sts.windows.net/{_tenantId}/"
                },
                ValidAudiences = new[]
                {
                    _spaClientId,              // ID token audience (frontend SPA client ID)
                    $"api://{_apiClientId}",   // Access token audience (future, when admin consent available)
                },
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Validate and return ClaimsPrincipal
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            throw new AzureAdTokenValidationException("Token has expired.", ex);
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            throw new AzureAdTokenValidationException("Token issuer is invalid.", ex);
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            throw new AzureAdTokenValidationException("Token audience is invalid.", ex);
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            throw new AzureAdTokenValidationException("Token signature is invalid.", ex);
        }
        catch (SecurityTokenException ex)
        {
            throw new AzureAdTokenValidationException($"Token validation failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new AzureAdTokenValidationException($"Unexpected error during token validation: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public string? GetClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }
}
