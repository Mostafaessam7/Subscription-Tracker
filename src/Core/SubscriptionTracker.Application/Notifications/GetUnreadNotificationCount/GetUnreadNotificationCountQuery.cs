using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Notifications.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery : IQuery<int>;
