using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity.Events;

public sealed record UserEmailVerified(Guid UserId) : DomainEvent;
