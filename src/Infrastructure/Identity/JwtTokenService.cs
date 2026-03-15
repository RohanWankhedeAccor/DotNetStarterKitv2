using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

/// <summary>
/// Service for generating and validating JWT bearer tokens.
/// Tokens include user ID, email, roles, and fine-grained permission claims.
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
    /// Generates a JWT token for the specified user, embedding role claims and
    /// fine-grained permission claims so ASP.NET Core policy checks can use them directly.
    /// </summary>
    /// <param name="userId">The user's unique identifier (Guid converted to string)</param>
    /// <param name="email">The user's email address</param>
    /// <param name="firstName">The user's first name</param>
    /// <param name="lastName">The user's last name</param>
    /// <param name="roles">Collection of role names assigned to the user</param>
    /// <param name="permissions">Collection of permission keys derived from the user's roles (e.g. "users.create")</param>
    /// <returns>A signed JWT token as a string</returns>
    public string GenerateToken(string userId, string email, string firstName, string lastName,
        IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, $"{firstName} {lastName}".Trim()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // Embed each permission as a "permission" claim so ASP.NET Core policies
        // can check them via RequireClaim("permission", "users.create") etc.
        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

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
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
