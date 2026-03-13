using Application.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Integration;

/// <summary>
/// Replaces SQL Server with SQLite in-memory for integration tests.
/// Each factory instance gets an isolated database kept alive for its full lifetime.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Keep the connection open so the SQLite in-memory database is not destroyed
    // between schema creation and actual test execution.
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        var dbName = $"IntTest_{Guid.NewGuid():N}";
        _connection = new SqliteConnection($"Data Source={dbName};Mode=Memory;Cache=Shared");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override JWT config so tests generate matching tokens
        builder.UseSetting("Jwt:SecretKey", TestConstants.JwtSecret);
        builder.UseSetting("Jwt:Issuer", TestConstants.JwtIssuer);
        builder.UseSetting("Jwt:Audience", TestConstants.JwtAudience);
        builder.UseSetting("Jwt:ExpirationMinutes", "60");

        // Blank out AzureAd keys so the real validator is not registered.
        builder.UseSetting("AzureAd:TenantId", "");
        builder.UseSetting("AzureAd:ClientId", "");
        builder.UseSetting("AzureAd:SpaClientId", "");

        builder.ConfigureServices(services =>
        {
            // Remove ALL descriptors that AddDbContext<ApplicationDbContext> registered:
            // - DbContextOptions<ApplicationDbContext>
            // - ApplicationDbContext itself
            // - IDbContextOptionsConfiguration<ApplicationDbContext>  (EF Core 9 addition)
            // - IApplicationDbContext (our own alias)
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType == typeof(IApplicationDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>) &&
                     d.ServiceType.GetGenericArguments()[0] == typeof(ApplicationDbContext)))
                .ToList();

            foreach (var d in toRemove) services.Remove(d);

            // MediatR scans AzureLoginCommandHandler which requires IAzureAdTokenValidator.
            // Register a no-op mock so DI validation passes for tests that don't hit the Azure login endpoint.
            services.AddSingleton(Substitute.For<IAzureAdTokenValidator>());

            // Replace with SQLite using the persistent connection (keeps the DB alive)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));

            services.AddScoped<IApplicationDbContext>(p =>
                p.GetRequiredService<ApplicationDbContext>());

            // Create schema using the same persistent connection
            using var tempProvider = services.BuildServiceProvider();
            using var scope = tempProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}

public static class TestConstants
{
    public const string JwtSecret = "integration-test-secret-key-must-be-32-chars!!";
    public const string JwtIssuer = "DotNetStarterKitv2";
    public const string JwtAudience = "DotNetStarterKitv2-App";
}
