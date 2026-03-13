using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

/// <summary>
/// Seeds the database with demo data on first startup.
/// Only runs when the Users table is completely empty — safe to call on every startup.
/// Inserts 3 users so the UI has something to display out of the box.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Seeds users if the table is empty.
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(context, passwordHasher, logger, cancellationToken);
    }

    private static async Task SeedUsersAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Users.IgnoreQueryFilters().AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        var users = new[]
        {
            CreateUser("Alice Johnson",  "alice@example.com",  passwordHasher, now),
            CreateUser("Bob Martinez",   "bob@example.com",    passwordHasher, now),
            CreateUser("Carol Williams", "carol@example.com",  passwordHasher, now),
        };

        await context.Users.AddRangeAsync(users, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[DataSeeder] Seeded {Count} users", users.Length);
    }

    private static User CreateUser(string fullName, string email, IPasswordHasher hasher, DateTimeOffset now)
    {
        var user = new User(email, fullName, hasher.HashPassword("Password123!"));
        user.Activate();
        user.CreatedAt = now;
        user.CreatedBy = "seed";
        user.ModifiedAt = now;
        user.ModifiedBy = "seed";
        return user;
    }
}
