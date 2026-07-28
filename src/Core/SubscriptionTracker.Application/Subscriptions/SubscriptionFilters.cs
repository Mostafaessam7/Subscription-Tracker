using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Subscriptions;

/// <summary>Shared subscription filtering, so GetSubscriptionsQuery and the report-export queries can't drift
/// out of sync on what "matching the current filters" means.</summary>
internal static class SubscriptionFilters
{
    public static IQueryable<Subscription> Apply(
        IQueryable<Subscription> query, string? searchTerm, Guid? categoryId, Guid? tagId, SubscriptionStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(s => EF.Functions.Like(s.Name, $"%{term}%") || EF.Functions.Like(s.Provider, $"%{term}%"));
        }

        if (categoryId is not null)
        {
            query = query.Where(s => s.CategoryId == categoryId);
        }

        if (tagId is not null)
        {
            query = query.Where(s => s.TagIds.Contains(tagId.Value));
        }

        if (status is not null)
        {
            query = query.Where(s => s.Status == status);
        }

        return query;
    }
}
