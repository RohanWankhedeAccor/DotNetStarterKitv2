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
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of <see cref="LoginCommandHandler"/>.
    /// </summary>
    public LoginCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Executes the login command: verifies email/password and returns a JWT token.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when user with given email is not found (HTTP 404)</exception>
    /// <exception cref="UnauthorizedException">Thrown when password is invalid or user is not active (HTTP 401)</exception>
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Email);

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash ?? string.Empty))
            throw new UnauthorizedException("Invalid email or password.");

        // Ensure user is active
        if (user.Status != UserStatus.Active)
            throw new UnauthorizedException($"User account is {user.Status.ToString().ToLowerInvariant()}. Please contact support.");

        // Extract roles (filter out null roles to satisfy nullable reference checks)
        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        // Generate JWT token
        var token = _tokenService.GenerateToken(user.Id.ToString(), user.Email, user.FullName, roles);

        return new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            ExpiresIn = 3600 // 60 minutes
        };
    }
}
