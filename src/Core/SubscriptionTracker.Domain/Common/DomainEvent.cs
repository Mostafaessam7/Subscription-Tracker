namespace SubscriptionTracker.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
