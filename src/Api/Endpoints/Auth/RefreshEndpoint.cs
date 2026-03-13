using System.Security.Claims;
using Application.Interfaces;

namespace Api.Endpoints.Auth;

/// <summary>
/// Re-issues the internal JWT and rotates the HttpOnly cookie.
/// Requires a valid existing cookie (or Bearer header) — no credentials needed.
/// Maps to: POST /api/v1/auth/refresh
/// </summary>
public static class RefreshEndpoint
{
    /// <summary>Maps the refresh endpoint.</summary>
    public static void MapRefresh(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/refresh", Refresh)
            .WithName("RefreshToken")
            .WithOpenApi()
            .WithSummary("Rotate the auth cookie with a fresh token")
            .Produces<RefreshResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }

    private static IResult Refresh(HttpContext httpContext, ITokenService tokenService)
    {
        var user = httpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var fullName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);

        var newToken = tokenService.GenerateToken(userId, email, fullName, roles);
        var expiresIn = tokenService.ExpirationMinutes * 60;

        LoginEndpoint.SetAuthCookie(httpContext, newToken, expiresIn);

        return Results.Ok(new RefreshResponse
        {
            ExpiresIn = expiresIn,
            TokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToUnixTimeMilliseconds(),
        });
    }
}

/// <summary>Response DTO for the refresh endpoint.</summary>
public class RefreshResponse
{
    /// <summary>Gets or sets token validity in seconds.</summary>
    public int ExpiresIn { get; set; }

    /// <summary>Gets or sets Unix ms timestamp when the new token expires.</summary>
    public long TokenExpiresAt { get; set; }
}
