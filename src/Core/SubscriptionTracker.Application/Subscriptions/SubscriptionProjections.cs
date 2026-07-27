using System.Linq.Expressions;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions;

internal static class SubscriptionProjections
{
    public static readonly Expression<Func<Subscription, SubscriptionDto>> ToDto = s => new SubscriptionDto(
        s.Id,
        s.Name,
        s.Provider,
        s.LogoUrl,
        s.WebsiteUrl,
        s.Notes,
        s.CategoryId,
        s.PaymentMethodId,
        s.Price.Amount,
        s.Price.CurrencyCode,
        s.BillingCycle.Frequency,
        s.BillingCycle.CustomIntervalDays,
        s.StartDate,
        s.TrialEndDate,
        s.NextRenewalDate,
        s.EndDate,
        s.AutoRenewal,
        s.Status,
        s.TagIds,
        s.SharedUserIds);
}
