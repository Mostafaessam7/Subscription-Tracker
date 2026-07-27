using MediatR;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Adapts a domain event (which deliberately has no dependency on MediatR) into a MediatR notification so it
/// can be published through <see cref="IPublisher"/>. Handlers implement INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;.
/// </summary>
public sealed class DomainEventNotification<TDomainEvent>(TDomainEvent domainEvent) : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; } = domainEvent;
}
