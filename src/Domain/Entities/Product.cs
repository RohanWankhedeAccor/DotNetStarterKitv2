using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a product available in the system.
/// Products have a name, optional description, price, and stock quantity.
/// They support soft-delete (inherited from <see cref="BaseEntity"/>) and an
/// additional <see cref="IsActive"/> flag that controls catalogue visibility
/// without removing the record from the database.
/// </summary>
public sealed class Product : BaseEntity
{
    /// <summary>
    /// Initializes a new <see cref="Product"/>.
    /// </summary>
    /// <param name="name">Display name of the product. Must be non-empty, max 200 characters.</param>
    /// <param name="description">Optional long-form description. Max 2000 characters.</param>
    /// <param name="price">Unit price — must be greater than zero.</param>
    /// <param name="stockQuantity">Units currently in stock — must be non-negative.</param>
    public Product(string name, string? description, decimal price, int stockQuantity)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        IsActive = true;
    }

    /// <summary>EF Core materialisation constructor — not for application use.</summary>
#pragma warning disable CS8618
    private Product() { }
#pragma warning restore CS8618

    /// <summary>Gets the display name of the product.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the optional long-form description of the product.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the unit price. Always greater than zero.</summary>
    public decimal Price { get; private set; }

    /// <summary>Gets the current number of units in stock.</summary>
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this product is visible in the catalogue.
    /// An inactive product is hidden from the storefront but not soft-deleted;
    /// it can be reactivated without re-creating the record.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Updates the product's mutable fields. Called by the UpdateProduct command handler.</summary>
    public void Update(string name, string? description, decimal price, int stockQuantity)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
    }

    /// <summary>Hides this product from the catalogue without deleting it.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Makes this product visible in the catalogue again.</summary>
    public void Reactivate() => IsActive = true;
}
