using Application.Features.Auth.Commands;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Api.Endpoints.Auth;

/// <summary>
/// Azure AD token exchange endpoint.
/// Receives an Azure AD token from MSAL.js frontend and exchanges it for an internal JWT token.
/// Part of Phase 12: Azure AD Integration.
/// </summary>
internal static class AzureLoginEndpoint
{
    /// <summary>
    /// Maps the Azure login endpoint to the application routes.
    /// </summary>
    public static void MapAzureLogin(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/azure-login", AzureLogin)
            .WithName("AzureLogin")
            .WithOpenApi()
            .WithSummary("Authenticate via Azure AD token exchange")
            .WithDescription("Exchanges an Azure AD token (from MSAL.js) for an internal JWT token. " +
                           "The Azure AD token should be obtained via MSAL.js in the frontend.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();
    }

    /// <summary>
    /// Handles the Azure login request: validates the Azure AD token and returns an internal JWT.
    /// </summary>
    private static async Task<IResult> AzureLogin(
        AzureLoginRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AzureAdToken))
        {
            return Results.BadRequest(new { error = "Azure AD token is required." });
        }

        var command = new AzureLoginCommand(request.AzureAdToken);
        var response = await mediator.Send(command, cancellationToken);
        LoginEndpoint.SetAuthCookie(httpContext, response.Token, response.ExpiresIn);
        return Results.Ok(LoginEndpoint.ToUserInfo(response));
    }
}

/// <summary>
/// Request DTO for Azure login endpoint.
/// Contains the Azure AD token obtained from MSAL.js.
/// </summary>
internal class AzureLoginRequest
{
    /// <summary>Gets or sets the Azure AD JWT token from MSAL.js.</summary>
    public required string AzureAdToken { get; set; }
}
