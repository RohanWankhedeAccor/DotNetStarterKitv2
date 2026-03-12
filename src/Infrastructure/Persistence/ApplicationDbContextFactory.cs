using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances.
/// Used by EF Core tooling (dotnet ef commands) when generating migrations.
/// This factory is not used at runtime; the DI container handles context creation then.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Creates a new ApplicationDbContext instance using configuration from appsettings.json.
    /// </summary>
    /// <param name="args">Command-line arguments (not used in this implementation).</param>
    /// <returns>A configured ApplicationDbContext instance.</returns>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Api"))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .Build();

        // Read connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Configure DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        // Create and return context instance (with dummy services for design-time)
        return new ApplicationDbContext(
            optionsBuilder.Options,
            new DesignTimeCurrentUserService(),
            new DesignTimeDateTimeService());
    }
}

/// <summary>
/// Dummy implementation of ICurrentUserService for design-time (migration) usage.
/// </summary>
internal sealed class DesignTimeCurrentUserService : Application.Interfaces.ICurrentUserService
{
    public Guid UserId => Guid.Empty;
    public string UserIdString => string.Empty;
}

/// <summary>
/// Dummy implementation of IDateTimeService for design-time (migration) usage.
/// </summary>
internal sealed class DesignTimeDateTimeService : Application.Interfaces.IDateTimeService
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
