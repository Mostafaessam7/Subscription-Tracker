using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Catalog;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Label).HasMaxLength(100).IsRequired();
        builder.Property(p => p.MaskedDetails).HasMaxLength(100);
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.LastModifiedBy).HasMaxLength(256);
        builder.Property(p => p.DeletedBy).HasMaxLength(256);

        builder.HasIndex(p => p.WorkspaceId);

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
