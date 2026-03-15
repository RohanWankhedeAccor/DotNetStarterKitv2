using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Integration.Helpers;

namespace Integration.Api.Users;

public class UsersEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    // All permissions granted to simulate an admin — keeps existing tests green.
    private static readonly string[] AllPermissions =
        ["users.view", "users.create", "users.delete", "roles.assign", "roles.view"];

    public UsersEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/v1/users ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithValidToken_Returns200()
    {
        Authorize();
        var response = await _client.GetAsync("/api/v1/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsers_WithViewerToken_Returns200()
    {
        Authorize(["users.view"]);
        var response = await _client.GetAsync("/api/v1/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/v1/users/{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetUserById_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_WithNonExistentId_Returns404()
    {
        Authorize();
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/v1/users ────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/users",
            new { email = "anon@example.com", firstName = "Anon", lastName = "User", password = "password123" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_WithValidToken_Returns201()
    {
        Authorize();
        var response = await _client.PostAsJsonAsync("/api/v1/users",
            new { email = "new@example.com", firstName = "New", lastName = "User", password = "password123" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateUser_WithViewerToken_Returns403()
    {
        Authorize(["users.view"]);
        var response = await _client.PostAsJsonAsync("/api/v1/users",
            new { email = "viewer-denied@example.com", firstName = "Viewer", lastName = "User", password = "password123" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_Returns409()
    {
        Authorize();
        var body = new { email = "duplicate@example.com", firstName = "Dup", lastName = "User", password = "password123" };
        await _client.PostAsJsonAsync("/api/v1/users", body);

        var second = await _client.PostAsJsonAsync("/api/v1/users", body);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_Returns400()
    {
        Authorize();
        var response = await _client.PostAsJsonAsync("/api/v1/users",
            new { email = "not-an-email", firstName = "Bad", lastName = "User", password = "password123" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/v1/users/{id}/roles ─────────────────────────────────────

    [Fact]
    public async Task AssignRole_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/users/{Guid.NewGuid()}/roles",
            new { roleName = "Editor" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignRole_WithEditorToken_Returns403()
    {
        Authorize(["users.view", "users.create"]);
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/users/{Guid.NewGuid()}/roles",
            new { roleName = "Editor" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_WithAdminToken_Returns204()
    {
        Authorize(AllPermissions);

        // Create a fresh user to assign the role to.
        var createResp = await _client.PostAsJsonAsync("/api/v1/users",
            new { email = "assignrole@example.com", firstName = "Role", lastName = "Test", password = "password123" });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<UserInfo>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        created.Should().NotBeNull();

        var assignResp = await _client.PostAsJsonAsync(
            $"/api/v1/users/{created!.Id}/roles",
            new { roleName = "Editor" });
        assignResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignRole_WithNonExistentUserId_Returns404()
    {
        Authorize(["roles.assign"]);
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/users/{Guid.NewGuid()}/roles",
            new { roleName = "Editor" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void Authorize(IEnumerable<string>? permissions = null) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                TestJwtTokenHelper.GenerateToken(permissions: permissions ?? AllPermissions));

    /// <summary>Minimal DTO for deserialising the CreateUser 201 response.</summary>
    private sealed record UserInfo(Guid Id, string Email, string FirstName, string LastName);
}
