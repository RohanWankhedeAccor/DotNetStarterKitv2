using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Api.Extensions;

/// <summary>
/// Service collection extensions for configuring API-specific services.
/// Called from Program.cs during service registration phase.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers API services: Swagger/OpenAPI, JSON serialization settings, CORS, and versioning.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading JWT and other settings.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register endpoints API explorer (required for minimal APIs + Swagger)
        services.AddEndpointsApiExplorer();

        // Configure JWT bearer authentication
        var jwtSecret = configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "DotNetStarterKitv2";
        var jwtAudience = configuration["Jwt:Audience"] ?? "DotNetStarterKitv2-App";

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Read token from HttpOnly cookie when present.
                // Falls back to Authorization: Bearer header if cookie is absent
                // (keeps integration tests that send the header working without changes).
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var cookie = context.Request.Cookies["auth_token"];
                        if (!string.IsNullOrEmpty(cookie))
                        {
                            context.Token = cookie;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        // Configure Swagger/OpenAPI documentation
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DotNetStarterKitv2 API",
                Version = "v1",
                Description = "A clean architecture REST API with CQRS, EF Core, and React frontend.",
                Contact = new OpenApiContact
                {
                    Name = "Development Team",
                    Email = "dev@example.com"
                }
            });

            // Configure JWT Bearer authentication scheme in Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT token for API authentication. Include in Authorization header: Bearer <token>",
                In = ParameterLocation.Header
            });

            // Require JWT token for all endpoints in Swagger UI
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });

            // Include XML documentation comments in Swagger if available
            try
            {
                var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
                foreach (var xmlFile in xmlFiles)
                {
                    options.IncludeXmlComments(xmlFile);
                }
            }
            catch
            {
                // XML files may not be available at runtime — this is not critical
            }
        });

        // Configure JSON serialization (camelCase for API responses)
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = false;
        });

        // Configure CORS for development (allow any localhost port due to Vite dev server)
        services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalhost", policy =>
            {
                policy
                    .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
