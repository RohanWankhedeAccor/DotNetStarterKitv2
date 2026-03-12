using Application.Features.Products.Dtos;
using MediatR;

namespace Application.Features.Products.Queries;

/// <summary>
/// Query to retrieve a single product by ID. Returns <see cref="ProductDto"/>.
/// Throws <see cref="Domain.Exceptions.NotFoundException"/> if product does not exist.
/// </summary>
public class GetProductByIdQuery : IRequest<ProductDto>
{
    /// <summary>Gets or sets the product ID to retrieve.</summary>
    public required Guid Id { get; set; }
}
