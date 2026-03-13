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
    /// <param name="app">The web application to register the endpoint on.</param>
    public static void MapCreateProduct(this WebApplication app)
    {
        app.MapPost("/api/v1/products", CreateProduct)
            .WithName("CreateProduct")
            .WithOpenApi()
            .WithSummary("Create a new product")
            .WithDescription("Creates a new product with the provided name, description, and price.")
            .Produces<ProductDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CreateProduct(
        CreateProductDto request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Map DTO to command and dispatch via MediatR
        var command = new CreateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        };

        var result = await mediator.Send(command, cancellationToken);

        // Return 201 Created with the new resource
        return Results.Created($"/api/v1/products/{result.Id}", result);
    }
}
