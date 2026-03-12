namespace Application.Features.Products.Dtos;

/// <summary>
/// DTO for creating a new product. Used as input to <see cref="Commands.CreateProductCommand"/>.
/// </summary>
public class CreateProductDto
{
    /// <summary>Gets or sets the product name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the product description.</summary>
    public required string Description { get; set; }

    /// <summary>Gets or sets the product price.</summary>
    public required decimal Price { get; set; }
}
