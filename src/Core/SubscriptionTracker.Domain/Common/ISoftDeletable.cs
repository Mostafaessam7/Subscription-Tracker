namespace SubscriptionTracker.Domain.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAtUtc { get; }
    string? DeletedBy { get; }

    void Delete(DateTimeOffset occurredOnUtc, string? actor);
    void Restore();
}
