using Application.Common.Models;
using Application.Features.Products.Dtos;
using Application.Features.Products.Queries;
using MediatR;

namespace Api.Endpoints.Products;

/// <summary>
/// Endpoint for retrieving a paginated, filtered list of products.
/// Maps to: GET /api/v1/products
/// </summary>
public static class GetProductsEndpoint
{
    /// <summary>Registers the GET /api/v1/products endpoint on the application.</summary>
    public static void MapGetProducts(this WebApplication app)
    {
        app.MapGet("/api/v1/products", GetProducts)
            .WithName("GetProducts")
            .WithOpenApi()
            .WithSummary("Get all products")
            .WithDescription("Retrieves a paginated, filtered, and sorted list of products.")
            .Produces<PagedResponse<ProductDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization("CanViewProducts");
    }

    private static async Task<IResult> GetProducts(
        IMediator mediator,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10,
        string? search = null,
        bool? isActive = null,
        string? sortBy = null,
        bool sortDescending = false)
    {
        var query = new GetProductsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            IsActive = isActive,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }
}
