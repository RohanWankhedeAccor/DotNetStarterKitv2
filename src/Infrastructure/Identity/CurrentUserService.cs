using System.Security.Claims;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity;

/// <summary>
/// Resolves the identity of the currently authenticated user from the ASP.NET Core
/// HTTP context. This is the Phase 1 implementation that reads the NameIdentifier claim
/// from the request principal and falls back to <see cref="Guid.Empty"/> for anonymous
/// or unauthenticated requests.
///
/// Phase 2 migration path: when MSAL / Entra ID is integrated, replace the fallback
/// in <see cref="UserId"/> with a throw or specific anonymous-user sentinel value
/// as defined in AUTH_PLAN.md Phase 2 requirements. The interface contract does not change.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="CurrentUserService"/>.
    /// </summary>
    /// <param name="httpContextAccessor">
    /// ASP.NET Core accessor for the ambient HTTP context. Injected by the DI container;
    /// never null at runtime because <c>AddHttpContextAccessor()</c> is called in Program.cs.
    /// </param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads the <see cref="ClaimTypes.NameIdentifier"/> claim from the current
    /// request's <see cref="ClaimsPrincipal"/>. Returns <see cref="Guid.Empty"/> when:
    /// - the request is unauthenticated (no principal),
    /// - the NameIdentifier claim is absent,
    /// - or the claim value is not a valid GUID (Phase 1 mock scenario).
    /// </remarks>
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
}
