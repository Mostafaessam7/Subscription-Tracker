using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity.Events;

public sealed record UserLockedOut(Guid UserId, DateTimeOffset LockedUntilUtc) : DomainEvent;
