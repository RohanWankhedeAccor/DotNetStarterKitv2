using Infrastructure.Identity;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Integration.Helpers;

/// <summary>Generates real JWTs using the same config as the test factory.</summary>
public static class TestJwtTokenHelper
{
    private static readonly JwtTokenService _service = new(Options.Create(new JwtOptions
    {
        SecretKey = TestConstants.JwtSecret,
        Issuer = TestConstants.JwtIssuer,
        Audience = TestConstants.JwtAudience,
        ExpirationMinutes = 60
    }));

    public static string GenerateToken(
        string userId = "00000000-0000-0000-0000-000000000001",
        string email = "testuser@example.com",
        string firstName = "Test",
        string lastName = "User",
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null) =>
        _service.GenerateToken(userId, email, firstName, lastName, roles ?? [], permissions ?? []);
}
