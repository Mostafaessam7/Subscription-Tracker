using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity;

/// <summary>
/// One single-use backup code issued alongside TOTP two-factor enrollment, so a user who loses their
/// authenticator device isn't permanently locked out of an account they can no longer prove ownership of
/// otherwise (the only other way to reach <see cref="User.DisableTwoFactor"/> is a successful TOTP code).
/// Stored hashed (via the same <c>IPasswordHasher</c> used for account passwords), never in plaintext -
/// the raw codes are only ever returned once, at generation time.
/// </summary>
public sealed class TwoFactorRecoveryCode : Entity<Guid>
{
    private TwoFactorRecoveryCode(Guid id, Guid userId, string codeHash)
        : base(id)
    {
        UserId = userId;
        CodeHash = codeHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private TwoFactorRecoveryCode()
    {
    }

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsed => UsedAtUtc is not null;

    internal static TwoFactorRecoveryCode Issue(Guid userId, string codeHash) => new(Guid.NewGuid(), userId, codeHash);

    internal void MarkUsed() => UsedAtUtc = DateTimeOffset.UtcNow;
}
