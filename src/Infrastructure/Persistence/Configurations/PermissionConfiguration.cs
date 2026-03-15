using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="Permission"/> entity.
/// Enforces the unique permission name constraint and the global soft-delete query filter.
/// </summary>
internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.ModifiedBy)
            .IsRequired()
            .HasMaxLength(128);

        // Unique index: permission names are globally unique dot-separated keys
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Name");

        // Global soft-delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
