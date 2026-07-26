namespace SubscriptionTracker.Domain.Common;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; }
    string? CreatedBy { get; }
    DateTimeOffset? LastModifiedAtUtc { get; }
    string? LastModifiedBy { get; }

    void SetCreated(DateTimeOffset occurredOnUtc, string? actor);
    void SetModified(DateTimeOffset occurredOnUtc, string? actor);
}
