using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Domain.Exceptions;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
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
///
/// Configuration is supplied via <see cref="AzureAdOptions"/> (bound from the "AzureAd" config section).
/// </summary>
internal sealed class AzureAdTokenValidator : IAzureAdTokenValidator
{
    private readonly IReadOnlyList<string> _validIssuers;
    private readonly IReadOnlyList<string> _validAudiences;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdTokenValidator"/> class.
    /// </summary>
    /// <param name="options">Azure AD configuration bound from the "AzureAd" appsettings section.</param>
    public AzureAdTokenValidator(IOptions<AzureAdOptions> options)
    {
        var azureAd = options.Value;

        _validIssuers = azureAd.GetValidIssuers();
        _validAudiences = azureAd.GetValidAudiences();

        // MapInboundClaims = false keeps original JWT claim names (e.g. "oid", "email",
        // "preferred_username") instead of mapping them to long .NET CLR URI strings.
        _tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        // Fetch signing keys from the v2.0 OpenID Connect metadata endpoint.
        // Instance already includes the trailing slash (e.g. "https://login.microsoftonline.com/").
        var metadataAddress = $"{azureAd.Instance.TrimEnd('/')}/{azureAd.TenantId}/v2.0/.well-known/openid-configuration";
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
            // Retrieve Azure AD configuration (includes public signing keys).
            var config = await _configurationManager.GetConfigurationAsync(cancellationToken);

            // Configure token validation parameters.
            // ValidIssuers covers both v2.0 (login.microsoftonline.com) and
            // v1.0 legacy tokens (sts.windows.net) — computed from AzureAdOptions.
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuers = _validIssuers,
                ValidAudiences = _validAudiences,
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
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
