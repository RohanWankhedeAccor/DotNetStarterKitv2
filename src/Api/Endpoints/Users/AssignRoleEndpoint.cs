using Api.Extensions;
using Application.Features.Users.Commands;
using MediatR;

namespace Api.Endpoints.Users;

/// <summary>
/// Endpoint for assigning a role to an existing user.
/// Maps to: POST /api/v1/users/{userId}/roles
/// Requires the "CanAssignRoles" policy (permission: roles.assign).
/// </summary>
public static class AssignRoleEndpoint
{
    /// <summary>Registers the POST /api/v1/users/{userId}/roles endpoint on the application.</summary>
    public static void MapAssignRole(this WebApplication app)
    {
        app.MapPost("/api/v1/users/{userId:guid}/roles", AssignRole)
            .WithName("AssignRole")
            .WithOpenApi()
            .WithSummary("Assign a role to a user")
            .WithDescription("Assigns the named role to the specified user. Idempotent — assigning an existing role is a no-op.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("CanAssignRoles");
    }

    private static async Task<IResult> AssignRole(
        Guid userId,
        AssignRoleRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new AssignRoleCommand
        {
            UserId = userId,
            RoleName = request.RoleName
        };

        var result = await mediator.Send(command, cancellationToken);
        return result.ToApiResult();
    }
}

/// <summary>Request body for the AssignRole endpoint.</summary>
public sealed record AssignRoleRequest(string RoleName);
