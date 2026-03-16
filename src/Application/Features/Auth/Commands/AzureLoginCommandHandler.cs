using System.Security.Claims;
using Application.Features.Auth.Dtos;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Commands;

/// <summary>
/// Handles Azure AD token exchange: receives an Azure AD token from MSAL.js,
/// validates it, synchronizes the user in the database, and issues an internal JWT token.
/// Part of Phase 12: Azure AD Integration.
/// </summary>
internal sealed class AzureLoginCommandHandler : IRequestHandler<AzureLoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzureAdTokenValidator _azureAdTokenValidator;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureLoginCommandHandler"/> class.
    /// </summary>
    public AzureLoginCommandHandler(
        IUnitOfWork unitOfWork,
        IAzureAdTokenValidator azureAdTokenValidator,
        ITokenService tokenService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _azureAdTokenValidator = azureAdTokenValidator;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<LoginResponse> Handle(AzureLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Azure AD token
        ClaimsPrincipal principal;
        try
        {
            principal = await _azureAdTokenValidator.ValidateTokenAsync(request.AzureAdToken, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new AzureAdTokenValidationException(
                $"Failed to validate Azure AD token: {ex.Message}", ex);
        }

        // 2. Extract claims from Azure AD token
        var azureAdObjectId = _azureAdTokenValidator.GetClaimValue(principal, "oid")
            ?? throw new AzureAdTokenValidationException("Azure AD token does not contain 'oid' claim.");

        // Use raw JWT claim names — MapInboundClaims = false keeps them in short form.
        var email = _azureAdTokenValidator.GetClaimValue(principal, "email")
            ?? _azureAdTokenValidator.GetClaimValue(principal, "preferred_username")
            ?? _azureAdTokenValidator.GetClaimValue(principal, "upn")
            ?? throw new AzureAdTokenValidationException("Azure AD token does not contain email, preferred_username, or upn claim.");

        var displayName = _azureAdTokenValidator.GetClaimValue(principal, "name")
            ?? email;

        // Split displayName (e.g. "John Smith") into firstName / lastName.
        var nameParts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var displayFirstName = nameParts.Length > 0 ? nameParts[0] : displayName;
        var displayLastName  = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        // 3. Find or create user in database.
        // Look up by AzureAdObjectId first; fall back to email so that an existing
        // local account with the same address is adopted rather than duplicated.
        var user = await _unitOfWork.Users.AsQueryable()
                       .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                           .ThenInclude(r => r!.RolePermissions).ThenInclude(rp => rp.Permission)
                       .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId, cancellationToken)
                   ?? await _unitOfWork.Users.AsQueryable()
                       .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                           .ThenInclude(r => r!.RolePermissions).ThenInclude(rp => rp.Permission)
                       .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            user = new User(email, displayFirstName, displayLastName, passwordHash: null);
            user.ProvisionAzureAd(azureAdObjectId);
            user.Activate();

            _unitOfWork.Users.Add(user);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Concurrent request already inserted this user — re-query by email.
                user = await _unitOfWork.Users.AsQueryable()
                           .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                           .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
                       ?? throw new AzureAdTokenValidationException(
                           "Failed to provision user: concurrent insert conflict.");
            }
        }
        else
        {
            // User exists — sync Azure AD identity and profile.
            bool hasChanges = false;

            if (user.FirstName != displayFirstName || user.LastName != displayLastName)
            {
                user.UpdateName(displayFirstName, displayLastName);
                hasChanges = true;
            }

            if (user.AuthSource != "AzureAd")
            {
                user.ProvisionAzureAd(azureAdObjectId);
                hasChanges = true;
            }

            if (user.Status != UserStatus.Active)
            {
                user.Activate();
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        // 4. Verify user is in Active status
        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedException(
                $"User account is {user.Status}. Please contact support.");
        }

        // 5. Extract roles and permissions from the loaded navigation tree
        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Name)
            .Distinct()
            .ToList();

        // 6. Generate internal JWT token with role + permission claims
        var token = _tokenService.GenerateToken(
            user.Id.ToString(),
            user.Email,
            user.FirstName,
            user.LastName,
            roles,
            permissions);

        // 7. Return login response
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
