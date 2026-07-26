using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Catalog;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Name).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Color).HasMaxLength(20);
        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.LastModifiedBy).HasMaxLength(256);
        builder.Property(t => t.DeletedBy).HasMaxLength(256);

        builder.HasIndex(t => new { t.WorkspaceId, t.Name }).IsUnique();

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
