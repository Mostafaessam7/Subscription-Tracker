using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity.Events;

public sealed record UserRegistered(Guid UserId, string Email) : DomainEvent;
