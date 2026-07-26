using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionAttachmentConfiguration : IEntityTypeConfiguration<SubscriptionAttachment>
{
    public void Configure(EntityTypeBuilder<SubscriptionAttachment> builder)
    {
        builder.ToTable("SubscriptionAttachments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.FileName).HasMaxLength(260).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(2048).IsRequired();

        builder.HasIndex(a => a.SubscriptionId);
    }
}
