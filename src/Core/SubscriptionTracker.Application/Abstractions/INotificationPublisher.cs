using SubscriptionTracker.Domain.Notifications;

namespace SubscriptionTracker.Application.Abstractions;

/// <summary>
/// Creates an in-app Notification and pushes it live to the recipient (e.g. over SignalR) if they're connected.
/// Distinct from IEmailSender - callers that want both channels (background jobs currently do) call both.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(
        Guid workspaceId, Guid userId, NotificationType type, string title, string message,
        Guid? relatedEntityId, CancellationToken cancellationToken);
}
