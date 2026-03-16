using Application.Features.Products.Dtos;
using MediatR;

namespace Application.Features.Products.Commands;

/// <summary>
/// Command to create a new product. Implements <see cref="IRequest{TResponse}"/>
/// with response type <see cref="ProductDto"/>.
/// </summary>
public class CreateProductCommand : IRequest<ProductDto>
{
    /// <summary>Gets or sets the display name of the product.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets an optional long-form description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the unit price. Must be greater than zero.</summary>
    public required decimal Price { get; set; }

    /// <summary>Gets or sets the initial stock quantity. Must be non-negative.</summary>
    public required int StockQuantity { get; set; }
}
