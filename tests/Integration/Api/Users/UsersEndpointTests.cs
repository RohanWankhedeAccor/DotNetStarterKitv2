using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Integration.Helpers;

namespace Integration.Api.Users;

public class UsersEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

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
            new { email = "anon@example.com", fullName = "Anon", password = "password123" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_WithValidToken_Returns201()
    {
        Authorize();
        var response = await _client.PostAsJsonAsync("/api/v1/users",
            new { email = "new@example.com", fullName = "New User", password = "password123" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_Returns409()
    {
        Authorize();
        var body = new { email = "duplicate@example.com", fullName = "Dup User", password = "password123" };
        await _client.PostAsJsonAsync("/api/v1/users", body);

        var second = await _client.PostAsJsonAsync("/api/v1/users", body);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_Returns400()
    {
        Authorize();
        var response = await _client.PostAsJsonAsync("/api/v1/users",
            new { email = "not-an-email", fullName = "Bad", password = "password123" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private void Authorize() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenHelper.GenerateToken());
}
