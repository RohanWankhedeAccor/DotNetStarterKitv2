using Application.Features.Users.Dtos;
using Application.Features.Users.Queries;
using MediatR;

namespace Api.Endpoints.Users;

/// <summary>
/// Endpoint for retrieving a single user by ID via HTTP GET.
/// Maps to: GET /api/v1/users/{id}
/// </summary>
public static class GetUserByIdEndpoint
{
    /// <summary>Registers the GET /api/v1/users/{id} endpoint on the application.</summary>
    /// <param name="app">The web application to register the endpoint on.</param>
    public static void MapGetUserById(this WebApplication app)
    {
        app.MapGet("/api/v1/users/{id:guid}", GetUserById)
            .WithName("GetUserById")
            .WithOpenApi()
            .WithSummary("Get a user by ID")
            .WithDescription("Retrieves a single user by their unique identifier.")
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetUserById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery { Id = id };
        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(result);
    }
}
