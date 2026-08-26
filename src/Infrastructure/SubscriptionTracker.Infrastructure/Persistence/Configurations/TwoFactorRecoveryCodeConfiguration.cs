using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Infrastructure.Persistence.Configurations;

internal sealed class TwoFactorRecoveryCodeConfiguration : IEntityTypeConfiguration<TwoFactorRecoveryCode>
{
    public void Configure(EntityTypeBuilder<TwoFactorRecoveryCode> builder)
    {
        builder.ToTable("TwoFactorRecoveryCodes");

        builder.HasKey(rc => rc.Id);
        builder.Property(rc => rc.Id).ValueGeneratedNever();

        // Hashed (PBKDF2, same as User.PasswordHash) - matches that column's width, not the raw code's length.
        builder.Property(rc => rc.CodeHash).HasMaxLength(512).IsRequired();

        builder.HasIndex(rc => rc.UserId);
    }
}
