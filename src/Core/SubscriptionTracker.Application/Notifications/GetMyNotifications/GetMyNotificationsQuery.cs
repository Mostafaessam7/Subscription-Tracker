using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Models;

namespace SubscriptionTracker.Application.Notifications.GetMyNotifications;

public sealed record GetMyNotificationsQuery(int PageNumber = 1, int PageSize = 20) : IQuery<PagedList<NotificationDto>>;
