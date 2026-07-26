using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Subscriptions.Events;

public sealed record SubscriptionPaused(Guid SubscriptionId) : DomainEvent;
