using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Provider).HasMaxLength(200).IsRequired();
        builder.Property(s => s.LogoUrl).HasMaxLength(2048);
        builder.Property(s => s.WebsiteUrl).HasMaxLength(2048);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.LastModifiedBy).HasMaxLength(256);
        builder.Property(s => s.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(s => s.Price, money =>
        {
            money.Property(m => m.Amount).HasColumnName("PriceAmount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.CurrencyCode).HasColumnName("PriceCurrencyCode").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(s => s.Price).IsRequired();

        builder.OwnsOne(s => s.BillingCycle, cycle =>
        {
            cycle.Property(c => c.Frequency).HasColumnName("BillingFrequency").HasConversion<string>().HasMaxLength(20).IsRequired();
            cycle.Property(c => c.CustomIntervalDays).HasColumnName("BillingCustomIntervalDays");
        });
        builder.Navigation(s => s.BillingCycle).IsRequired();

        builder.PrimitiveCollection(s => s.TagIds).HasField("_tagIds").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.PrimitiveCollection(s => s.SharedUserIds).HasField("_sharedUserIds").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.PrimitiveCollection(s => s.ReminderDaysBeforeRenewal)
            .HasField("_reminderDaysBeforeRenewal")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.RenewalHistory)
            .WithOne()
            .HasForeignKey(r => r.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.RenewalHistory).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        builder.HasMany(s => s.Attachments)
            .WithOne()
            .HasForeignKey(a => a.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        builder.HasIndex(s => s.WorkspaceId);
        builder.HasIndex(s => s.OwnerId);
        builder.HasIndex(s => s.CategoryId);
        builder.HasIndex(s => s.NextRenewalDate);
        builder.HasIndex(s => new { s.WorkspaceId, s.Status });

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
