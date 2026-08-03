using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Notifications.Specifications;

public sealed class UnreadNotificationsByUserIdSpecification : Specification<Notification>
{
    public UnreadNotificationsByUserIdSpecification(Guid userId)
    {
        AddCriteria(n => n.UserId == userId && !n.IsRead);
    }
}
