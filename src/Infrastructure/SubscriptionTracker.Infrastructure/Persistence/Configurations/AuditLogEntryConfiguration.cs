using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Auditing;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserEmail).HasMaxLength(256);
        builder.Property(e => e.Action).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ErrorCode).HasMaxLength(200);
        builder.Property(e => e.Details).HasMaxLength(4000);

        builder.HasIndex(e => new { e.WorkspaceId, e.OccurredAtUtc });
    }
}
