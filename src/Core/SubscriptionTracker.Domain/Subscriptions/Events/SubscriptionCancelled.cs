using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Subscriptions.Events;

public sealed record SubscriptionCancelled(Guid SubscriptionId, string? Reason) : DomainEvent;
