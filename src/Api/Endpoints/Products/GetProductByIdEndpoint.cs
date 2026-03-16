using Application.Features.Products.Dtos;
using Application.Features.Products.Queries;
using MediatR;

namespace Api.Endpoints.Products;

/// <summary>
/// Endpoint for retrieving a single product by GUID.
/// Maps to: GET /api/v1/products/{id}
/// </summary>
public static class GetProductByIdEndpoint
{
    /// <summary>Registers the GET /api/v1/products/{id} endpoint on the application.</summary>
    public static void MapGetProductById(this WebApplication app)
    {
        app.MapGet("/api/v1/products/{id:guid}", GetProductById)
            .WithName("GetProductById")
            .WithOpenApi()
            .WithSummary("Get product by ID")
            .WithDescription("Retrieves a single product by its unique identifier.")
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization("CanViewProducts");
    }

    private static async Task<IResult> GetProductById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductByIdQuery { Id = id }, cancellationToken);
        return Results.Ok(result);
    }
}
