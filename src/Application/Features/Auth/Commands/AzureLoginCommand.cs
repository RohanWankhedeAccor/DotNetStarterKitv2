using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Commands;

/// <summary>
/// Command to authenticate a user via Azure AD token exchange.
/// The frontend (MSAL.js) obtains an Azure AD token, sends it to this endpoint,
/// and receives an internal JWT token in exchange.
/// Part of Phase 12: Azure AD Integration.
/// </summary>
public record AzureLoginCommand(string AzureAdToken) : IRequest<LoginResponse>;
