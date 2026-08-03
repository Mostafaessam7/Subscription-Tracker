using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Models;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Notifications.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetMyNotificationsQuery, PagedList<NotificationDto>>
{
    public async Task<Result<PagedList<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications
            .Where(n => n.UserId == currentUserService.UserId)
            .OrderByDescending(n => n.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Message, n.RelatedEntityId, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedList<NotificationDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
