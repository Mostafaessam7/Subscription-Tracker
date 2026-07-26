using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Subscriptions.Events;

public sealed record SubscriptionResumed(Guid SubscriptionId) : DomainEvent;
