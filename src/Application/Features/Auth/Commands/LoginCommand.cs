using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Commands;

/// <summary>
/// Command to authenticate a user and return a JWT bearer token.
/// Uses the user's email and password to verify identity.
/// </summary>
public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
