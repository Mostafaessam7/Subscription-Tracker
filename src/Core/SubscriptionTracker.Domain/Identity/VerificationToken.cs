using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity;

public sealed class VerificationToken : Entity<Guid>
{
    private VerificationToken(Guid id, Guid userId, VerificationTokenPurpose purpose, string tokenHash, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private VerificationToken()
    {
    }

    public Guid UserId { get; private set; }
    public VerificationTokenPurpose Purpose { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsConsumed => ConsumedAtUtc is not null;
    public bool IsValid => !IsExpired && !IsConsumed;

    internal static VerificationToken Issue(Guid userId, VerificationTokenPurpose purpose, string tokenHash, DateTimeOffset expiresAtUtc) =>
        new(Guid.NewGuid(), userId, purpose, tokenHash, expiresAtUtc);

    internal void Consume() => ConsumedAtUtc = DateTimeOffset.UtcNow;
}
