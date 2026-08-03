using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Notifications.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;
