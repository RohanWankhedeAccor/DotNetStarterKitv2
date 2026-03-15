using Api.Extensions;
using Application;
using Application.Interfaces;
using Infrastructure.DependencyInjection;
using Infrastructure.Persistence;
using Serilog;

// ═══════════════════════════════════════════════════════════════════════════════
// BOOTSTRAP LOGGER — captures fatal startup errors before the host is built.
// Uses CreateLogger() (not CreateBootstrapLogger()) so Log.Logger is a plain
// Logger, not a ReloadableLogger. This prevents AddSerilog from entering the
// freeze-and-upgrade path, which breaks WebApplicationFactory in integration tests.
// ═══════════════════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog as the DI-integrated logger.
    // Uses AddSerilog (not UseSerilog) to avoid ReloadableLogger.Freeze() conflicts
    // that occur when WebApplicationFactory's internal BuildServiceProvider() resolves
    // ILogger during integration tests — causing a double-freeze on the same instance.
    builder.Services.AddSerilog((services, config) =>
        config.ReadFrom.Configuration(builder.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Enrich.WithThreadId());

    // ═══════════════════════════════════════════════════════════════════════════════
    // 1. REGISTER SERVICES (Dependency Injection)
    // ═══════════════════════════════════════════════════════════════════════════════

    // Register Application layer services (MediatR handlers, validators, behaviors, AutoMapper)
    builder.Services.AddApplication();

    // Register Infrastructure layer services (DbContext, services, repository implementations)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Make the HTTP context accessible to scoped services like CurrentUserService
    builder.Services.AddHttpContextAccessor();

    // Register API-specific services (Swagger, CORS, JSON serialization, JWT authentication)
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    // ═══════════════════════════════════════════════════════════════════════════════
    // 2. SEED DATABASE (runs only when tables are empty)
    // ═══════════════════════════════════════════════════════════════════════════════

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DataSeeder.SeedAsync(db, hasher, logger);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // 3. CONFIGURE MIDDLEWARE PIPELINE
    // ═══════════════════════════════════════════════════════════════════════════════

    // Configure middleware in the correct order
    app.UseApiMiddleware();

    // ═══════════════════════════════════════════════════════════════════════════════
    // 4. MAP API ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════════

    app.MapApiEndpoints();

    // ═══════════════════════════════════════════════════════════════════════════════
    // 5. RUN THE APPLICATION
    // ═══════════════════════════════════════════════════════════════════════════════

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    // Flush all remaining log events before the process exits
    await Log.CloseAndFlushAsync();
}

// Required for WebApplicationFactory<Program> in integration tests
#pragma warning disable CS1591
public partial class Program { }
#pragma warning restore CS1591
