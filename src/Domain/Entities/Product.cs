using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a product in the system.
/// Products have a lifecycle (Draft → Active → Discontinued) and are used to populate
/// the inventory or catalog that teams select from when creating projects.
///
/// Once a product reaches the Discontinued state, it can no longer be referenced in new
/// projects, though historical references are preserved for audit accuracy.
/// </summary>
public sealed class Product : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Product"/> class with the specified details.
    /// The new product is created in Draft status and must be activated before it can be
    /// referenced in projects or orders.
    /// </summary>
    /// <param name="name">The product name or identifier (e.g., "Premium Support", "Data Package").</param>
    /// <param name="description">A detailed description of the product's features and use cases.</param>
    /// <param name="price">The price of the product in the system's default currency. Must be non-negative.</param>
    public Product(string name, string description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
        Status = ProductStatus.Draft;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Product"/> class.
    /// This parameterless constructor is called exclusively by EF Core when materializing
    /// product records from the database via reflection. Direct invocation from application code is not possible.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Product()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets the product's name or unique identifier.
    /// Examples: "Premium Support", "Professional Data Package", "Annual License".
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets a detailed description of the product's purpose, features, and recommended use cases.
    /// This text is displayed to end users when selecting products.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the price of this product in the system's default currency.
    /// The Infrastructure layer stores this as decimal(18,2) to ensure cent-level precision.
    /// Prices must be non-negative; negative prices are prevented by validation in the
    /// Application layer's CreateProductValidator.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Gets or sets the current lifecycle status of the product.
    /// Initial status is Draft; transitions to Active after configuration is complete.
    /// Once Discontinued, the product cannot be referenced in new projects.
    /// Possible values: Draft, Active, Discontinued.
    /// </summary>
    public ProductStatus Status { get; private set; }

    /// <summary>
    /// Activates the product, changing status from Draft to Active.
    /// Once active, the product becomes available for selection in projects and orders.
    /// </summary>
    public void Activate()
    {
        Status = ProductStatus.Active;
    }

    /// <summary>
    /// Discontinues the product, changing status to Discontinued.
    /// Discontinued products can no longer be referenced in new projects, though
    /// historical references in completed projects are preserved for audit accuracy.
    /// Discontinuation is permanent — there is no reactivation method.
    /// </summary>
    public void Discontinue()
    {
        Status = ProductStatus.Discontinued;
    }
}
