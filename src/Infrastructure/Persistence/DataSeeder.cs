using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

/// <summary>
/// Seeds the database with demo data on first startup.
/// Only runs when the tables are completely empty — safe to call on every startup.
/// Inserts 3 users and 5 products so the UI has something to display out of the box.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Seeds users and products if their tables are empty.
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(context, passwordHasher, logger, cancellationToken);
        await SeedProductsAsync(context, logger, cancellationToken);
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

    private static async Task SeedProductsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Products.IgnoreQueryFilters().AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        var products = new[]
        {
            CreateProduct("Starter Plan",     "Basic access with core features for small teams.",            9.99m,   ProductStatus.Active, now),
            CreateProduct("Professional Plan", "Advanced features, priority support, and analytics.",         49.99m,  ProductStatus.Active, now),
            CreateProduct("Enterprise Plan",  "Unlimited users, SLA, custom integrations, and onboarding.", 199.99m, ProductStatus.Active, now),
            CreateProduct("Data Add-on",      "10 GB additional storage and extended data retention.",       14.99m,  ProductStatus.Active, now),
            CreateProduct("API Access",       "Full REST API access with higher rate limits.",               29.99m,  ProductStatus.Draft,  now),
        };

        await context.Products.AddRangeAsync(products, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[DataSeeder] Seeded {Count} products", products.Length);
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

    private static Product CreateProduct(string name, string description, decimal price, ProductStatus status, DateTimeOffset now)
    {
        var product = new Product(name, description, price);
        if (status == ProductStatus.Active) product.Activate();
        product.CreatedAt = now;
        product.CreatedBy = "seed";
        product.ModifiedAt = now;
        product.ModifiedBy = "seed";
        return product;
    }
}
