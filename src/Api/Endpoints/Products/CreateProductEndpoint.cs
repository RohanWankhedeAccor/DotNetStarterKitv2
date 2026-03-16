using Application.Features.Products.Commands;
using Application.Features.Products.Dtos;
using MediatR;

namespace Api.Endpoints.Products;

/// <summary>
/// Endpoint for creating a new product via HTTP POST.
/// Maps to: POST /api/v1/products
/// </summary>
public static class CreateProductEndpoint
{
    /// <summary>Registers the POST /api/v1/products endpoint on the application.</summary>
    public static void MapCreateProduct(this WebApplication app)
    {
        app.MapPost("/api/v1/products", CreateProduct)
            .WithName("CreateProduct")
            .WithOpenApi()
            .WithSummary("Create a new product")
            .WithDescription("Creates a new product with the provided name, description, price, and stock quantity.")
            .Produces<ProductDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("CanCreateProduct");
    }

    private static async Task<IResult> CreateProduct(
        CreateProductDto request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        var result = await mediator.Send(command, cancellationToken);

        return Results.Created($"/api/v1/products/{result.Id}", result);
    }
}
