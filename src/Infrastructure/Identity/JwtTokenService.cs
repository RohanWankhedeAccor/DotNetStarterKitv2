using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

/// <summary>
/// Service for generating and validating JWT bearer tokens.
/// Tokens include user ID, email, and roles as claims.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    // Thread-safe — reuse across calls to avoid repeated reflection/regex init cost.
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <summary>
    /// Initializes a new instance of <see cref="JwtTokenService"/>.
    /// </summary>
    /// <param name="secretKey">JWT signing key (must be at least 32 chars for HS256)</param>
    /// <param name="issuer">Token issuer (e.g., "DotNetStarterKitv2")</param>
    /// <param name="audience">Token audience (e.g., "DotNetStarterKitv2-App")</param>
    /// <param name="expirationMinutes">Token validity duration in minutes (default: 60)</param>
    public JwtTokenService(string secretKey, string issuer = "DotNetStarterKitv2",
        string audience = "DotNetStarterKitv2-App", int expirationMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("Secret key cannot be null or empty.", nameof(secretKey));
        if (secretKey.Length < 32)
            throw new ArgumentException("Secret key must be at least 32 characters long for HS256.", nameof(secretKey));

        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _expirationMinutes = expirationMinutes;
    }

    /// <inheritdoc />
    public int ExpirationMinutes => _expirationMinutes;

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier (Guid converted to string)</param>
    /// <param name="email">The user's email address</param>
    /// <param name="fullName">The user's full name</param>
    /// <param name="roles">Collection of role names assigned to the user</param>
    /// <returns>A signed JWT token as a string</returns>
    public string GenerateToken(string userId, string email, string fullName, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, fullName),
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT token and extracts claims if valid.
    /// </summary>
    /// <param name="token">The JWT token string to validate</param>
    /// <returns>The claims principal if valid; null if invalid or expired</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var principal = _tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
