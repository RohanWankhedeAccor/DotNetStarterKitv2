using Application.Features.Auth.Commands;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Api.Endpoints.Auth;

/// <summary>
/// Endpoint for user authentication via email and password.
/// Issues a JWT bearer token upon successful authentication.
/// Maps to: POST /api/v1/auth/login
/// </summary>
public static class LoginEndpoint
{
    /// <summary>
    /// Maps the login endpoint.
    /// </summary>
    public static void MapLogin(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/login", Login)
            .WithName("Login")
            .WithOpenApi()
            .WithSummary("Authenticate user and get JWT token")
            .WithDescription("Authenticates a user using email and password credentials. Returns a JWT bearer token valid for 60 minutes.")
            .Accepts<LoginRequest>("application/json")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Handles the login request.
    /// </summary>
    private static async Task<IResult> Login(
        LoginRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }
}
