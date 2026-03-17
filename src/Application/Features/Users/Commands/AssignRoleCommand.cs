using Application.Common.Results;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to assign a named role to an existing user.
/// This is an idempotent operation: assigning a role the user already holds is a no-op.
/// Requires the caller to hold the <c>roles.assign</c> permission (enforced at the API layer).
/// Returns <see cref="Result"/> — callers must inspect <c>IsSuccess</c> rather than catching exceptions.
/// </summary>
public sealed class AssignRoleCommand : IRequest<Result>
{
    /// <summary>Gets the identifier of the user to assign the role to.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets the name of the role to assign (e.g. "Editor", "Viewer").</summary>
    public string RoleName { get; init; } = string.Empty;
}
