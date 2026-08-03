using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Notifications;

namespace SubscriptionTracker.Application.Notifications.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommandHandler(
    IRepository<Notification, Guid> notificationRepository, ICurrentUserService currentUserService)
    : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null || notification.UserId != currentUserService.UserId)
        {
            return Result.Failure(Error.NotFound("MarkNotificationAsRead.NotFound", "Notification was not found."));
        }

        notification.MarkAsRead();
        notificationRepository.Update(notification);

        return Result.Success();
    }
}
