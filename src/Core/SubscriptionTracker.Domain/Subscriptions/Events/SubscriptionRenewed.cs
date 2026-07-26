using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Subscriptions.Events;

public sealed record SubscriptionRenewed(Guid SubscriptionId, DateOnly NewRenewalDate) : DomainEvent;
