namespace Application.Features.Products.Dtos;

/// <summary>
/// DTO returned by all product queries and the create/update responses.
/// Contains all public product fields including audit metadata.
/// </summary>
public class ProductDto
{
    /// <summary>Gets or sets the unique product identifier.</summary>
    public required Guid Id { get; set; }

    /// <summary>Gets or sets the display name of the product.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the optional long-form description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the unit price.</summary>
    public required decimal Price { get; set; }

    /// <summary>Gets or sets the number of units currently in stock.</summary>
    public required int StockQuantity { get; set; }

    /// <summary>Gets or sets a value indicating whether the product is active (visible in catalogue).</summary>
    public required bool IsActive { get; set; }

    /// <summary>Gets or sets when the product was created (UTC).</summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the identifier of the user who created the product.</summary>
    public required string CreatedBy { get; set; }

    /// <summary>Gets or sets when the product was last modified (UTC).</summary>
    public required DateTimeOffset ModifiedAt { get; set; }

    /// <summary>Gets or sets the identifier of the user who last modified the product.</summary>
    public required string ModifiedBy { get; set; }
}
