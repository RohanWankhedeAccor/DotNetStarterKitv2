using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

/// <summary>
/// Seeds the database with reference data and demo users on first startup.
/// Each seed method is independently guarded so partial seeds (e.g. roles but no
/// permissions yet) are safe to run on an existing database.
/// </summary>
public static class DataSeeder
{
    // ── Catalogue constants ─────────────────────────────────────────────────

    private static readonly string[] RoleNames = ["Administrator", "Editor", "Viewer"];

    private static readonly (string Name, string Description)[] PermissionCatalogue =
    [
        ("users.view",   "View user list and user details"),
        ("users.create", "Create new users"),
        ("users.delete", "Soft-delete a user"),
        ("roles.assign", "Assign or remove roles from a user"),
        ("roles.view",   "View role list"),
    ];

    // Role → permitted permission keys
    private static readonly Dictionary<string, string[]> RolePermissionMap = new()
    {
        ["Administrator"] = ["users.view", "users.create", "users.delete", "roles.assign", "roles.view"],
        ["Editor"]        = ["users.view", "users.create"],
        ["Viewer"]        = ["users.view"],
    };

    // Demo user email → role name
    private static readonly Dictionary<string, string> UserRoleMap = new()
    {
        ["alice@example.com"] = "Administrator",
        ["bob@example.com"]   = "Editor",
        ["carol@example.com"] = "Viewer",
    };

    // ── Public entry point ──────────────────────────────────────────────────

    /// <summary>
    /// Seeds all reference data and demo records in dependency order.
    /// Each step is idempotent — re-running on a populated database is safe.
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(context, passwordHasher, logger, cancellationToken);
        await SeedRolesAsync(context, logger, cancellationToken);
        await SeedPermissionsAsync(context, logger, cancellationToken);
        await SeedRolePermissionsAsync(context, logger, cancellationToken);
        await SeedUserRolesAsync(context, logger, cancellationToken);
    }

    // ── Private seed methods ────────────────────────────────────────────────

    private static async Task SeedUsersAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var existingEmails = await context.Users.IgnoreQueryFilters()
            .Select(u => u.Email)
            .ToHashSetAsync(cancellationToken);

        var allSeedUsers = new[]
        {
            ("Alice",  "Johnson",   "alice@example.com"),
            ("Bob",    "Martinez",  "bob@example.com"),
            ("Carol",  "Williams",  "carol@example.com"),
        };

        var missing = allSeedUsers
            .Where(u => !existingEmails.Contains(u.Item3))
            .Select(u => CreateUser(u.Item1, u.Item2, u.Item3, passwordHasher, now))
            .ToArray();

        if (missing.Length == 0) return;

        await context.Users.AddRangeAsync(missing, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[DataSeeder] Seeded {Count} users", missing.Length);
    }

    private static async Task SeedRolesAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var descriptions = new Dictionary<string, string>
        {
            ["Administrator"] = "System administrator with full access to all features.",
            ["Editor"]        = "Can view and create users.",
            ["Viewer"]        = "Read-only access to user list and user details.",
        };

        var existingNames = await context.Roles.IgnoreQueryFilters()
            .Select(r => r.Name)
            .ToHashSetAsync(cancellationToken);

        var missing = RoleNames
            .Where(name => !existingNames.Contains(name))
            .Select(name => CreateAuditedEntity(new Role(name, descriptions[name]), now))
            .ToArray();

        if (missing.Length == 0) return;

        await context.Roles.AddRangeAsync(missing, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[DataSeeder] Seeded {Count} roles", missing.Length);
    }

    private static async Task SeedPermissionsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Permissions.IgnoreQueryFilters().AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;
        var permissions = PermissionCatalogue
            .Select(p => CreateAuditedEntity(new Permission(p.Name, p.Description), now))
            .ToArray();

        await context.Permissions.AddRangeAsync(permissions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[DataSeeder] Seeded {Count} permissions", permissions.Length);
    }

    private static async Task SeedRolePermissionsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var roles = await context.Roles.IgnoreQueryFilters()
            .ToDictionaryAsync(r => r.Name, cancellationToken);
        var permissions = await context.Permissions.IgnoreQueryFilters()
            .ToDictionaryAsync(p => p.Name, cancellationToken);

        var existing = await context.RolePermissions.IgnoreQueryFilters()
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(cancellationToken);
        var existingSet = existing.Select(rp => (rp.RoleId, rp.PermissionId)).ToHashSet();

        var assignments = new List<RolePermission>();
        foreach (var (roleName, permNames) in RolePermissionMap)
        {
            if (!roles.TryGetValue(roleName, out var role)) continue;
            foreach (var permName in permNames)
            {
                if (!permissions.TryGetValue(permName, out var perm)) continue;
                if (existingSet.Contains((role.Id, perm.Id))) continue;
                assignments.Add(CreateAuditedEntity(new RolePermission(role.Id, perm.Id), now));
            }
        }

        if (assignments.Count == 0) return;

        await context.RolePermissions.AddRangeAsync(assignments, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[DataSeeder] Seeded {Count} role-permission assignments", assignments.Count);
    }

    private static async Task SeedUserRolesAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var users = await context.Users.IgnoreQueryFilters()
            .Where(u => UserRoleMap.Keys.Contains(u.Email))
            .ToDictionaryAsync(u => u.Email, cancellationToken);

        var roles = await context.Roles.IgnoreQueryFilters()
            .ToDictionaryAsync(r => r.Name, cancellationToken);

        var existing = await context.UserRoles.IgnoreQueryFilters()
            .Select(ur => new { ur.UserId, ur.RoleId })
            .ToListAsync(cancellationToken);
        var existingSet = existing.Select(ur => (ur.UserId, ur.RoleId)).ToHashSet();

        var assignments = new List<UserRole>();
        foreach (var (email, roleName) in UserRoleMap)
        {
            if (!users.TryGetValue(email, out var user)) continue;
            if (!roles.TryGetValue(roleName, out var role)) continue;
            if (existingSet.Contains((user.Id, role.Id))) continue;
            assignments.Add(CreateAuditedEntity(new UserRole(user.Id, role.Id), now));
        }

        if (assignments.Count == 0) return;

        await context.UserRoles.AddRangeAsync(assignments, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[DataSeeder] Seeded {Count} user-role assignments", assignments.Count);
    }

    // ── Factory helpers ─────────────────────────────────────────────────────

    private static User CreateUser(string firstName, string lastName, string email, IPasswordHasher hasher, DateTimeOffset now)
    {
        var user = new User(email, firstName, lastName, hasher.HashPassword("Password123!"));
        user.Activate();
        return CreateAuditedEntity(user, now);
    }

    private static T CreateAuditedEntity<T>(T entity, DateTimeOffset now) where T : Domain.Common.BaseEntity
    {
        entity.CreatedAt = now;
        entity.CreatedBy = "seed";
        entity.ModifiedAt = now;
        entity.ModifiedBy = "seed";
        return entity;
    }
}
