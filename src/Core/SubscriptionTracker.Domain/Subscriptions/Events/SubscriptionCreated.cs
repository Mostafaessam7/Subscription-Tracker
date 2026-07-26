using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Subscriptions.Events;

public sealed record SubscriptionCreated(Guid SubscriptionId, Guid WorkspaceId, Guid OwnerId, string Name) : DomainEvent;
