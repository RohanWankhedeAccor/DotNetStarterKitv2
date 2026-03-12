# HTTP API Standards & Conventions

Guidelines for REST API design, endpoint structure, error handling, and API versioning.

---

## Endpoint Structure

### URL Format
```
https://[host]:[port]/api/v[version]/[resource]/[action]
```

**Examples:**
- `GET /api/v1/users` — List all users
- `GET /api/v1/users/123` — Get specific user
- `POST /api/v1/users` — Create user
- `PUT /api/v1/users/123` — Update user
- `DELETE /api/v1/users/123` — Delete user

### Versioning
- **URL-based versioning** (preferred): `/api/v1/`, `/api/v2/`
- Increment major version only on breaking changes
- Support previous version for at least 2 minor releases

### Resource Naming
- **Plural nouns** for resource names: `/users`, `/products`, `/roles`
- **Lowercase** with hyphens for multi-word resources: `/user-profiles`, `/access-tokens`
- **No verbs** in URLs (verbs are HTTP methods)

### HTTP Methods (RESTful Semantics)
| Method | Semantics | Status | Body |
|--------|-----------|--------|------|
| **GET** | Retrieve (safe, idempotent) | 200 OK | Response data |
| **POST** | Create new resource | 201 Created | Response with new resource |
| **PUT** | Replace entire resource (idempotent) | 200 OK | Updated resource |
| **PATCH** | Partial update (not commonly used) | 200 OK | Updated resource |
| **DELETE** | Remove resource (idempotent) | 204 No Content | (empty) |
| **HEAD** | Like GET but no body (for health checks) | 200 OK | (empty) |

---

## Request/Response Format

### JSON Serialization
- **Property naming**: `camelCase` (configured in `Program.cs`)
- **Encoding**: UTF-8
- **Content-Type**: `application/json`

**Bad (PascalCase):**
```json
{
  "UserId": 123,
  "FullName": "John Doe"
}
```

**Good (camelCase):**
```json
{
  "userId": 123,
  "fullName": "John Doe"
}
```

### Request Example (Create User)
```http
POST /api/v1/users HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "email": "john@example.com",
  "fullName": "John Doe",
  "password": "SecurePassword123!"
}
```

### Response Example (201 Created)
```http
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/v1/users/123

{
  "id": 123,
  "email": "john@example.com",
  "fullName": "John Doe",
  "createdAt": "2026-03-09T10:30:00Z"
}
```

---

## Pagination

### Request Format
**Query Parameters:**
- `pageNumber` (default: 1, min: 1)
- `pageSize` (default: 10, min: 1, max: 100)

**Example:**
```
GET /api/v1/users?pageNumber=2&pageSize=20
```

### Response Format
```json
{
  "items": [
    { "id": 1, "email": "user1@example.com" },
    { "id": 2, "email": "user2@example.com" }
  ],
  "pageNumber": 2,
  "pageSize": 20,
  "totalPages": 5,
  "totalCount": 100,
  "hasNextPage": true,
  "hasPreviousPage": true
}
```

### Implementation in Minimal APIs
```csharp
app.MapGet("/api/v1/users", async (int pageNumber = 1, int pageSize = 10, IMediator mediator) =>
{
    // Clamp pageSize to max 100
    var clamped = new GetUsersQuery
    {
        PageNumber = Math.Max(pageNumber, 1),
        PageSize = Math.Min(Math.Max(pageSize, 1), 100)
    };

    var result = await mediator.Send(clamped);
    return Results.Ok(result);
})
.Produces<PagedResponse<UserDto>>(StatusCodes.Status200OK)
.WithName("GetUsers")
.WithOpenApi();
```

---

## Error Handling

### Error Response Format (RFC 9457 - Problem Details)

```json
{
  "type": "https://api.example.com/errors/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "User with ID 999 not found",
  "instance": "/api/v1/users/999",
  "traceId": "0HN1GJQFP69G0:00000001"
}
```

### HTTP Status Codes

| Status | Meaning | When to Use |
|--------|---------|------------|
| **200** | OK | GET/PUT/PATCH succeeded, returning data |
| **201** | Created | POST created a new resource |
| **204** | No Content | DELETE succeeded, or action completed with no response |
| **400** | Bad Request | Client sent invalid data (validation failed) |
| **401** | Unauthorized | Missing or invalid authentication |
| **403** | Forbidden | Authenticated, but not authorized |
| **404** | Not Found | Resource doesn't exist |
| **409** | Conflict | Email already exists, FK constraint violation |
| **422** | Unprocessable Entity | Business logic validation failed |
| **429** | Too Many Requests | Rate limit exceeded |
| **500** | Internal Server Error | Unhandled exception |
| **503** | Service Unavailable | Database offline, critical dependency down |

### Implementation in Minimal APIs

**Success (200 OK):**
```csharp
return Results.Ok(data);
```

**Created (201):**
```csharp
return Results.Created($"/api/v1/users/{user.Id}", user);
```

**No Content (204):**
```csharp
return Results.NoContent();
```

**Error (via global exception handler middleware):**
```csharp
throw new NotFoundException("User", userId);
throw new ConflictException("User with this email already exists");
throw new ForbiddenException("You don't have permission to delete this user");
```

### Exception Handling in Middleware

**In `ExceptionHandlerMiddleware.cs`:**
```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (Exception ex)
    {
        await HandleExceptionAsync(context, ex);
    }
}

private static Task HandleExceptionAsync(HttpContext context, Exception exception)
{
    context.Response.ContentType = "application/json";

    var response = exception switch
    {
        NotFoundException notFound => new {
            status = StatusCodes.Status404NotFound,
            detail = notFound.Message
        },
        ConflictException conflict => new {
            status = StatusCodes.Status409Conflict,
            detail = conflict.Message
        },
        ForbiddenException forbidden => new {
            status = StatusCodes.Status403Forbidden,
            detail = forbidden.Message
        },
        FluentValidation.ValidationException validation => new {
            status = StatusCodes.Status400BadRequest,
            detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
        },
        _ => new {
            status = StatusCodes.Status500InternalServerError,
            detail = "An unexpected error occurred"
        }
    };

    context.Response.StatusCode = response.status;
    return context.Response.WriteAsJsonAsync(response);
}
```

---

## Authentication & Authorization

### Header Format
```http
Authorization: Bearer <jwt-token>
```

### Secured Endpoints
```csharp
app.MapPost("/api/v1/users/{id}/assign-role", async (int id, AssignRoleCommand cmd, IMediator mediator) =>
{
    var result = await mediator.Send(cmd);
    return Results.Ok(result);
})
.RequireAuthorization()                    // Requires valid JWT
.WithName("AssignUserRole")
.WithOpenApi();
```

### Role-Based Access
```csharp
.RequireAuthorization("AdminPolicy")       // Custom policy for Admin role
```

---

## CORS & Security Headers

### CORS Configuration (Development)
```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

app.UseCors("AllowLocalhost");
```

### Security Headers (Production)
```csharp
app.UseHsts();                                      // Strict-Transport-Security
app.UseSecurityHeaders();                          // Custom middleware
```

**In `SecurityHeadersMiddleware.cs`:**
```csharp
public async Task InvokeAsync(HttpContext context)
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    await _next(context);
}
```

---

## Rate Limiting

### Configuration
```csharp
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", policy =>
    {
        policy.PermitLimit = 100;
        policy.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();
```

### Applied to Endpoints
```csharp
app.MapPost("/api/v1/users", createUserEndpoint)
    .RequireRateLimiting("fixed");
```

---

## Documentation (OpenAPI/Swagger)

### Endpoint Documentation
```csharp
app.MapGet("/api/v1/users", getUsers)
    .Produces<PagedResponse<UserDto>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("GetUsers")
    .WithOpenApi()
    .WithDescription("Retrieve paginated list of all users")
    .WithSummary("Get all users");
```

### XML Documentation in Handlers
```csharp
/// <summary>
/// Creates a new user with the provided email and password.
/// </summary>
/// <remarks>
/// Email must be unique in the system.
/// Password must meet complexity requirements.
/// </remarks>
/// <param name="command">Create user command with email, name, password.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The newly created user with ID.</returns>
public async Task<UserDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
{
    // Implementation
}
```

---

## Minimal APIs Pattern

### Structure
All endpoints for a resource are grouped in a single endpoint handler class.

**File:** `src/Api/Endpoints/Users/UsersEndpoints.cs`
```csharp
using MediatR;
using Application.Features.Users.Commands;
using Application.Features.Users.Queries;
using Application.Features.Users.Dtos;
using Api.Extensions;

namespace Api.Endpoints.Users;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .WithOpenApi();

        group.MapPost("/", CreateUser);
        group.MapGet("/{id}", GetUserById);
        group.MapGet("/", GetUsers);
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand
        {
            Email = request.Email,
            FullName = request.FullName,
            Password = request.Password
        };
        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/v1/users/{result.Id}", result);
    }

    private static async Task<IResult> GetUserById(
        int id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery { Id = id };
        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUsers(
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery
        {
            PageNumber = Math.Max(pageNumber, 1),
            PageSize = Math.Min(Math.Max(pageSize, 1), 100)
        };
        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }
}
```

**File:** `src/Api/Program.cs`
```csharp
var app = builder.Build();

// Map all endpoint groups
app.MapUsersEndpoints();
app.MapProductsEndpoints();
app.MapRolesEndpoints();
app.MapProjectsEndpoints();

app.Run();
```

---

## Testing Endpoints

### Using REST Client (VS Code Extension)
```http
### Get all users (paginated)
GET https://localhost:5001/api/v1/users?pageNumber=1&pageSize=10
Authorization: Bearer {{token}}

### Create user
POST https://localhost:5001/api/v1/users
Content-Type: application/json

{
  "email": "newuser@example.com",
  "fullName": "New User",
  "password": "SecurePassword123!"
}

### Get specific user
GET https://localhost:5001/api/v1/users/123

### Delete user
DELETE https://localhost:5001/api/v1/users/123
Authorization: Bearer {{token}}
```

### Using curl
```bash
# Get users
curl -s https://localhost:5001/api/v1/users --insecure | jq

# Create user
curl -X POST https://localhost:5001/api/v1/users \
  --insecure \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","fullName":"Test User","password":"Pass123!"}'

# Get specific user
curl -s https://localhost:5001/api/v1/users/123 --insecure | jq
```

---

## Common Patterns

### Request Validation
```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MustAsync(async (email, ct) =>
            {
                var exists = await context.Users
                    .AnyAsync(u => u.Email == email, ct);
                return !exists;
            }).WithMessage("Email already exists");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .Length(2, 100).WithMessage("Name must be 2-100 characters");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase")
            .Matches(@"[0-9]").WithMessage("Must contain digit")
            .Matches(@"[!@#$%^&*]").WithMessage("Must contain special char");
    }
}
```

### Soft Delete Handling
```csharp
// Query automatically excludes IsDeleted=true via global filter
var users = await context.Users
    .AsNoTracking()
    .Where(u => u.Email == email)
    .FirstOrDefaultAsync();

// To include deleted: explicitly remove filter
var usersIncludingDeleted = await context.Users
    .IgnoreQueryFilters()
    .AsNoTracking()
    .FirstOrDefaultAsync();
```

### DTO Projection
```csharp
// GOOD: Project to DTO early
var result = await context.Users
    .AsNoTracking()
    .Where(u => !u.IsDeleted)
    .Select(u => new UserDto
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName
    })
    .ToListAsync();

// BAD: Fetch entire entity, then map
var users = await context.Users.ToListAsync();
var dtos = mapper.Map<List<UserDto>>(users);  // Extra memory!
```

---

## Versioning Strategy

### Breaking Changes
When making breaking changes:
1. Create `/api/v2/` routes alongside `/api/v1/`
2. Implement necessary adapter/converter logic
3. Support v1 for 2 major releases minimum
4. Document migration guide

### Example
```csharp
// Old endpoint (v1)
app.MapGet("/api/v1/products/{id}", GetProductV1);

// New endpoint (v2) - different response shape
app.MapGet("/api/v2/products/{id}", GetProductV2);

private static async Task<IResult> GetProductV1(int id, IMediator mediator)
{
    var result = await mediator.Send(new GetProductQuery { Id = id });
    return Results.Ok(new ProductV1Dto { /* old shape */ });
}

private static async Task<IResult> GetProductV2(int id, IMediator mediator)
{
    var result = await mediator.Send(new GetProductQuery { Id = id });
    return Results.Ok(new ProductV2Dto { /* new shape */ });
}
```

---

## Health Check Endpoint

```csharp
app.MapGet("/health", async (ApplicationDbContext context) =>
{
    try
    {
        await context.Database.ExecuteScalarAsync("SELECT 1");
        return Results.Ok(new { status = "healthy" });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("HealthCheck")
.WithOpenApi();
```

**Test:**
```bash
curl https://localhost:5001/health --insecure
```

---

**API development is about clear contracts. Think like your frontend teammate. 🚀**
