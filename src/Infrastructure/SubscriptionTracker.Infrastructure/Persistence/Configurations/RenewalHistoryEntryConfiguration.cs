using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class RenewalHistoryEntryConfiguration : IEntityTypeConfiguration<RenewalHistoryEntry>
{
    public void Configure(EntityTypeBuilder<RenewalHistoryEntry> builder)
    {
        builder.ToTable("SubscriptionRenewalHistory");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.OwnsOne(r => r.AmountCharged, money =>
        {
            money.Property(m => m.Amount).HasColumnName("AmountCharged").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.CurrencyCode).HasColumnName("AmountChargedCurrencyCode").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(r => r.AmountCharged).IsRequired();

        builder.HasIndex(r => r.SubscriptionId);
    }
}
