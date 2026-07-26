using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Budgets;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Period).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.CreatedBy).HasMaxLength(256);
        builder.Property(b => b.LastModifiedBy).HasMaxLength(256);
        builder.Property(b => b.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(b => b.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.CurrencyCode).HasColumnName("CurrencyCode").HasMaxLength(3).IsRequired();
        });

        builder.Navigation(b => b.Amount).IsRequired();

        builder.HasIndex(b => b.WorkspaceId);

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
