using Application.Features.Products.Dtos;
using MediatR;

namespace Application.Features.Products.Commands;

/// <summary>
/// Command to create a new product. Implements <see cref="IRequest{TResponse}"/>
/// with response type <see cref="ProductDto"/>.
/// </summary>
public class CreateProductCommand : IRequest<ProductDto>
{
    /// <summary>Gets or sets the product name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the product description.</summary>
    public required string Description { get; set; }

    /// <summary>Gets or sets the product price.</summary>
    public required decimal Price { get; set; }
}
