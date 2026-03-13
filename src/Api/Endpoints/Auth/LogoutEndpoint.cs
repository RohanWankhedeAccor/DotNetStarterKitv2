namespace Api.Endpoints.Auth;

/// <summary>
/// Endpoint that clears the HttpOnly auth cookie, effectively logging the user out.
/// Maps to: POST /api/v1/auth/logout
/// </summary>
public static class LogoutEndpoint
{
    /// <summary>Maps the logout endpoint.</summary>
    public static void MapLogout(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/logout", Logout)
            .WithName("Logout")
            .WithOpenApi()
            .WithSummary("Log out and clear session cookie")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static IResult Logout(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(LoginEndpoint.CookieName);
        return Results.NoContent();
    }
}
