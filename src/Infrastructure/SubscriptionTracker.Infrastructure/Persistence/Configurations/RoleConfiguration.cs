using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.LastModifiedBy).HasMaxLength(256);
        builder.Property(r => r.DeletedBy).HasMaxLength(256);

        builder.HasIndex(r => new { r.WorkspaceId, r.Name }).IsUnique();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.PrimitiveCollection(r => r.PermissionCodes)
            .HasField("_permissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .ElementType(e => e.HasMaxLength(100));
    }
}
