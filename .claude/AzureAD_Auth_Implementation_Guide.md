# Azure AD Authentication — Implementation Plan
## React SPA + .NET Core Minimal API (New Project)

---

## Context

The existing **Staffing** project implements Azure AD / OpenID Connect authentication using OWIN middleware on ASP.NET MVC (.NET Framework). This plan recreates equivalent authentication for a **new project** using modern patterns:

- **Frontend**: React SPA with `@azure/msal-react`
- **Backend**: .NET Core Minimal API with `Microsoft.Identity.Web`
- **Authorization**: Role-based, with roles stored in SQL (same DB schema as Staffing)
- **New** Azure AD App Registration (isolated from Staffing)

Security gaps found in Staffing (nonce validation disabled, secrets in config, hardcoded user lists) are addressed and fixed in this plan.

---

## Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                      AZURE AD TENANT                           │
│  App Registration: "MyApp"                                     │
│  ├── Client ID: <guid>                                         │
│  ├── Redirect URIs: http://localhost:3000                      │
│  ├── Expose API scope: api://<client-id>/access_as_user        │
│  └── Auth flow: PKCE (no implicit grant)                       │
└────────────┬────────────────────────────┬──────────────────────┘
             │ OIDC / PKCE                │ Bearer JWT
             ▼                            ▼
  ┌─────────────────────┐     ┌───────────────────────┐
  │    React SPA        │────►│  .NET Core Minimal API │
  │    Port 3000        │ API │    Port 5000            │
  │  @azure/msal-react  │     │  Microsoft.Identity.Web│
  └─────────────────────┘     └────────────┬───────────┘
                                           │ EF Core
                                           ▼
                               ┌─────────────────────┐
                               │    SQL Database      │
                               │  Users, Roles,       │
                               │  RoleUsers           │
                               └─────────────────────┘
```

**Key differences from Staffing:**
| Staffing (old) | New Project |
|---|---|
| OWIN + OpenID Connect | MSAL.js (PKCE) + Microsoft.Identity.Web |
| Cookie-based sessions | Stateless JWT Bearer tokens |
| CustomRoleProvider (sync) | `OnTokenValidated` event loads DB roles as claims (async) |
| Nonce validation DISABLED | MSAL handles nonce — never disabled |
| Secrets in Web.config | `appsettings.json` + Azure Key Vault |
| Whitelist of 3 users | Full DB-backed role management |

---

## File Structure

### Backend
```
/backend
├── MyApp.Api/
│   ├── Program.cs                    ← Central wiring (auth, CORS, policies)
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── MyApp.Api.csproj
└── MyApp.Infrastructure/
    ├── Data/
    │   ├── AppDbContext.cs           ← EF Core model
    │   └── Migrations/
    ├── Entities/
    │   ├── User.cs
    │   ├── Role.cs
    │   └── RoleUser.cs
    ├── Repositories/
    │   ├── IUserRepository.cs
    │   └── UserRepository.cs
    └── Services/
        ├── IRoleService.cs
        └── RoleService.cs            ← Mirror of Staffing's RoleService
```

### Frontend
```
/frontend/src/
├── auth/
│   ├── msalConfig.ts                 ← MSAL instance + token requests
│   ├── authProvider.tsx              ← MsalProvider wrapper
│   ├── useAuth.ts                    ← login, logout, acquireToken hook
│   └── AuthGuard.tsx                 ← Route protection component
├── api/
│   └── apiClient.ts                  ← Axios + silent token interceptor
├── components/
│   ├── SignInButton.tsx
│   └── SignOutButton.tsx
└── App.tsx
```

---

## Phase 1 — Azure AD App Registration (Azure Portal)

1. **New registration** → Azure AD → App registrations → New registration
   - Name: `MyApp`
   - Supported account types: `Single tenant`
   - Redirect URI type: `Single-page application (SPA)` → `http://localhost:3000`

2. **Note from Overview:**
   - `Application (client) ID` → `CLIENT_ID`
   - `Directory (tenant) ID` → `TENANT_ID`

3. **Authentication blade:**
   - Add production redirect URI (e.g., `https://yourdomain.com`)
   - **Leave both Implicit grant checkboxes UNCHECKED** (PKCE only — no implicit flow)

4. **Expose an API blade:**
   - Accept default App ID URI: `api://<CLIENT_ID>`
   - Add scope: `access_as_user` (Admins and users can consent)
   - Full scope string: `api://<CLIENT_ID>/access_as_user`

5. **API Permissions blade:**
   - Keep default `User.Read` (Microsoft Graph, Delegated)
   - Add: My APIs → MyApp → `access_as_user`
   - Grant admin consent

6. **Token configuration blade:**
   - Add optional claim → Access token → add: `upn`, `preferred_username`, `email`
   - This ensures UPN is available for DB user lookup

7. **Record values:**
   ```
   TENANT_ID = xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
   CLIENT_ID = yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy
   SCOPE     = api://yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy/access_as_user
   ```

---

## Phase 2 — Database Schema

Mirror Staffing's `Users` / `Roles` / `RoleUsers` schema with English column names.

```sql
CREATE TABLE Users (
    Username    NVARCHAR(128) NOT NULL PRIMARY KEY,
    FirstName   NVARCHAR(256) NULL,
    LastName    NVARCHAR(256) NULL,
    Email       NVARCHAR(256) NULL,
    IsActive    BIT NOT NULL DEFAULT 1
);

CREATE TABLE Roles (
    Id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Label         NVARCHAR(256) NULL,
    Code          NVARCHAR(5) NOT NULL,   -- e.g. ADMIN, MGR, L2
    Dashboard     BIT NOT NULL DEFAULT 0,
    IsValidation  BIT NOT NULL DEFAULT 0
);

CREATE TABLE RoleUsers (
    Role_Id        INT NOT NULL,
    User_Username  NVARCHAR(128) NOT NULL,
    PRIMARY KEY (Role_Id, User_Username),
    CONSTRAINT FK_RoleUsers_Role FOREIGN KEY (Role_Id) REFERENCES Roles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RoleUsers_User FOREIGN KEY (User_Username) REFERENCES Users(Username) ON DELETE CASCADE
);

-- Seed initial roles (adjust codes to match your domain)
INSERT INTO Roles (Label, Code, Dashboard, IsValidation) VALUES
    ('Administrator', 'ADMIN', 1, 0),
    ('Manager',       'MGR',   1, 0),
    ('Level 2',       'L2',    0, 0),
    ('Validator',     'VALID', 0, 1);
```

**EF Core Entities** (in `MyApp.Infrastructure/Entities/`):

`User.cs`:
```csharp
public class User {
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RoleUser> RoleUsers { get; set; } = new List<RoleUser>();
}
```

`Role.cs`:
```csharp
public class Role {
    public int Id { get; set; }
    public string? Label { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool Dashboard { get; set; }
    public bool IsValidation { get; set; }
    public ICollection<RoleUser> RoleUsers { get; set; } = new List<RoleUser>();
}
```

`RoleUser.cs` (junction):
```csharp
public class RoleUser {
    public int RoleId { get; set; }
    public string UserUsername { get; set; } = string.Empty;
    public Role Role { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

`AppDbContext.cs` — configure many-to-many with explicit junction table (mirrors `Staffing.Context.cs`).

---

## Phase 3 — .NET Core Minimal API Backend

### NuGet Packages
```xml
<PackageReference Include="Microsoft.Identity.Web" Version="3.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.*" />
```

### `appsettings.json`
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "Audience": "api://YOUR_CLIENT_ID",
    "ValidateIssuer": true,
    "ValidIssuers": [
      "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0",
      "https://sts.windows.net/YOUR_TENANT_ID/"
    ]
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=MyAppDb;..."
  },
  "AllowedOrigins": ["http://localhost:3000"]
}
```

### `RoleService.cs` (mirrors `Staffing.Services/RoleService.cs`)
```csharp
public class RoleService : IRoleService
{
    private readonly AppDbContext _db;
    public RoleService(AppDbContext db) => _db = db;

    public async Task<List<string>> GetRolesForUserAsync(string username)
    {
        var user = await _db.Users
            .Where(u => u.Username.ToLower() == username.ToLowerInvariant() && u.IsActive)
            .Include(u => u.RoleUsers).ThenInclude(ru => ru.Role)
            .FirstOrDefaultAsync();

        if (user is null) return new List<string>();

        var roles = user.RoleUsers.Select(ru => ru.Role.Code.Trim()).ToList();

        // Mirror Staffing: synthetic DASHBOARD role
        if (user.RoleUsers.Any(ru => ru.Role.Dashboard || ru.Role.IsValidation))
            roles.Add("DASHBOARD");

        return roles;
    }

    public async Task EnsureUserExistsAsync(string username, string? email,
                                             string? firstName, string? lastName)
    {
        var normalized = username.ToLowerInvariant();
        if (!await _db.Users.AnyAsync(u => u.Username.ToLower() == normalized))
        {
            _db.Users.Add(new User { Username = normalized, Email = email,
                                     FirstName = firstName, LastName = lastName });
            await _db.SaveChangesAsync();
        }
    }
}
```

### `Program.cs` — Core wiring
```csharp
// 1. Azure AD JWT Bearer Auth
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// 2. Authorization policies (role codes from DB)
builder.Services.AddAuthorization(options => {
    options.AddPolicy("Authenticated",    p => p.RequireAuthenticatedUser());
    options.AddPolicy("RequireAdmin",     p => p.RequireAuthenticatedUser()
                                                 .RequireClaim("roles_db", "ADMIN"));
    options.AddPolicy("RequireManager",   p => p.RequireAuthenticatedUser()
                                                 .RequireClaim("roles_db", "MGR"));
    options.AddPolicy("RequireDashboard", p => p.RequireAuthenticatedUser()
                                                 .RequireClaim("roles_db", "DASHBOARD"));
});

// 3. DB + Services
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IRoleService, RoleService>();

// 4. CORS
builder.Services.AddCors(o => o.AddPolicy("SpaPolicy", p =>
    p.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()!)
     .AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

// 5. Inject DB roles as claims on token validation
//    Replaces Staffing's CustomRoleProvider + AzureAdAuthorizeAttribute pattern
builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme, options => {
        options.Events = new JwtBearerEvents {
            OnTokenValidated = async context => {
                var roleService = context.HttpContext.RequestServices
                                         .GetRequiredService<IRoleService>();
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity is null) return;

                // Extract UPN — mirrors Staffing's NameIdentifier / Name claim fallback
                var upn = identity.FindFirst("preferred_username")?.Value
                       ?? identity.FindFirst(ClaimTypes.Upn)?.Value;
                if (string.IsNullOrWhiteSpace(upn)) return;

                await roleService.EnsureUserExistsAsync(upn,
                    identity.FindFirst("email")?.Value,
                    identity.FindFirst(ClaimTypes.GivenName)?.Value,
                    identity.FindFirst(ClaimTypes.Surname)?.Value);

                var roles = await roleService.GetRolesForUserAsync(upn);
                foreach (var role in roles)
                    identity.AddClaim(new Claim("roles_db", role));
            }
        };
    });

// 6. Pipeline (ORDER MATTERS)
app.UseCors("SpaPolicy");     // before Auth
app.UseAuthentication();
app.UseAuthorization();

// 7. Endpoints
app.MapGet("/api/health",    () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/me",        (HttpContext h) => Results.Ok(new {
    upn   = h.User.FindFirst("preferred_username")?.Value,
    roles = h.User.FindAll("roles_db").Select(c => c.Value)
})).RequireAuthorization("Authenticated");
app.MapGet("/api/dashboard", () => Results.Ok()).RequireAuthorization("RequireDashboard");
app.MapGet("/api/admin",     () => Results.Ok()).RequireAuthorization("RequireAdmin");
```

---

## Phase 4 — React SPA Frontend

### Install
```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install @azure/msal-browser @azure/msal-react axios react-router-dom
```

### `.env`
```
VITE_AZURE_CLIENT_ID=yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy
VITE_AZURE_TENANT_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
VITE_AZURE_REDIRECT_URI=http://localhost:3000
VITE_API_SCOPE=api://yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy/access_as_user
VITE_API_BASE_URL=http://localhost:5000
```

### `src/auth/msalConfig.ts`
```typescript
export const msalConfig: Configuration = {
  auth: {
    clientId:    import.meta.env.VITE_AZURE_CLIENT_ID,
    authority:   `https://login.microsoftonline.com/${import.meta.env.VITE_AZURE_TENANT_ID}/v2.0`,
    redirectUri: import.meta.env.VITE_AZURE_REDIRECT_URI,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.SessionStorage,  // NOT localStorage
    storeAuthStateInCookie: false,
  },
  system: { loggerOptions: { piiLoggingEnabled: false } }
};

export const apiTokenRequest = {
  scopes: [import.meta.env.VITE_API_SCOPE],
};

export const msalInstance = new PublicClientApplication(msalConfig);
```

### `src/auth/useAuth.ts`
- `login()` → `instance.loginRedirect(apiTokenRequest)`
- `logout()` → `instance.logoutRedirect(...)`
- `acquireToken()` → `acquireTokenSilent` first, falls back to `acquireTokenRedirect`
- Returns `{ isAuthenticated, login, logout, acquireToken, username, name }`

### `src/auth/AuthGuard.tsx`
- Checks `useIsAuthenticated()` from MSAL
- If not authenticated → calls `login()` (redirect, no flash of content)
- Renders children when authenticated

### `src/api/apiClient.ts`
- Axios instance with base URL from env
- **Request interceptor**: `acquireTokenSilent` → sets `Authorization: Bearer <token>`
- **Response interceptor**: 401 → triggers `acquireTokenRedirect`

### `src/App.tsx` routing example
```tsx
<Route path="/dashboard" element={<AuthGuard><Dashboard /></AuthGuard>} />
<Route path="/admin"     element={<AuthGuard><Admin /></AuthGuard>} />
```

---

## Phase 5 — Security Fixes vs Staffing

| # | Staffing Issue | Fix in New Project |
|---|---|---|
| 1 | `ShouldValidateNonce = false` | MSAL handles nonce automatically — never touch it |
| 2 | Implicit grant potentially enabled | Implicit grant checkboxes **unchecked** in portal |
| 3 | Secrets in Web.config (SMTP pwd, DB pwd) | `appsettings.json` secrets → Azure Key Vault / env vars |
| 4 | Hardcoded authorized users in config | Full DB-backed `RoleUsers` table |
| 5 | Role lookup with no fallback for missing user | `EnsureUserExistsAsync` auto-provisions user on first login |
| 6 | Tokens potentially in localStorage | `BrowserCacheLocation.SessionStorage` |
| 7 | No audience validation | `Audience: "api://CLIENT_ID"` in `appsettings.json` |
| 8 | Frontend treated as security boundary | Backend enforces roles via JWT claim policy; frontend is UX-only |

---

## Phase 6 — Verification & Testing

### Step-by-step smoke test
```bash
# 1. No token → expect 401
curl -i http://localhost:5000/api/me

# 2. Valid token → expect 200 with upn + roles
curl -i http://localhost:5000/api/me -H "Authorization: Bearer <token>"

# 3. CORS preflight
curl -i -X OPTIONS http://localhost:5000/api/me \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: GET" \
  -H "Access-Control-Request-Headers: Authorization"

# 4. Assign ADMIN role in DB then test
INSERT INTO RoleUsers (Role_Id, User_Username)
SELECT Id, 'testuser@tenant.com' FROM Roles WHERE Code = 'ADMIN';

curl -i http://localhost:5000/api/admin -H "Authorization: Bearer <token>"
```

### JWT verification (paste token at jwt.ms)
- `iss` matches tenant issuer
- `aud` = `api://YOUR_CLIENT_ID`
- `preferred_username` present
- `scp` = `access_as_user`
- `nonce` is present

### End-to-end checklist
- [ ] Unauthenticated visit to `/dashboard` redirects to Microsoft login
- [ ] After login, returns to originally requested page
- [ ] `/api/me` returns correct UPN and DB roles
- [ ] User with no DB roles gets empty roles array (no 500)
- [ ] New user auto-provisioned in `Users` table on first login
- [ ] Non-ADMIN user gets 403 on `/api/admin`
- [ ] Logout clears session and MSAL state
- [ ] Requests carry `Authorization: Bearer` header
- [ ] No tokens appear in `localStorage`

---

## Critical Files

| File | Why Critical |
|---|---|
| `Program.cs` | Central wiring: auth, CORS, JWT event, policies |
| `RoleService.cs` | Direct mirror of Staffing's role logic — must be correct |
| `AppDbContext.cs` | EF model must match SQL schema exactly |
| `msalConfig.ts` | Wrong tenant/scope/cache here silently breaks everything |
| `apiClient.ts` | All API calls flow through here; token acquisition logic lives here |
