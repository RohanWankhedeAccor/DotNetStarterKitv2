using Application.Interfaces;
using Infrastructure.Identity;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.DependencyInjection;

/// <summary>
/// Provides the <see cref="AddInfrastructure"/> extension method that registers all
/// Infrastructure layer services with the ASP.NET Core dependency injection container.
///
/// Called once from <c>Program.cs</c> in the Api project:
/// <code>
/// builder.Services.AddInfrastructure(builder.Configuration);
/// </code>
///
/// This extension is the only public surface area of the Infrastructure project
/// from the perspective of the Api project. All other Infrastructure types are
/// <c>internal sealed</c> — visible only within the Infrastructure assembly.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure layer services: strongly-typed options, EF Core DbContext,
    /// and identity/utility services (current user, datetime, password hasher, token service).
    /// </summary>
    /// <param name="services">The ASP.NET Core service collection to register into.</param>
    /// <param name="configuration">
    /// The application configuration (appsettings.json + environment overrides).
    /// </param>
    /// <returns>The <paramref name="services"/> collection for fluent chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Strongly-typed options ────────────────────────────────────────────────
        // ValidateDataAnnotations() enforces [Required], [MinLength], [Range] etc.
        // ValidateOnStart() makes the app fail immediately at startup if config is
        // missing or invalid — far preferable to a cryptic runtime error mid-request.

        services.AddOptions<JwtOptions>()
            .BindConfiguration("Jwt")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .BindConfiguration("Database")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // AzureAd options are optional — no ValidateOnStart so the app starts without
        // Azure AD configured (local JWT auth only).
        services.AddOptions<AzureAdOptions>()
            .BindConfiguration("AzureAd");

        // ── Database ──────────────────────────────────────────────────────────────
        // Connection string uses the standard ASP.NET Core named-connection-string
        // pattern (not the Options pattern) to stay compatible with EF tooling.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration. " +
                "Add it to appsettings.Development.json (dev) or Azure Key Vault (staging/prod).");

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: dbOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(dbOptions.MaxRetryDelaySeconds),
                    errorNumbersToAdd: null));
        });

        // Register the interface alias so Application layer handlers receive
        // IApplicationDbContext from DI — never the concrete ApplicationDbContext.
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Unit of Work + generic Repository — the preferred abstraction for handlers.
        // EfUnitOfWork wraps the same scoped ApplicationDbContext, so all repositories
        // within a single request share one ChangeTracker and one SaveChangesAsync call.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // ── Identity / utility services ───────────────────────────────────────────

        // Scoped — one instance per HTTP request.
        // IHttpContextAccessor is registered separately in Program.cs (Api layer)
        // to keep Infrastructure decoupled from HTTP host configuration.
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // JWT token service resolves IOptions<JwtOptions> via DI — no raw config reads.
        services.AddSingleton<ITokenService, JwtTokenService>();

        // In-memory cache: Singleton so the cache is shared across all requests.
        // AddMemoryCache is idempotent — safe to call multiple times (e.g., in tests).
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, InMemoryCacheService>();

        // ── Azure AD token validator (conditional) ────────────────────────────────
        // Only registered when TenantId, ClientId, and SpaClientId are all configured.
        // If any are absent, Azure AD login is disabled and only local JWT auth works.
        var azureAdOptions = configuration.GetSection("AzureAd").Get<AzureAdOptions>();
        if (azureAdOptions?.IsConfigured == true)
        {
            services.AddSingleton<IAzureAdTokenValidator, AzureAdTokenValidator>();
        }

        return services;
    }
}
