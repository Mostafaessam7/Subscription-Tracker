using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class EmailInvitationConfiguration : IEntityTypeConfiguration<EmailInvitation>
{
    public void Configure(EntityTypeBuilder<EmailInvitation> builder)
    {
        builder.ToTable("EmailInvitations");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Email)
            .HasConversion(e => e.Value, v => Email.Create(v).Value)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.TokenHash).HasMaxLength(512).IsRequired();

        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => new { i.Email, i.ConsumedAtUtc });
    }
}
