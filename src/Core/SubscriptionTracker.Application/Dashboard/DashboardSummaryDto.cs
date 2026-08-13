using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Dashboard;

public sealed record DashboardSummaryDto(
    int TotalSubscriptions,
    int ActiveCount,
    int TrialCount,
    decimal EstimatedMonthlySpend,
    IReadOnlyCollection<UpcomingRenewalDto> UpcomingRenewals,
    IReadOnlyCollection<FrequencyBreakdownDto> SpendByFrequency);

public sealed record UpcomingRenewalDto(
    Guid SubscriptionId,
    string Name,
    decimal Amount,
    string CurrencyCode,
    DateOnly NextRenewalDate,
    int DaysUntil);

public sealed record FrequencyBreakdownDto(BillingFrequency Frequency, int Count);
