using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="Role"/> entity.
/// Enforces the unique role name constraint and the global soft-delete query filter.
/// </summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(r => r.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.ModifiedBy)
            .IsRequired()
            .HasMaxLength(128);

        // Unique index: role names are globally unique identifiers
        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        // Global soft-delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
