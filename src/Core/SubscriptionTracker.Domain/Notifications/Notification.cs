using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Notifications;

public enum NotificationType
{
    RenewalReminder = 0,
    BudgetAlert = 1,
    General = 2,
}

/// <summary>An in-app notification for a single recipient, shown in the frontend's notification bell and pushed
/// live over SignalR when created. Complements (does not replace) email delivery - see IEmailSender and
/// INotificationPublisher, which raises both together.</summary>
public sealed class Notification : AggregateRoot<Guid>
{
    private Notification(
        Guid id, Guid workspaceId, Guid userId, NotificationType type, string title, string message,
        Guid? relatedEntityId, DateTimeOffset createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        RelatedEntityId = relatedEntityId;
        CreatedAtUtc = createdAtUtc;
        IsRead = false;
    }

    private Notification()
    {
    }

    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public Guid? RelatedEntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Notification Create(
        Guid workspaceId, Guid userId, NotificationType type, string title, string message,
        Guid? relatedEntityId, DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), workspaceId, userId, type, title, message, relatedEntityId, createdAtUtc);

    public void MarkAsRead() => IsRead = true;
}
