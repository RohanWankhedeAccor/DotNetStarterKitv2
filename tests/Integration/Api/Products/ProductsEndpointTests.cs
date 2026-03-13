using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Integration.Helpers;

namespace Integration.Api.Products;

public class ProductsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/v1/products ──────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_WithValidToken_Returns200()
    {
        Authorize();
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/v1/products/{id} ─────────────────────────────────────────

    [Fact]
    public async Task GetProductById_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductById_WithNonExistentId_Returns404()
    {
        Authorize();
        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/v1/products ─────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Widget", description = "A widget", price = 9.99 });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithValidToken_Returns201()
    {
        Authorize();
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Widget", description = "A widget", price = 9.99 });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_WithZeroPrice_Returns400()
    {
        Authorize();
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Free", description = "Free thing", price = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithEmptyName_Returns400()
    {
        Authorize();
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "", description = "No name", price = 5.00 });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private void Authorize() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenHelper.GenerateToken());
}
