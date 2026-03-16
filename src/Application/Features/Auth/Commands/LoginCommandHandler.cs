using Application.Features.Auth.Commands;
using Application.Features.Auth.Dtos;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Commands;

/// <summary>
/// Handles login requests by verifying credentials and issuing JWT tokens.
/// Loads the user's roles and all permissions derived from those roles so that
/// both role and permission claims are embedded in the token.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of <see cref="LoginCommandHandler"/>.
    /// </summary>
    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Executes the login command: verifies email/password and returns a JWT token
    /// containing both role claims and fine-grained permission claims.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when user with given email is not found (HTTP 404)</exception>
    /// <exception cref="UnauthorizedException">Thrown when password is invalid or user is not active (HTTP 401)</exception>
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Load user with roles and their permissions in a single round-trip.
        var user = await _unitOfWork.Users.AsQueryable()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Email);

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash ?? string.Empty))
            throw new UnauthorizedException("Invalid email or password.");

        if (user.Status != UserStatus.Active)
            throw new UnauthorizedException($"User account is {user.Status.ToString().ToLowerInvariant()}. Please contact support.");

        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        // Collect all unique permissions from all assigned roles.
        var permissions = user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Name)
            .Distinct()
            .ToList();

        var token = _tokenService.GenerateToken(user.Id.ToString(), user.Email, user.FirstName, user.LastName, roles, permissions);

        return new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
            ExpiresIn = _tokenService.ExpirationMinutes * 60
        };
    }
}
