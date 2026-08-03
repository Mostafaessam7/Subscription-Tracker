using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Notifications.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetUnreadNotificationCountQuery, int>
{
    public async Task<Result<int>> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var count = await dbContext.Notifications
            .CountAsync(n => n.UserId == currentUserService.UserId && !n.IsRead, cancellationToken);

        return Result.Success(count);
    }
}
