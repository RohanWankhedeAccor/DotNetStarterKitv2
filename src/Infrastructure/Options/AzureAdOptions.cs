namespace Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for Azure AD token validation.
/// Bound from the <c>AzureAd</c> section in appsettings.json.
/// Follows Microsoft.Identity.Web naming conventions so the section is compatible
/// with AddMicrosoftIdentityWebApi if the project migrates to that library later.
/// </summary>
public sealed class AzureAdOptions
{
    /// <summary>Azure AD tenant ID (GUID).</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>API Application (Client) ID from the Azure AD app registration.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>SPA Application (Client) ID — audience of ID tokens issued to the frontend.</summary>
    public string SpaClientId { get; init; } = string.Empty;

    /// <summary>
    /// Azure AD authority base URL.
    /// Defaults to the global cloud endpoint "https://login.microsoftonline.com/".
    /// Override for sovereign clouds (e.g. US Gov: "https://login.microsoftonline.us/").
    /// </summary>
    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// API scope URI (e.g. <c>api://{ClientId}/access_as_user</c>).
    /// Used by the SPA to request access tokens for this API.
    /// </summary>
    public string ApiScope { get; init; } = string.Empty;

    /// <summary>
    /// Returns <see langword="true"/> when all required Azure AD values are configured.
    /// Used to conditionally register the validator in DI — if any value is missing,
    /// Azure AD login is disabled and only local JWT auth is available.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrEmpty(TenantId) &&
        !string.IsNullOrEmpty(ClientId) &&
        !string.IsNullOrEmpty(SpaClientId);

    /// <summary>
    /// Returns the valid token audiences for this API:
    /// the SPA client ID (for ID tokens) and the <c>api://</c> URI (for access tokens).
    /// </summary>
    public IReadOnlyList<string> GetValidAudiences() =>
    [
        SpaClientId,
        $"api://{ClientId}"
    ];

    /// <summary>
    /// Returns the valid token issuers derived from <see cref="TenantId"/> and <see cref="Instance"/>.
    /// Covers both v2.0 (<c>login.microsoftonline.com</c>) and legacy v1.0 (<c>sts.windows.net</c>) issuers.
    /// </summary>
    public IReadOnlyList<string> GetValidIssuers()
    {
        var authority = Instance.TrimEnd('/');
        return
        [
            $"{authority}/{TenantId}/v2.0",
            $"https://sts.windows.net/{TenantId}/"
        ];
    }
}
