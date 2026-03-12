using Application;
using Api.Extensions;
using Infrastructure.DependencyInjection;

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
// 2. CONFIGURE MIDDLEWARE PIPELINE
// ═══════════════════════════════════════════════════════════════════════════════

// Configure middleware in the correct order
app.UseApiMiddleware();

// ═══════════════════════════════════════════════════════════════════════════════
// 3. MAP API ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════

app.MapApiEndpoints();

// ═══════════════════════════════════════════════════════════════════════════════
// 4. RUN THE APPLICATION
// ═══════════════════════════════════════════════════════════════════════════════

app.Run();
