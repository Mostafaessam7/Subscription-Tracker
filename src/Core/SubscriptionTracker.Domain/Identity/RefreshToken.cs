using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity;

public sealed class RefreshToken : Entity<Guid>
{
    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, string? createdByIp)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        CreatedByIp = createdByIp;
    }

    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsExpired && !IsRevoked;

    internal static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, string? createdByIp) =>
        new(Guid.NewGuid(), userId, tokenHash, expiresAtUtc, createdByIp);

    internal void Revoke(string? revokedByIp, string? replacedByTokenHash = null)
    {
        RevokedAtUtc = DateTimeOffset.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
