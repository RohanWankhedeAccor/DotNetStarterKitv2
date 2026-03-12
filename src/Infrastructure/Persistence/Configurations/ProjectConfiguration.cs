using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the EF Core mapping for the <see cref="Project"/> entity.
/// Configures the Owner foreign key relationship with <see cref="DeleteBehavior.Restrict"/>
/// to prevent cascade deletes and applies the global soft-delete query filter.
///
/// The Owner navigation is not included automatically by EF Core on standard queries —
/// handlers that need the owner's name or email must use a Select() projection
/// or an explicit Include(p => p.Owner) with AsNoTracking().
/// </summary>
internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(p => p.OwnerId)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.ModifiedBy)
            .IsRequired()
            .HasMaxLength(128);

        // Owner FK: Restrict prevents cascade deletes from User → Project
        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global soft-delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
