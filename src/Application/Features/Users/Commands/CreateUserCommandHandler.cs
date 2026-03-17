using Application.Common.Results;
using Application.Features.Users.Dtos;
using Application.Features.Users.Queries;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Commands;

/// <summary>
/// Handler for <see cref="CreateUserCommand"/>. Creates a new user with:
/// - Email uniqueness check (returns <see cref="Error.Conflict"/> if email exists)
/// - Password hashing using bcrypt
/// - Active status by default (for demo; requires email confirmation in production)
/// - Audit fields auto-populated by DbContext.SaveChangesAsync override
/// - User-list cache invalidation so subsequent list queries see the new user
/// </summary>
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICacheService _cache;

    /// <summary>Initializes a new instance of the <see cref="CreateUserCommandHandler"/> class.</summary>
    public CreateUserCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check for email uniqueness (soft-delete aware: ignores deleted users).
        var emailExists = await _unitOfWork.Users.AsQueryable()
            .AsNoTracking()
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
            return Error.Conflict($"A user with email '{request.Email}' already exists.");

        // Create the new user with bcrypt-hashed password.
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(
            email: request.Email,
            firstName: request.FirstName,
            lastName: request.LastName,
            passwordHash: passwordHash);

        // Activate the user for demo purposes. In production, email confirmation would be required.
        user.Activate();

        _unitOfWork.Users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate all user-list cache entries so subsequent GET /users calls see the new user.
        _cache.RemoveByPrefix(GetUsersQueryHandler.CacheKeyPrefix);

        return _mapper.Map<UserDto>(user);
    }
}
