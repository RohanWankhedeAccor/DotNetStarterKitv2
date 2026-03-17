using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Models;
using Application.Features.Users.Dtos;
using FluentAssertions;
using Integration.Helpers;

namespace Integration.Api.Users;

/// <summary>
/// Integration tests for filtering and sorting on GET /api/v1/users.
///
/// Uses an isolated SQLite in-memory database per test class (via <see cref="CustomWebApplicationFactory"/>)
/// seeded with alice/bob/carol (all Active).
/// Tests create additional users to exercise filter and sort scenarios.
/// </summary>
public sealed class GetUsersFilterSortTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly string[] AllPermissions =
        ["users.view", "users.create", "users.delete", "roles.assign", "roles.view"];

    public GetUsersFilterSortTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        Authorize();
    }

    // ── Search filter ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_WithSearchByEmail_ReturnsMatchingUsers()
    {
        // "alice" is in the seed data with email "alice@example.com"
        var response = await _client.GetAsync("/api/v1/users?search=alice@example");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await Deserialize(response);
        page.Items.Should().NotBeEmpty();
        page.Items.Should().AllSatisfy(u =>
            u.Email.Should().Contain("alice", Exactly.Once()));
    }

    [Fact]
    public async Task GetUsers_WithSearchByFirstName_ReturnsMatchingUsers()
    {
        var response = await _client.GetAsync("/api/v1/users?search=Bob");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await Deserialize(response);
        page.Items.Should().NotBeEmpty();
        page.Items.Should().AllSatisfy(u =>
            (u.FirstName + u.LastName + u.Email)
                .ToLower().Should().Contain("bob"));
    }

    [Fact]
    public async Task GetUsers_WithSearchNoMatch_ReturnsEmptyPage()
    {
        var response = await _client.GetAsync("/api/v1/users?search=zzz_no_such_user_xyz");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await Deserialize(response);
        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUsers_WithSearchTerm_TotalCountReflectsFilteredSet()
    {
        // TotalCount must be the FILTERED count, not total rows in the table.
        var allResp = await _client.GetAsync("/api/v1/users?pageSize=1");
        var aliceResp = await _client.GetAsync("/api/v1/users?search=alice&pageSize=1");

        var allPage = await Deserialize(allResp);
        var alicePage = await Deserialize(aliceResp);

        alicePage.TotalCount.Should().BeLessThan(allPage.TotalCount,
            "filtering by 'alice' should return fewer results than the full user set");
    }

    // ── Sorting ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_SortByEmailAsc_ReturnsEmailsInAscendingOrder()
    {
        var response = await _client.GetAsync("/api/v1/users?pageSize=100&sortBy=email&sortDescending=false");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await Deserialize(response);
        var emails = page.Items.Select(u => u.Email).ToList();
        emails.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetUsers_SortByEmailDesc_ReturnsEmailsInDescendingOrder()
    {
        var response = await _client.GetAsync("/api/v1/users?pageSize=100&sortBy=email&sortDescending=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await Deserialize(response);
        var emails = page.Items.Select(u => u.Email).ToList();
        emails.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetUsers_SortByFirstNameAsc_ReturnsFirstNamesInAscendingOrder()
    {
        var response = await _client.GetAsync("/api/v1/users?pageSize=100&sortBy=firstName");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await Deserialize(response);
        var names = page.Items.Select(u => u.FirstName).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetUsers_WithUnknownSortBy_FallsBackToEmailAscending()
    {
        var defaultResp = await _client.GetAsync("/api/v1/users?pageSize=100");
        var unknownResp = await _client.GetAsync("/api/v1/users?pageSize=100&sortBy=unknownColumn");

        var defaultPage = await Deserialize(defaultResp);
        var unknownPage = await Deserialize(unknownResp);

        // Both should produce the same ordering (email asc by default)
        defaultPage.Items.Select(u => u.Id)
            .Should().ContainInConsecutiveOrder(unknownPage.Items.Select(u => u.Id));
    }

    // ── Pagination metadata ───────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_PageSizeLargerThanTotal_HasNextPageFalse()
    {
        var response = await _client.GetAsync("/api/v1/users?pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await Deserialize(response);
        page.HasNextPage.Should().BeFalse("all users fit in one page");
        page.HasPreviousPage.Should().BeFalse("first page has no previous page");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void Authorize() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                TestJwtTokenHelper.GenerateToken(permissions: AllPermissions));

    private static async Task<PagedResponse<UserDto>> Deserialize(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResponse<UserDto>>(json, JsonOpts)!;
    }
}
