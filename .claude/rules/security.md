# Security Standards & Guidelines

Best practices for authentication, authorization, secrets management, and secure API design.

---

## Authentication (Phase 2)

### Current State (Phase 1)
- Mock authentication in place
- No real JWT validation
- For development only

### Phase 2 Implementation (Planned)
**Entra ID (Azure AD)** with MSAL:
- Integrate Microsoft.Identity.Web
- JWT token validation
- Refresh token handling
- Role-based claims

See `.claude/AUTH_PLAN.md` for detailed Phase 2 roadmap.

---

## Secrets Management

### ❌ What NOT to Do

**Never commit secrets:**
```csharp
// ❌ BAD: Password hardcoded
string connectionString = "Server=localhost;Password=MyPassword123";

// ❌ BAD: API key in code
var apiKey = "sk_live_abc123xyz";
```

**Never put secrets in `appsettings.json`:**
```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Password=Secret"  // ❌ NEVER
  }
}
```

### ✅ What to Do

#### Development: User Secrets
Store secrets locally without committing:

```bash
# Initialize secrets for project
dotnet user-secrets init --project src/Api

# Store a secret
dotnet user-secrets set "Database:ConnectionString" \
  "Server=localhost;Password=Secret" --project src/Api

# View all secrets
dotnet user-secrets list --project src/Api

# Remove a secret
dotnet user-secrets remove "Database:Password" --project src/Api
```

Secrets stored in: `%APPDATA%\Microsoft\UserSecrets\[project-id]\secrets.json`

#### Production: Azure Key Vault
```csharp
// In Program.cs
var app = builder
    .AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential()
    );
```

#### Configuration Priority (Highest → Lowest)
1. User Secrets (dev)
2. Environment Variables
3. appsettings.{Environment}.json (NO SECRETS)
4. appsettings.json (NO SECRETS)

### appsettings Files Structure

**appsettings.json (committed, NO SECRETS):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  "Database": {
    "Provider": "SqlServer"
  },
  "Authentication": {
    "Scheme": "Bearer",
    "Authority": "" // Leave empty, set in secrets
  }
}
```

**appsettings.Development.json (committed, NO SECRETS):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

**Secrets (NOT committed, stored via User Secrets or Key Vault):**
```
Database:ConnectionString=Data Source=...\SQLEXPRESS;Initial Catalog=DotNetStarterKitV2;Integrated Security=True
Authentication:Authority=https://login.microsoftonline.com/[tenant-id]
Authentication:ClientId=[app-id]
Authentication:ClientSecret=[secret-value]
SendGrid:ApiKey=SG.xxxxxxx
```

---

## CORS & Origin Policy

### Current Configuration (Development)

**Allowed origins:**
- `http://localhost:5173`
- `https://localhost:5173`

**Implementation:** `src/Api/Extensions/ServiceCollectionExtensions.cs`
```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();  // For cookies/auth headers
    });
});

app.UseCors("AllowLocalhost");
```

### Production Configuration
```csharp
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["https://yourdomain.com"];

options.AddPolicy("Production", policy =>
{
    policy
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
});
```

### Configuration (secrets):
```
Cors:AllowedOrigins:0=https://yourdomain.com
Cors:AllowedOrigins:1=https://www.yourdomain.com
```

---

## HTTP Security Headers

### Implementation: SecurityHeadersMiddleware

**File:** `src/Api/Middleware/SecurityHeadersMiddleware.cs`
```csharp
using Microsoft.AspNetCore.Http;

namespace Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add(
            "Permissions-Policy",
            "geolocation=(), microphone=(), camera=(), payment=()"
        );

        // HSTS (only in HTTPS)
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Add(
                "Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload"
            );
        }

        await _next(context);
    }
}
```

### Register in Program.cs
```csharp
app.UseSecurityHeaders();  // After CORS, before auth
```

### Headers Explained
| Header | Purpose |
|--------|---------|
| `X-Content-Type-Options: nosniff` | Prevent MIME-sniffing attacks |
| `X-Frame-Options: DENY` | Prevent clickjacking (framing) |
| `X-XSS-Protection: 1; mode=block` | Enable browser XSS filters |
| `Referrer-Policy: strict-origin-when-cross-origin` | Control referrer info leakage |
| `Permissions-Policy` | Disable camera, microphone, geolocation, payment APIs |
| `Strict-Transport-Security` | Force HTTPS for entire site (HSTS) |

---

## Input Validation

### Frontend Validation
**Always validate on frontend with Zod:**
```typescript
const CreateUserSchema = z.object({
  email: z.string().email('Invalid email'),
  fullName: z.string().min(2, 'Min 2 characters'),
  password: z
    .string()
    .min(8, 'Min 8 characters')
    .regex(/[A-Z]/, 'Must contain uppercase')
    .regex(/[0-9]/, 'Must contain number')
})
```

### Backend Validation
**Always re-validate on backend with FluentValidation:**
```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) =>
            {
                var exists = await context.Users
                    .AnyAsync(u => u.Email == email, ct);
                return !exists;  // Email must not exist
            });

        RuleFor(x => x.FullName)
            .NotEmpty()
            .Length(2, 100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[0-9]")
            .Matches(@"[!@#$%^&*]");
    }
}
```

**Never trust frontend validation alone!** Always validate on backend.

---

## Password Security

### Requirements
Enforce strong passwords:
- Minimum 8 characters
- At least one uppercase letter (A-Z)
- At least one digit (0-9)
- At least one special character (!@#$%^&*)

### Hashing (Phase 2)
Never store plaintext passwords. Use:
- **ASP.NET Core Identity** with Argon2 hashing (recommended)
- Or PBKDF2 with 100,000+ iterations

**Implementation (Phase 2):**
```csharp
using Microsoft.AspNetCore.Identity;

public class User : IdentityUser
{
    public string FullName { get; set; }
    // ... other fields
}

// In DI:
services.AddIdentityCore<User>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

---

## Authorization

### Role-Based Access Control (RBAC)

**Roles in database:**
```sql
INSERT INTO Roles (Name) VALUES ('Admin')
INSERT INTO Roles (Name) VALUES ('User')
INSERT INTO Roles (Name) VALUES ('Moderator')
```

### Policy-Based Authorization (Phase 2)

**Define policies in Program.cs:**
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Admin");
    });

    options.AddPolicy("ContentModeration", policy =>
    {
        policy.RequireRole("Admin", "Moderator");
    });

    options.AddPolicy("CanDeleteUsers", policy =>
    {
        policy.Requirements.Add(new CanDeleteUsersRequirement());
    });
});
```

**Use in endpoints:**
```csharp
app.MapDelete("/api/v1/users/{id}", DeleteUser)
    .RequireAuthorization("AdminOnly");

app.MapDelete("/api/v1/users/{id}", DeleteUser)
    .RequireAuthorization("CanDeleteUsers");
```

### Permission Checks in Handlers

```csharp
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        // Check if current user is Admin
        if (!_currentUser.Roles.Contains("Admin"))
        {
            throw new ForbiddenException("Only admins can delete users");
        }

        // Check if trying to delete self
        if (request.UserId == _currentUser.UserId)
        {
            throw new ForbiddenException("Cannot delete your own account");
        }

        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken: ct);
        if (user == null)
            throw new NotFoundException("User", request.UserId);

        user.IsDeleted = true;
        await _context.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
```

---

## SQL Injection Prevention

### ✅ Safe: Use EF Core LINQ
```csharp
// Safe: EF Core parameterizes automatically
var user = await context.Users
    .FirstOrDefaultAsync(u => u.Email == email);
```

### ❌ Unsafe: String Concatenation
```csharp
// ❌ NEVER do this
var sql = $"SELECT * FROM Users WHERE Email = '{email}'";
var result = context.Users.FromSqlInterpolated(sql);
```

### ✅ Safe: Parameterized Queries
```csharp
// If using raw SQL:
var email = "test@example.com";
var user = await context.Users
    .FromSql($"SELECT * FROM Users WHERE Email = {email}")
    .FirstOrDefaultAsync();

// EF Core automatically parameterizes the {email} placeholder
```

---

## XSS Prevention (Frontend)

### React Auto-Escaping
React escapes HTML by default:

```typescript
// ✅ SAFE: React escapes HTML
const userData = "<script>alert('xss')</script>";
return <div>{userData}</div>;  // Renders as text, not script

// ❌ UNSAFE: dangerouslySetInnerHTML
return <div dangerouslySetInnerHTML={{ __html: userData }} />;  // NEVER use this
```

### Never Use `dangerouslySetInnerHTML`
Unless you control the content and have sanitized it:
```typescript
import DOMPurify from 'dompurify'

// ✅ SAFE with sanitization
const sanitized = DOMPurify.sanitize(htmlContent)
return <div dangerouslySetInnerHTML={{ __html: sanitized }} />
```

---

## CSRF Prevention

### Token-Based CSRF Protection
ASP.NET Core provides automatic CSRF protection:

```csharp
// Automatically generated and validated for POST/PUT/DELETE
app.MapPost("/api/v1/users", CreateUser)
    .RequireAntiforgeryToken();
```

### For SPA (JSON APIs)
Include token in custom header:

**Backend:**
```csharp
app.MapGet("/csrf-token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var token = antiforgery.GetAndStoreTokens(context).RequestToken;
    return Results.Ok(new { token });
})
```

**Frontend:**
```typescript
// Get token on app load
const response = await fetch('/csrf-token')
const { token } = await response.json()

// Include in requests
axios.defaults.headers.common['X-CSRF-TOKEN'] = token
```

---

## Rate Limiting

### Implementation
```csharp
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("default", policy =>
    {
        policy.PermitLimit = 100;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 2;  // Queue up to 2 requests
    });

    options.AddFixedWindowLimiter("strict", policy =>
    {
        policy.PermitLimit = 10;
        policy.Window = TimeSpan.FromMinutes(1);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

app.UseRateLimiter();
```

### Apply to Endpoints
```csharp
// Default: 100/min
app.MapGet("/api/v1/users", GetUsers)
    .RequireRateLimiting("default");

// Strict: 10/min (auth endpoint)
app.MapPost("/api/v1/login", Login)
    .RequireRateLimiting("strict");

// No limit (health check)
app.MapGet("/health", HealthCheck)
    .ExcludeFromRateLimiting();
```

---

## Dependency Security

### NuGet Package Management

**Keep packages updated:**
```bash
# Check for outdated packages
dotnet outdated

# Update to latest
dotnet package update

# Check for vulnerabilities
dotnet list package --vulnerable
```

**Approved Packages Only:**
- Do not add new packages without justification
- Review dependencies in CLAUDE.md approved stack
- Check NuGet for security advisories

### Lock Files
Commit `packages.lock.json` to ensure reproducible builds:
```bash
dotnet restore --use-lock-files --locked-mode
```

---

## Logging & Monitoring

### Never Log Sensitive Data
```csharp
// ❌ BAD: Logs password
logger.LogInformation("User {Email} created with password {Password}", email, password);

// ✅ GOOD: Doesn't log password
logger.LogInformation("User {Email} created successfully", email);

// ✅ GOOD: Masks sensitive data
var maskedEmail = email[..Math.Min(3, email.Length)] + "***";
logger.LogInformation("User {Email} created", maskedEmail);
```

### Structured Logging
```csharp
using Serilog;

logger.LogInformation("User creation attempt",
    new { UserId = userId, Timestamp = DateTime.UtcNow });
```

### Correlation IDs
```csharp
// All logs for a request share correlation ID
var correlationId = context.TraceIdentifier;
using (LogContext.PushProperty("CorrelationId", correlationId))
{
    logger.LogInformation("Processing user request");
}
```

---

## Security Checklist

### Before Deployment
- [ ] All secrets in Key Vault (no hardcoded values)
- [ ] CORS origins restricted to known domains
- [ ] Security headers configured
- [ ] Rate limiting enabled
- [ ] HTTPS enforced (HSTS header)
- [ ] CSRF protection enabled
- [ ] Input validation on all endpoints
- [ ] No sensitive data in logs
- [ ] Dependencies scanned for vulnerabilities
- [ ] Authentication properly integrated (Phase 2)
- [ ] Authorization policies defined (Phase 2)
- [ ] Error messages don't leak system details
- [ ] Database credentials use managed identity/Key Vault
- [ ] API versioning in place

### Development Guidelines
- [ ] Never commit `.env` files (commit `.env.example` only)
- [ ] Use user-secrets locally
- [ ] Never hardcode API keys
- [ ] Always validate input on backend
- [ ] Sanitize any user-generated HTML
- [ ] Use parameterized queries
- [ ] Review third-party dependencies
- [ ] Test authorization scenarios

---

## Incident Response

### If Secrets Are Exposed
1. Immediately rotate the secret in Key Vault
2. Check logs for unauthorized access
3. Notify relevant stakeholders
4. Rekey database credentials if SQL password leaked
5. Force password reset for affected users
6. Document incident for post-mortem

### If You Commit a Secret
```bash
# Option 1: Undo last commit (if not pushed)
git reset --soft HEAD~1

# Option 2: Remove from history (if pushed)
# Use git-filter-repo:
git-filter-repo --path [file] --invert-paths
git push --force-with-lease

# Option 3: Mark in secrets management as compromised and rotate
```

---

## Resources
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Microsoft Security Best Practices](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/security-warnings)
- [ASP.NET Core Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/)

---

**Security is not optional. It's a core responsibility. 🔒**
