using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Models;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Subscriptions.GetSubscriptions;

public sealed record GetSubscriptionsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    Guid? TagId = null,
    SubscriptionStatus? Status = null,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedList<SubscriptionDto>>;
