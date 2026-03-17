using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="AuditLog"/> entity.
///
/// <para>
/// AuditLog has no global soft-delete filter — audit records are permanent by design.
/// JSON payload columns use the database's maximum text type to accommodate large snapshots.
/// </para>
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TableName)
            .IsRequired()
            .HasMaxLength(128);

        // Guid formatted as "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" = 36 chars
        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(36);

        // Store the enum as an integer column for compact storage and fast range queries.
        builder.Property(a => a.Action)
            .IsRequired()
            .HasConversion<int>();

        // 450 chars matches ASP.NET Identity's default userId column length.
        builder.Property(a => a.ChangedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.ChangedAt)
            .IsRequired();

        // JSON snapshots can be arbitrarily large — use the unlimited text type.
        builder.Property(a => a.OldValues)
            .IsRequired(false);

        builder.Property(a => a.NewValues)
            .IsRequired(false);

        // ── Indexes ──────────────────────────────────────────────────────────────────

        // Supports "show me all history for entity X in table Y" queries.
        builder.HasIndex(a => new { a.TableName, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_TableName_EntityId");

        // Supports "show me all changes made by user X" queries.
        builder.HasIndex(a => a.ChangedBy)
            .HasDatabaseName("IX_AuditLogs_ChangedBy");

        // Supports chronological range queries (e.g. last 24 hours of changes).
        builder.HasIndex(a => a.ChangedAt)
            .HasDatabaseName("IX_AuditLogs_ChangedAt");
    }
}
