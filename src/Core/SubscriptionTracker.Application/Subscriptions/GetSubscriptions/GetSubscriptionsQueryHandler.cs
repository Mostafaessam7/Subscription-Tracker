using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Models;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.GetSubscriptions;

public sealed class GetSubscriptionsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetSubscriptionsQuery, PagedList<SubscriptionDto>>
{
    public async Task<Result<PagedList<SubscriptionDto>>> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Subscriptions.Where(s => s.WorkspaceId == currentUserService.WorkspaceId);
        query = SubscriptionFilters.Apply(query, request.SearchTerm, request.CategoryId, request.TagId, request.Status);
        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(SubscriptionProjections.ToDto)
            .ToListAsync(cancellationToken);

        return new PagedList<SubscriptionDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private static IQueryable<Subscription> ApplySorting(IQueryable<Subscription> query, string? sortBy, bool descending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "amount" => descending ? query.OrderByDescending(s => s.Price.Amount) : query.OrderBy(s => s.Price.Amount),
            "status" => descending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            _ => descending
                ? query.OrderByDescending(s => s.NextRenewalDate)
                : query.OrderBy(s => s.NextRenewalDate),
        };
}
