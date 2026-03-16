using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="Product"/> entity.
/// Enforces column constraints, a performance index on <c>IsActive</c>,
/// and the global soft-delete query filter inherited from <see cref="Domain.Common.BaseEntity"/>.
/// </summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .IsRequired(false)
            .HasMaxLength(2000);

        // decimal(18,2) — standard precision for monetary values.
        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired();

        // Audit fields from BaseEntity
        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.ModifiedBy)
            .IsRequired()
            .HasMaxLength(128);

        // Index for catalogue queries that filter by active status.
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Products_IsActive");

        // Global soft-delete filter — deleted products are excluded from all standard queries.
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
