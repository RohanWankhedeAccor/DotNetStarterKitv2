using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="User"/> entity.
/// Enforces the unique email constraint and the global soft-delete query filter.
/// PasswordHash is explicitly marked as nullable to support OAuth/SSO users who
/// authenticate via Entra ID and never have a local password in the system.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(256);

        // Nullable: SSO/OAuth users authenticated via Entra ID Phase 2
        // will not have a local password hash.
        builder.Property(u => u.PasswordHash)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.Property(u => u.Status)
            .IsRequired();

        // Phase 12: Azure AD integration — nullable for users with no Azure AD account
        builder.Property(u => u.AzureAdObjectId)
            .IsRequired(false)
            .HasMaxLength(128);

        // Authentication source: "Local" or "AzureAd"
        builder.Property(u => u.AuthSource)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Local");

        // Audit fields from BaseEntity
        builder.Property(u => u.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(u => u.ModifiedBy)
            .IsRequired()
            .HasMaxLength(128);

        // Unique index: two users cannot share the same email address.
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // Sparse index on AzureAdObjectId for efficient Azure AD lookups (Phase 12)
        // Sparse: only includes rows where AzureAdObjectId is not null
        builder.HasIndex(u => u.AzureAdObjectId)
            .IsUnique()
            .HasDatabaseName("IX_Users_AzureAdObjectId")
            .HasFilter("[AzureAdObjectId] IS NOT NULL");

        // Global soft-delete filter
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
