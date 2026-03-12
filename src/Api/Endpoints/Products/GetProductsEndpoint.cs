using Application.Common.Models;
using Application.Features.Products.Dtos;
using Application.Features.Products.Queries;
using MediatR;

namespace Api.Endpoints.Products;

/// <summary>
/// Endpoint for retrieving a paginated list of products via HTTP GET.
/// Maps to: GET /api/v1/products?pageNumber=1&amp;pageSize=10
/// </summary>
public static class GetProductsEndpoint
{
    public static void MapGetProducts(this WebApplication app)
    {
        app.MapGet("/api/v1/products", GetProducts)
            .WithName("GetProducts")
            .WithOpenApi()
            .WithSummary("Get all products (paginated)")
            .WithDescription("Retrieves a paginated list of products. PageSize is clamped to a maximum of 100.")
            .Produces<PagedResponse<ProductDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetProducts(
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(result);
    }
}
