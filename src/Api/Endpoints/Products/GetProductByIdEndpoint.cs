using Application.Features.Products.Dtos;
using Application.Features.Products.Queries;
using MediatR;

namespace Api.Endpoints.Products;

/// <summary>
/// Endpoint for retrieving a single product by ID via HTTP GET.
/// Maps to: GET /api/v1/products/{id}
/// </summary>
public static class GetProductByIdEndpoint
{
    /// <summary>Registers the GET /api/v1/products/{id} endpoint on the application.</summary>
    /// <param name="app">The web application to register the endpoint on.</param>
    public static void MapGetProductById(this WebApplication app)
    {
        app.MapGet("/api/v1/products/{id:guid}", GetProductById)
            .WithName("GetProductById")
            .WithOpenApi()
            .WithSummary("Get a product by ID")
            .WithDescription("Retrieves a single product by its unique identifier.")
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetProductById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery { Id = id };
        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(result);
    }
}
