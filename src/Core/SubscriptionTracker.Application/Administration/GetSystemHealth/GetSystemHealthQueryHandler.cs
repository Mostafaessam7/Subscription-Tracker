using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Administration.GetSystemHealth;

public sealed class GetSystemHealthQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetSystemHealthQuery, SystemHealthDto>
{
    public async Task<Result<SystemHealthDto>> Handle(GetSystemHealthQuery request, CancellationToken cancellationToken)
    {
        // Subscriptions/Budgets are tenant-filtered by ApplicationDbContext's global query filters (see that
        // class) - IgnoreQueryFilters() is required here since a system-wide count is exactly the kind of
        // legitimate cross-tenant read those filters are meant to still allow for admin tooling.
        var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
        var totalWorkspaces = await dbContext.Workspaces.CountAsync(cancellationToken);
        var totalSubscriptions = await dbContext.Subscriptions.IgnoreQueryFilters().CountAsync(cancellationToken);
        var activeSubscriptions = await dbContext.Subscriptions.IgnoreQueryFilters()
            .CountAsync(s => s.Status == SubscriptionStatus.Active, cancellationToken);
        var totalBudgets = await dbContext.Budgets.IgnoreQueryFilters().CountAsync(cancellationToken);

        return Result.Success(new SystemHealthDto(totalUsers, totalWorkspaces, totalSubscriptions, activeSubscriptions, totalBudgets));
    }
}
