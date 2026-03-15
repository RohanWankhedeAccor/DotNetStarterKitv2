using Application.Features.Users.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Commands;

/// <summary>
/// Handler for <see cref="CreateUserCommand"/>. Creates a new user with:
/// - Email uniqueness check (throws <see cref="ConflictException"/> if email exists)
/// - Password hashing using bcrypt
/// - Active status by default (for demo; requires email confirmation in production)
/// - Audit fields auto-populated by DbContext.SaveChangesAsync override
/// </summary>
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    /// <param name="passwordHasher">Service for password hashing.</param>
    public CreateUserCommandHandler(IApplicationDbContext context, IMapper mapper, IPasswordHasher passwordHasher)
    {
        _context = context;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check for email uniqueness (soft-delete aware: ignores deleted users).
        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        // Create the new user with bcrypt-hashed password.
        // Status is automatically set to PendingActivation by the User constructor.
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(
            email: request.Email,
            firstName: request.FirstName,
            lastName: request.LastName,
            passwordHash: passwordHash);

        // Activate the user for demo purposes. In production, email confirmation would be required.
        // Phase 2: Require email confirmation before activating.
        user.Activate();

        // Add to DbSet and persist.
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Return the created user as a DTO.
        return _mapper.Map<UserDto>(user);
    }
}
