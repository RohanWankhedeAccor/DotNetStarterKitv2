namespace Application.Features.Products.Dtos;

/// <summary>
/// Input DTO for the POST /api/v1/products endpoint.
/// Mapped to <see cref="Commands.CreateProductCommand"/> by the endpoint handler.
/// </summary>
public class CreateProductDto
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
