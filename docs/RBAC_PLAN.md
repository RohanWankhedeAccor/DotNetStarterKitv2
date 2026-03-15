# Full RBAC with Permissions — DotNetStarterKitv2

## Context

The app already has Users, Roles, and UserRoles tables, but has no enforcement. This plan introduces a proper RBAC model by adding a **Permissions** table (all actions in the system) and a **RolePermissions** mapping table (which role gets which permissions). Permissions are embedded as JWT claims at login so endpoints can use claim-based policies — no hardcoded role checks in code.

---

## Final Data Model

```
Users ←→ UserRoles ←→ Roles ←→ RolePermissions ←→ Permissions
```

### Tables (existing — no changes)
- `Users` — Id, Email, FullName, PasswordHash, Status, AzureAdObjectId, AuthSource, + audit
- `Roles` — Id, Name, Description, + audit
- `UserRoles` — Id, UserId, RoleId, + audit

### Tables (new — migration required)
- `Permissions` — Id, Name (e.g. "users.create"), Description, + audit
- `RolePermissions` — Id, RoleId, PermissionId, + audit

---

## Permissions Catalogue (seeded)

| Key | Description |
|-----|-------------|
| `users.view` | View user list and user details |
| `users.create` | Create new users |
| `users.delete` | Soft-delete a user |
| `roles.assign` | Assign or remove roles from a user |
| `roles.view` | View role list |

## Role → Permission Assignments (seeded)

| Role | Permissions |
|------|-------------|
| Administrator | All 5 |
| Editor | `users.view`, `users.create` |
| Viewer | `users.view` |

## Seeded Users → Roles

| User | Role |
|------|------|
| alice@example.com | Administrator |
| bob@example.com | Editor |
| carol@example.com | Viewer |

---

## How Authorization Works

**At login**, the handler:
1. Loads user's roles (via `UserRoles`)
2. Loads all permissions for those roles (via `RolePermissions → Permissions`)
3. Embeds each permission as a `"permission"` claim in the JWT

**JWT payload example (Bob / Editor):**
```
role: "Editor"
permission: "users.view"
permission: "users.create"
```

**ASP.NET Core policies** check the `permission` claim — not the role directly:
```csharp
"CanViewUsers"   → RequireClaim("permission", "users.view")
"CanCreateUser"  → RequireClaim("permission", "users.create")
"CanAssignRoles" → RequireClaim("permission", "roles.assign")
```

**Frontend** reads `user.roles[]` from the API response (already in Redux) and conditionally renders UI based on the user's permission set.

---

## New Domain Entities

### `Permission`
```csharp
public sealed class Permission : BaseEntity
{
    public string Name { get; private set; }        // e.g. "users.create"
    public string Description { get; private set; }
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];
}
```

### `RolePermission`
```csharp
public sealed class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
```

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/Domain/Entities/Permission.cs` | Permission entity |
| `src/Domain/Entities/RolePermission.cs` | RolePermission junction entity |
| `src/Infrastructure/Persistence/Configurations/PermissionConfiguration.cs` | EF config: table name, unique index on Name, query filter |
| `src/Infrastructure/Persistence/Configurations/RolePermissionConfiguration.cs` | EF config: FKs (RoleId→Roles, PermissionId→Permissions), composite unique index |
| `src/Application/Features/Users/Commands/AssignRoleCommand.cs` | `IRequest<Unit>` with UserId + RoleName |
| `src/Application/Features/Users/Commands/AssignRoleCommandHandler.cs` | Finds user + role, creates UserRole, saves |
| `src/Api/Endpoints/Users/AssignRoleEndpoint.cs` | `POST /api/v1/users/{userId}/roles` → 204, `"CanAssignRoles"` policy |

---

## Files to Modify

### Domain
**`src/Domain/Entities/Role.cs`**
- Add `public ICollection<RolePermission> RolePermissions { get; private set; } = [];`

### Application
**`src/Application/Interfaces/IApplicationDbContext.cs`**
- Add `DbSet<Permission> Permissions { get; }`
- Add `DbSet<RolePermission> RolePermissions { get; }`

**`src/Application/Features/Users/Dtos/UserDto.cs`**
- Add `List<string> Roles { get; set; } = new();`

**`src/Application/Features/Users/Queries/GetUsersQueryHandler.cs`**
- `.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)` + project roles to DTO

**`src/Application/Features/Users/Queries/GetUserByIdQueryHandler.cs`**
- Same Include + roles projection

**`src/Application/Features/Auth/Commands/LoginCommandHandler.cs`**
- After loading roles, also load permissions via `RolePermissions → Permission`
- Pass permissions list to `ITokenService.GenerateToken(..., permissions)`

**`src/Application/Features/Auth/Commands/AzureLoginCommandHandler.cs`**
- Same permissions loading + token generation

**`src/Application/Interfaces/ICurrentUserService.cs`**
- Add `IEnumerable<string> Roles { get; }`
- Add `IEnumerable<string> Permissions { get; }`

### Infrastructure
**`src/Infrastructure/Persistence/ApplicationDbContext.cs`**
- Add `public DbSet<Permission> Permissions => Set<Permission>();`
- Add `public DbSet<RolePermission> RolePermissions => Set<RolePermission>();`

**`src/Infrastructure/Identity/CurrentUserService.cs`**
- Implement `Roles`: read all `ClaimTypes.Role` claims
- Implement `Permissions`: read all `"permission"` claims

**`src/Infrastructure/Identity/JwtTokenService.cs`**
- Add `permissions` parameter to `GenerateToken`
- Add each permission as `new Claim("permission", p)` in the claims list

**`src/Infrastructure/Persistence/DataSeeder.cs`**
- Seed Roles: Administrator, Editor, Viewer (guard with AnyAsync on Roles)
- Seed Permissions: 5 entries (guard with AnyAsync on Permissions)
- Seed RolePermissions: assign per matrix above
- Seed UserRoles: Alice=Admin, Bob=Editor, Carol=Viewer

### API
**`src/Api/Extensions/ServiceCollectionExtensions.cs`**
- Register claim-based policies:
  ```csharp
  options.AddPolicy("CanViewUsers",   p => p.RequireClaim("permission", "users.view"));
  options.AddPolicy("CanCreateUser",  p => p.RequireClaim("permission", "users.create"));
  options.AddPolicy("CanAssignRoles", p => p.RequireClaim("permission", "roles.assign"));
  ```

**`src/Api/Endpoints/Users/CreateUserEndpoint.cs`**
- `.RequireAuthorization("CanCreateUser")`

**`src/Api/Endpoints/Users/GetUsersEndpoint.cs`** + **`GetUserByIdEndpoint.cs`**
- `.RequireAuthorization("CanViewUsers")`

**`src/Api/Extensions/ApplicationBuilderExtensions.cs`**
- Add `app.MapAssignRole();`

**`src/Application/Interfaces/ITokenService.cs`**
- Add `permissions` parameter to `GenerateToken`

### Frontend (`src/Web/src/App.tsx`)
- Add `roles: string[]` to `User` interface
- Read `myRoles` from Redux (`selectUserRoles`)
- Note: frontend role-check is a UX convenience only — API enforces the real permission
- "New User" button: visible when roles include Editor or Administrator
- `UserCard`: show role badges from `user.roles[]`
- `UserCard`: "Assign Role" button visible when roles include Administrator
- Add `useAssignRole()` mutation + role `<select>` dropdown (Admin, Editor, Viewer)

---

## New AssignRole Endpoint

```
POST /api/v1/users/{userId}/roles
Authorization: Cookie / Bearer token
Body: { "roleName": "Editor" }
Policy: "CanAssignRoles"  (permission: roles.assign)

Responses:
  204 No Content  — role assigned successfully
  400 Bad Request — unknown role name
  404 Not Found   — user not found
  401 Unauthorized
  403 Forbidden
```

---

## Migration

One new migration: `AddPermissionsAndRolePermissions`
- Creates `Permissions` table
- Creates `RolePermissions` table with FKs + composite unique index

No changes to existing `Users`, `Roles`, or `UserRoles` tables.

---

## Tests

| Test | Type | File |
|------|------|------|
| `AssignRole_WithAdminToken_Returns204` | Integration | `UsersEndpointTests.cs` |
| `AssignRole_WithEditorToken_Returns403` | Integration | same |
| `AssignRole_WithoutToken_Returns401` | Integration | same |
| `CreateUser_WithViewerToken_Returns403` | Integration | same |
| `GetUsers_WithViewerToken_Returns200` | Integration | same |
| `AssignRoleHandler_ValidInput_AddsUserRole` | Unit | `AssignRoleCommandHandlerTests.cs` |

---

## Verification Checklist

1. `dotnet build --configuration Release /p:TreatWarningsAsErrors=true` — zero errors
2. `dotnet test` — all tests pass
3. `npm run type-check && npm run lint` — zero errors
4. Check DB: `Permissions` (5 rows), `RolePermissions` (8 rows), `Roles` (3 rows), `UserRoles` (3 rows)
5. Login as Alice → JWT contains `permission: users.view/create/delete/roles.assign/roles.view`
6. Login as Bob → JWT contains `permission: users.view/users.create` only
7. Login as Carol → JWT contains `permission: users.view` only
8. As Bob (Editor): "New User" button visible; "Assign Role" button hidden
9. As Carol (Viewer): "New User" button hidden; no assign role
10. As Alice: assign Carol → Editor, re-login Carol, verify "New User" button appears
