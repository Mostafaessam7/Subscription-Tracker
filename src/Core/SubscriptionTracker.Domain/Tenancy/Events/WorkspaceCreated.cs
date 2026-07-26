using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Tenancy.Events;

public sealed record WorkspaceCreated(Guid WorkspaceId, Guid OwnerId, string Name) : DomainEvent;
