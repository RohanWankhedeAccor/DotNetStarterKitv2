using Api.Extensions;
using Application.Features.Auth.Commands;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Api.Endpoints.Auth;

/// <summary>
/// Endpoint for user authentication via email and password.
/// Sets an HttpOnly cookie containing the JWT on successful authentication.
/// Maps to: POST /api/v1/auth/login
/// </summary>
public static class LoginEndpoint
{
    internal const string CookieName = "auth_token";

    /// <summary>
    /// Maps the login endpoint.
    /// </summary>
    public static void MapLogin(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/login", Login)
            .WithName("Login")
            .WithOpenApi()
            .WithSummary("Authenticate user and get session cookie")
            .WithDescription("Authenticates a user using email and password credentials. Sets an HttpOnly cookie containing the JWT (valid 60 minutes).")
            .Accepts<LoginRequest>("application/json")
            .Produces<UserInfoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await mediator.Send(command, cancellationToken);

        return result.ToApiResult(response =>
        {
            SetAuthCookie(httpContext, response.Token, response.ExpiresIn);
            return Results.Ok(ToUserInfo(response));
        });
    }

    internal static void SetAuthCookie(HttpContext httpContext, string token, int expiresInSeconds)
    {
        httpContext.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !httpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds),
        });
    }

    internal static UserInfoResponse ToUserInfo(LoginResponse r) => new()
    {
        UserId = r.UserId,
        Email = r.Email,
        FirstName = r.FirstName,
        LastName = r.LastName,
        Roles = r.Roles,
        ExpiresIn = r.ExpiresIn,
    };
}

/// <summary>Request DTO for login endpoint.</summary>
internal record LoginRequest(string Email, string Password);

/// <summary>
/// User information returned after successful authentication.
/// The JWT itself is set as an HttpOnly cookie and not included in this payload.
/// </summary>
public class UserInfoResponse
{
    /// <summary>Gets or sets the user's unique identifier.</summary>
    public required Guid UserId { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    public required string Email { get; set; }

    /// <summary>Gets or sets the user's first name.</summary>
    public required string FirstName { get; set; }

    /// <summary>Gets or sets the user's last name.</summary>
    public required string LastName { get; set; }

    /// <summary>Gets or sets the roles assigned to the user.</summary>
    public required IEnumerable<string> Roles { get; set; }

    /// <summary>Gets or sets the token expiration time in seconds from issue time.</summary>
    public int ExpiresIn { get; set; }
}
