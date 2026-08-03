using SubscriptionTracker.Domain.Notifications;

namespace SubscriptionTracker.Application.Notifications;

public sealed record NotificationDto(
    Guid Id, NotificationType Type, string Title, string Message, Guid? RelatedEntityId, bool IsRead, DateTimeOffset CreatedAtUtc);
