using Api.Extensions;
using Application.Features.Users.Commands;
using Application.Features.Users.Dtos;
using MediatR;

namespace Api.Endpoints.Users;

/// <summary>
/// Endpoint for creating a new user via HTTP POST.
/// Maps to: POST /api/v1/users
/// </summary>
public static class CreateUserEndpoint
{
    /// <summary>Registers the POST /api/v1/users endpoint on the application.</summary>
    /// <param name="app">The web application to register the endpoint on.</param>
    public static void MapCreateUser(this WebApplication app)
    {
        app.MapPost("/api/v1/users", CreateUser)
            .WithName("CreateUser")
            .WithOpenApi()
            .WithSummary("Create a new user")
            .WithDescription("Creates a new user account with the provided email, name, and password.")
            .Produces<UserDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("CanCreateUser");
    }

    private static async Task<IResult> CreateUser(
        CreateUserDto request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Map DTO to command and dispatch via MediatR
        var command = new CreateUserCommand
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Password = request.Password
        };

        var result = await mediator.Send(command, cancellationToken);

        return result.ToCreatedResult(u => $"/api/v1/users/{u.Id}");
    }
}
