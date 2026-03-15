using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="RolePermission"/> junction entity.
/// Enforces the composite unique constraint on (RoleId, PermissionId) and configures
/// both foreign keys with <see cref="DeleteBehavior.Restrict"/> to prevent cascade deletes.
/// </summary>
internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.RoleId)
            .IsRequired();

        builder.Property(rp => rp.PermissionId)
            .IsRequired();

        builder.Property(rp => rp.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(rp => rp.ModifiedBy)
            .IsRequired()
            .HasMaxLength(128);

        // Role FK: restrict so that deleting a Role does not cascade to RolePermissions
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Permission FK: restrict so that deleting a Permission does not cascade
        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite unique index: a permission can only be granted to a role once
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_RolePermissions_RoleId_PermissionId");

        // Global soft-delete filter
        builder.HasQueryFilter(rp => !rp.IsDeleted);
    }
}
