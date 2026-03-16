using MediatR;

namespace Application.Features.Products.Commands;

/// <summary>
/// Command to soft-delete a product. Implements <see cref="IRequest"/> (no return value).
/// Throws <see cref="Domain.Exceptions.NotFoundException"/> if the product does not exist.
/// </summary>
public class DeleteProductCommand : IRequest
{
    /// <summary>Gets or sets the ID of the product to delete.</summary>
    public required Guid Id { get; set; }
}
