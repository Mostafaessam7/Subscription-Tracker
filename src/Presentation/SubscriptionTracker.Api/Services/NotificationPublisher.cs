using Microsoft.AspNetCore.SignalR;
using SubscriptionTracker.Api.Hubs;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Notifications;
using SubscriptionTracker.Domain.Notifications;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Api.Services;

/// <summary>
/// Lives in the API layer (not Infrastructure) purely because SignalR's server types require the ASP.NET Core
/// web SDK - same reasoning as ICurrentUserService. Writes the Notification directly via ApplicationDbContext
/// (bypassing the command pipeline, since background jobs and other server-side callers publish notifications as
/// a side effect of their own work, not as a user-initiated command) and pushes it live if the recipient is
/// connected; SaveChangesAsync commits immediately since callers may not go through UnitOfWorkBehavior at all
/// (Quartz jobs never do).
/// </summary>
public sealed class NotificationPublisher(ApplicationDbContext dbContext, IHubContext<NotificationsHub> hubContext) : INotificationPublisher
{
    public async Task PublishAsync(
        Guid workspaceId, Guid userId, NotificationType type, string title, string message,
        Guid? relatedEntityId, CancellationToken cancellationToken)
    {
        var notification = Notification.Create(workspaceId, userId, type, title, message, relatedEntityId, DateTimeOffset.UtcNow);

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new NotificationDto(
            notification.Id, notification.Type, notification.Title, notification.Message,
            notification.RelatedEntityId, notification.IsRead, notification.CreatedAtUtc);

        await hubContext.Clients.Group(NotificationsHub.GroupName(userId.ToString()))
            .SendAsync("ReceiveNotification", dto, cancellationToken);
    }
}
