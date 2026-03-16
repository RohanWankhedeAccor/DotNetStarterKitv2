using Application.Common;
using Application.Common.Results;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Commands;

/// <summary>
/// Command to authenticate a user and return a JWT bearer token.
/// Returns <see cref="Result{T}"/> — callers must inspect <c>IsSuccess</c> rather than catching exceptions.
/// </summary>
public record LoginCommand(string Email, [property: Sensitive] string Password) : IRequest<Result<LoginResponse>>;
