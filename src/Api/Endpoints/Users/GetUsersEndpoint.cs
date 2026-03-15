using Application.Common.Models;
using Application.Features.Users.Dtos;
using Application.Features.Users.Queries;
using MediatR;

namespace Api.Endpoints.Users;

/// <summary>
/// Endpoint for retrieving a paginated list of users via HTTP GET.
/// Maps to: GET /api/v1/users?pageNumber=1&amp;pageSize=10
/// </summary>
public static class GetUsersEndpoint
{
    /// <summary>Registers the GET /api/v1/users endpoint on the application.</summary>
    /// <param name="app">The web application to register the endpoint on.</param>
    public static void MapGetUsers(this WebApplication app)
    {
        app.MapGet("/api/v1/users", GetUsers)
            .WithName("GetUsers")
            .WithOpenApi()
            .WithSummary("Get all users (paginated)")
            .WithDescription("Retrieves a paginated list of users. PageSize is clamped to a maximum of 100.")
            .Produces<PagedResponse<UserDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("CanViewUsers");
    }

    private static async Task<IResult> GetUsers(
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(result);
    }
}
