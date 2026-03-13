using Application;
using Api.Extensions;
using Application.Interfaces;
using Infrastructure.DependencyInjection;
using Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
#pragma warning disable CS1591
public partial class Program { }
#pragma warning restore CS1591
