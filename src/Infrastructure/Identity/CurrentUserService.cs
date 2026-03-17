using System.Security.Claims;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity;

/// <summary>
/// Resolves the identity of the currently authenticated user from the ASP.NET Core
/// HTTP context. Reads standard claims from the JWT bearer token including roles
/// and fine-grained permission claims embedded by <see cref="JwtTokenService"/>.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="CurrentUserService"/>.
    /// </summary>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Guid UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier);

            return claim is not null && Guid.TryParse(claim.Value, out var id)
                ? id
                : Guid.Empty;
        }
    }

    /// <inheritdoc />
    public string UserIdString => UserId == Guid.Empty
        ? string.Empty
        : UserId.ToString();

    /// <inheritdoc />
    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?
            .User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
        ?? [];

    /// <inheritdoc />
    public IEnumerable<string> Permissions =>
        _httpContextAccessor.HttpContext?
            .User
            .FindAll("permission")
            .Select(c => c.Value)
        ?? [];
}
