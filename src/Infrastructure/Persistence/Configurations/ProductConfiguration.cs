using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="Product"/> entity.
/// Enforces decimal precision for the Price column and applies the global
/// soft-delete query filter.
///
/// Price uses <c>decimal(18,2)</c> — 18 total digits with 2 decimal places —
/// which supports values up to 9,999,999,999,999,999.99 while guaranteeing
/// cent-level precision without floating-point rounding errors.
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
            .HasMaxLength(256);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(2000);

        // decimal(18,2): avoids floating-point precision errors for monetary values
        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.ModifiedBy)
            .IsRequired()
            .HasMaxLength(128);

        // Global soft-delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
