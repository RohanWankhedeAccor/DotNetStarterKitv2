using Application.Common.Models;
using Application.Features.Users.Dtos;
using Application.Features.Users.Queries;
using Domain.Enums;
using MediatR;

namespace Api.Endpoints.Users;

/// <summary>
/// Endpoint for retrieving a paginated, filtered, and sorted list of users via HTTP GET.
/// Maps to: GET /api/v1/users
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
            .WithSummary("Get users (paginated, filtered, sorted)")
            .WithDescription(
                "Retrieves a filtered and sorted page of users. " +
                "Filter by free-text search (email/name) or by status. " +
                "Sort by: email (default), firstName, lastName, status, createdAt. " +
                "PageSize is clamped to 1–100.")
            .Produces<PagedResponse<UserDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("CanViewUsers");
    }

    private static async Task<IResult> GetUsers(
        int pageNumber = 1,
        int pageSize = 10,
        string? search = null,
        UserStatus? status = null,
        string? sortBy = null,
        bool sortDescending = false,
        IMediator mediator = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            Status = status,
            SortBy = sortBy,
            SortDescending = sortDescending,
        };

        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }
}
