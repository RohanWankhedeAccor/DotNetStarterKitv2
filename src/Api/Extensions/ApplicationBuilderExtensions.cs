using Api.Endpoints.Auth;
using Api.Endpoints.Products;
using Api.Endpoints.Users;
using Api.Middleware;
using Serilog;

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

        // Assigns / propagates X-Correlation-Id and pushes it into Serilog LogContext.
        // Placed inside ExceptionHandlerMiddleware so the exception log also carries the ID.
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Structured HTTP request/response logging via Serilog.
        // Placed after CorrelationIdMiddleware so the request completion log includes CorrelationId.
        app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms | CorrelationId: {CorrelationId}";
            // Enrich Serilog's per-request diagnostic context with the correlation ID
            // so it appears as a structured property (not just inline in the message).
            opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                if (httpContext.Items[CorrelationIdMiddleware.ItemsKey] is string correlationId)
                    diagnosticContext.Set(CorrelationIdMiddleware.ItemsKey, correlationId);
            };
        });

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
        app.MapAzureLogin();  // Azure AD token exchange
        app.MapLogout();      // Clear HttpOnly cookie
        app.MapRefresh();     // Rotate HttpOnly cookie

        // User endpoints
        app.MapCreateUser();
        app.MapGetUserById();
        app.MapGetUsers();
        app.MapAssignRole();

        // Product endpoints
        app.MapCreateProduct();
        app.MapGetProductById();
        app.MapGetProducts();
        app.MapDeleteProduct();

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
