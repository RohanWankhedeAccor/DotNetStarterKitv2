using Domain.Enums;

namespace Application.Features.Products.Dtos;

/// <summary>
/// DTO for returning product data. Returned by all product queries.
/// </summary>
public class ProductDto
{
    /// <summary>Gets or sets the unique product identifier.</summary>
    public required Guid Id { get; set; }

    /// <summary>Gets or sets the product name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the product description.</summary>
    public required string Description { get; set; }

    /// <summary>Gets or sets the product price.</summary>
    public required decimal Price { get; set; }

    /// <summary>Gets or sets the product's current status.</summary>
    public required ProductStatus Status { get; set; }

    /// <summary>Gets or sets when the product was created (UTC).</summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets who created the product (user ID).</summary>
    public required string CreatedBy { get; set; }

    /// <summary>Gets or sets when the product was last modified (UTC).</summary>
    public required DateTimeOffset ModifiedAt { get; set; }

    /// <summary>Gets or sets who last modified the product (user ID).</summary>
    public required string ModifiedBy { get; set; }
}
