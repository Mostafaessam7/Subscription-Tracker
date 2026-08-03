using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Notifications;
using SubscriptionTracker.Domain.Notifications.Specifications;

namespace SubscriptionTracker.Application.Notifications.MarkAllNotificationsAsRead;

public sealed class MarkAllNotificationsAsReadCommandHandler(
    IRepository<Notification, Guid> notificationRepository, ICurrentUserService currentUserService)
    : ICommandHandler<MarkAllNotificationsAsReadCommand>
{
    public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Success();
        }

        var unread = await notificationRepository.ListAsync(
            new UnreadNotificationsByUserIdSpecification(currentUserService.UserId.Value), cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkAsRead();
            notificationRepository.Update(notification);
        }

        return Result.Success();
    }
}
