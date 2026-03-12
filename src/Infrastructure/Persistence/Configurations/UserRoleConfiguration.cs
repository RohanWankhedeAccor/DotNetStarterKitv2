using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="UserRole"/> junction entity.
/// Enforces the composite unique constraint on (UserId, RoleId) and configures
/// both foreign keys with <see cref="DeleteBehavior.Restrict"/> to prevent
/// cascade deletes — soft-delete is the only permitted deletion mechanism.
/// </summary>
internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.UserId)
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .IsRequired();

        builder.Property(ur => ur.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(ur => ur.ModifiedBy)
            .IsRequired()
            .HasMaxLength(128);

        // User FK: Restrict prevents cascade delete from User → UserRole
        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Role FK
        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite unique index
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoles_UserId_RoleId");

        // Global soft-delete filter
        builder.HasQueryFilter(ur => !ur.IsDeleted);
    }
}
