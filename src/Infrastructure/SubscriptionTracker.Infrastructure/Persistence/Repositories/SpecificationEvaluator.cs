using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Infrastructure.Persistence.Repositories;

internal static class SpecificationEvaluator
{
    public static IQueryable<T> Apply<T>(IQueryable<T> inputQuery, Specification<T> specification)
        where T : class
    {
        var query = inputQuery;

        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy.Count > 0)
        {
            var (firstKeySelector, firstDescending) = specification.OrderBy[0];
            var orderedQuery = firstDescending
                ? query.OrderByDescending(firstKeySelector)
                : query.OrderBy(firstKeySelector);

            for (var i = 1; i < specification.OrderBy.Count; i++)
            {
                var (keySelector, descending) = specification.OrderBy[i];
                orderedQuery = descending ? orderedQuery.ThenByDescending(keySelector) : orderedQuery.ThenBy(keySelector);
            }

            query = orderedQuery;
        }

        if (specification.Skip is not null)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take is not null)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
