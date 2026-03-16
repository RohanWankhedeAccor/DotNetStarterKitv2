using Application.Features.Products.Dtos;
using MediatR;

namespace Application.Features.Products.Queries;

/// <summary>
/// Query to retrieve a single product by its GUID identifier.
/// Throws <see cref="Domain.Exceptions.NotFoundException"/> when the product does not exist
/// or has been soft-deleted.
/// </summary>
public class GetProductByIdQuery : IRequest<ProductDto>
{
    /// <summary>Gets or sets the unique identifier of the product to retrieve.</summary>
    public required Guid Id { get; set; }
}
