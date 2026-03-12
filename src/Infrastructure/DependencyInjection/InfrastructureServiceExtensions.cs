using Application.Interfaces;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    /// Registers all Infrastructure layer services: EF Core DbContext, HTTP context
    /// accessor, and the current-user and datetime services.
    /// </summary>
    /// <param name="services">The ASP.NET Core service collection to register into.</param>
    /// <param name="configuration">
    /// The application configuration (appsettings.json + environment overrides).
    /// Used to read the <c>ConnectionStrings:DefaultConnection</c> value.
    /// </param>
    /// <returns>The <paramref name="services"/> collection for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown at startup if <c>ConnectionStrings:DefaultConnection</c> is absent or null
    /// in the configuration. Fail-fast at startup is preferable to a cryptic runtime error
    /// on the first database query.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read the connection string — fail fast at startup if it is not configured.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration. " +
                "Add it to appsettings.Development.json (dev) or Azure Key Vault (staging/prod).");

        // Register ApplicationDbContext with SQL Server provider.
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    // Enable automatic retry on transient SQL failures
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                }));

        // Register the interface alias so Application layer handlers receive
        // IApplicationDbContext from DI — never the concrete ApplicationDbContext.
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Register the current-user service as scoped — one instance per HTTP request
        // Note: IHttpContextAccessor must be registered separately in Program.cs (Api layer)
        // to keep Infrastructure decoupled from HTTP concerns
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Register the datetime service as singleton
        services.AddSingleton<IDateTimeService, DateTimeService>();

        // Register password hasher as scoped (allows stateful implementations if needed)
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Register JWT token service as singleton
        // Uses configuration for secret key and token settings
        var jwtSecret = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException(
                "JWT configuration 'Jwt:SecretKey' not found in configuration. " +
                "Add it to appsettings.Development.json (dev) or Azure Key Vault (staging/prod).");

        var jwtIssuer = configuration["Jwt:Issuer"] ?? "DotNetStarterKitv2";
        var jwtAudience = configuration["Jwt:Audience"] ?? "DotNetStarterKitv2-App";
        var jwtExpirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");

        services.AddSingleton<ITokenService>(new JwtTokenService(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

        // Register Azure AD token validator.
        // Config keys follow the Microsoft.Identity.Web standard naming convention
        // (AzureAd:TenantId, AzureAd:ClientId) so the same appsettings block can be
        // reused if the project later migrates to AddMicrosoftIdentityWebApi.
        var azureAdTenantId = configuration["AzureAd:TenantId"];
        var azureAdApiClientId = configuration["AzureAd:ClientId"];
        var azureAdSpaClientId = configuration["AzureAd:SpaClientId"];

        if (!string.IsNullOrEmpty(azureAdTenantId) && !string.IsNullOrEmpty(azureAdApiClientId)
            && !string.IsNullOrEmpty(azureAdSpaClientId))
        {
            services.AddSingleton<IAzureAdTokenValidator>(
                new AzureAdTokenValidator(azureAdTenantId, azureAdApiClientId, azureAdSpaClientId));
        }

        return services;
    }
}
