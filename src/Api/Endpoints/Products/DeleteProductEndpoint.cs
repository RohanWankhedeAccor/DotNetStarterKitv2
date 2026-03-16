using Api.Extensions;
using Application.Features.Products.Commands;
using MediatR;

namespace Api.Endpoints.Products;

/// <summary>
/// Endpoint for soft-deleting a product by GUID.
/// Maps to: DELETE /api/v1/products/{id}
/// </summary>
public static class DeleteProductEndpoint
{
    /// <summary>Registers the DELETE /api/v1/products/{id} endpoint on the application.</summary>
    public static void MapDeleteProduct(this WebApplication app)
    {
        app.MapDelete("/api/v1/products/{id:guid}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithOpenApi()
            .WithSummary("Delete a product")
            .WithDescription("Soft-deletes a product by its unique identifier. The product is hidden from all queries but not removed from the database.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("CanDeleteProduct");
    }

    private static async Task<IResult> DeleteProduct(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteProductCommand { Id = id }, cancellationToken);
        return result.ToApiResult();
    }
}
