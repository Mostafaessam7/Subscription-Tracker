using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.CreatedBy).HasMaxLength(256);
        builder.Property(w => w.LastModifiedBy).HasMaxLength(256);
        builder.Property(w => w.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(w => w.Settings, settings =>
        {
            settings.Property(s => s.DefaultCurrencyCode).HasColumnName("DefaultCurrencyCode").HasMaxLength(3).IsRequired();
            settings.Property(s => s.TimeZoneId).HasColumnName("TimeZoneId").HasMaxLength(100).IsRequired();
            settings.Property(s => s.Locale).HasColumnName("Locale").HasMaxLength(20).IsRequired();
        });

        builder.Navigation(w => w.Settings).IsRequired();

        builder.HasQueryFilter(w => !w.IsDeleted);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasMany(w => w.Members)
            .WithOne()
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
