using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("WorkspaceMembers");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(m => new { m.WorkspaceId, m.UserId }).IsUnique();
    }
}
