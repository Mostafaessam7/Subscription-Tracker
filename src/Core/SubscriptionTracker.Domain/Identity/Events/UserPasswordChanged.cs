using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity.Events;

public sealed record UserPasswordChanged(Guid UserId) : DomainEvent;
