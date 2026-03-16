using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for JWT token generation and validation.
/// Bound from the <c>Jwt</c> section in appsettings.json.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>HMAC-SHA256 signing key — minimum 32 characters required for HS256.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:SecretKey is required. Add it to appsettings or Azure Key Vault.")]
    [MinLength(32, ErrorMessage = "Jwt:SecretKey must be at least 32 characters long for HS256.")]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>Token issuer claim (iss). Defaults to "DotNetStarterKitv2".</summary>
    public string Issuer { get; init; } = "DotNetStarterKitv2";

    /// <summary>Token audience claim (aud). Defaults to "DotNetStarterKitv2-App".</summary>
    public string Audience { get; init; } = "DotNetStarterKitv2-App";

    /// <summary>Token lifetime in minutes. Defaults to 60 (1 hour).</summary>
    [Range(1, 10080, ErrorMessage = "Jwt:ExpirationMinutes must be between 1 minute and 7 days (10080).")]
    public int ExpirationMinutes { get; init; } = 60;
}
