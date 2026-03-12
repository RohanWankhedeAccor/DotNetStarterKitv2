using Api.Endpoints.Auth;
using Api.Endpoints.Products;
using Api.Endpoints.Users;
using Api.Middleware;

namespace Api.Extensions;

/// <summary>
/// Application builder extensions for configuring the HTTP request pipeline (middleware)
/// and mapping API endpoints. Called from Program.cs after building the app.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline with middleware in the correct order
    /// and maps all API endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for fluent chaining.</returns>
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        // Middleware pipeline order is critical:
        // 1. ExceptionHandler must be first to catch all exceptions from downstream middleware
        // 2. HTTPS redirection and security headers
        // 3. CORS before authentication
        // 4. Routing and endpoint mapping

        // Global exception handler (must be first)
        app.UseMiddleware<ExceptionHandlerMiddleware>();

        // HTTPS redirection (enforce HTTPS in production)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // Add security headers to all responses
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Enable CORS (before authentication/authorization)
        app.UseCors("AllowLocalhost");

        // Enable JWT bearer authentication
        app.UseAuthentication();
        app.UseAuthorization();

        // Enable Swagger/OpenAPI in Development and Staging environments only
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "DotNetStarterKitv2 API v1");
                options.RoutePrefix = "swagger"; // Access at /swagger instead of /swagger/index.html
                options.DefaultModelsExpandDepth(2);
                options.DefaultModelExpandDepth(2);
            });
        }

        return app;
    }

    /// <summary>
    /// Maps all API endpoints grouped by feature.
    /// Call this after UseApiMiddleware but before app.Run().
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for fluent chaining.</returns>
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        // Auth endpoints
        app.MapLogin();
        app.MapAzureLogin(); // Phase 12: Azure AD token exchange

        // User endpoints
        app.MapCreateUser();
        app.MapGetUserById();
        app.MapGetUsers();

        // Product endpoints
        app.MapCreateProduct();
        app.MapGetProductById();
        app.MapGetProducts();

        // Health check endpoint
        app.MapHealthCheck();

        return app;
    }

    /// <summary>
    /// Maps a simple health check endpoint for monitoring.
    /// </summary>
    private static void MapHealthCheck(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
            .WithName("Health")
            .WithOpenApi()
            .WithSummary("Health check")
            .WithDescription("Simple health check endpoint for monitoring.");
    }
}
