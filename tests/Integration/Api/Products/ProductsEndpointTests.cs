using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Integration.Helpers;

namespace Integration.Api.Products;

public class ProductsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly string[] AllPermissions =
        ["products.view", "products.create", "products.delete", "users.view", "users.create", "users.delete", "roles.assign", "roles.view"];

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
    public async Task GetProducts_WithViewToken_Returns200()
    {
        Authorize(["products.view"]);
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithoutViewPermission_Returns403()
    {
        Authorize(["products.create"]);
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
        Authorize(AllPermissions);
        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/v1/products ─────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Widget", price = 9.99, stockQuantity = 10 });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithCreatePermission_Returns201()
    {
        Authorize(AllPermissions);
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Widget A", price = 9.99, stockQuantity = 10 });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_WithViewOnlyPermission_Returns403()
    {
        Authorize(["products.view"]);
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Denied Widget", price = 1.00, stockQuantity = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_WithZeroPrice_Returns400()
    {
        Authorize(AllPermissions);
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Bad Widget", price = 0, stockQuantity = 5 });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithEmptyName_Returns400()
    {
        Authorize(AllPermissions);
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "", price = 1.00, stockQuantity = 5 });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProductInBody()
    {
        Authorize(AllPermissions);
        var response = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Inspect Widget", description = "Test", price = 14.99, stockQuantity = 3 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductInfo>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        product.Should().NotBeNull();
        product!.Name.Should().Be("Inspect Widget");
        product.Price.Should().Be(14.99m);
        product.IsActive.Should().BeTrue();
    }

    // ── DELETE /api/v1/products/{id} ──────────────────────────────────────

    [Fact]
    public async Task DeleteProduct_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentId_Returns404()
    {
        Authorize(AllPermissions);
        var response = await _client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_WithViewOnlyPermission_Returns403()
    {
        Authorize(["products.view"]);
        var response = await _client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProduct_WithDeletePermission_Returns204()
    {
        Authorize(AllPermissions);

        // Create a product to delete.
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Delete Me", price = 5.00, stockQuantity = 1 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ProductInfo>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        created.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/products/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_ThenGetById_Returns404()
    {
        Authorize(AllPermissions);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products",
            new { name = "Temp Product", price = 3.00, stockQuantity = 2 });
        var created = await createResponse.Content.ReadFromJsonAsync<ProductInfo>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        await _client.DeleteAsync($"/api/v1/products/{created!.Id}");

        var getResponse = await _client.GetAsync($"/api/v1/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void Authorize(IEnumerable<string>? permissions = null) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                TestJwtTokenHelper.GenerateToken(permissions: permissions ?? AllPermissions));

    private sealed record ProductInfo(Guid Id, string Name, decimal Price, int StockQuantity, bool IsActive);
}
