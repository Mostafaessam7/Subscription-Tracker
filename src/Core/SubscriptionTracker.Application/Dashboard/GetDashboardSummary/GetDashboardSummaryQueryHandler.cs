using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Budgets;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Dashboard.GetDashboardSummary;

/// <summary>
/// Computes dashboard KPIs server-side over every subscription in the workspace, not just the first page.
/// The frontend previously computed these client-side from GetSubscriptionsQuery capped at pageSize=100
/// (the validator's max), so a workspace with more than 100 subscriptions showed undercounted KPIs — see
/// HANDOVER.md. Reuses BudgetSpendCalculator (already shared between GetBudgetsQuery and BudgetAlertJob) so
/// the "estimated monthly spend" figure is computed the same way everywhere in the app, including converting
/// cross-currency subscriptions into the workspace's default currency via IExchangeRateProvider rather than
/// summing raw amounts across currencies as if they were interchangeable.
/// </summary>
public sealed class GetDashboardSummaryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IExchangeRateProvider exchangeRateProvider,
    TimeProvider timeProvider)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private const int UpcomingRenewalWindowDays = 30;
    private const int UpcomingRenewalListSize = 5;
    private const string FallbackCurrencyCode = "USD";

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var subscriptions = await dbContext.Subscriptions
            .Where(s => s.WorkspaceId == currentUserService.WorkspaceId)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Status,
                s.NextRenewalDate,
                Amount = s.Price.Amount,
                CurrencyCode = s.Price.CurrencyCode,
                Frequency = s.BillingCycle.Frequency,
                s.BillingCycle.CustomIntervalDays,
            })
            .ToListAsync(cancellationToken);

        var targetCurrencyCode = await dbContext.Workspaces
            .Where(w => w.Id == currentUserService.WorkspaceId)
            .Select(w => w.Settings.DefaultCurrencyCode)
            .FirstOrDefaultAsync(cancellationToken) ?? FallbackCurrencyCode;

        // "Today" is UTC, matching every other date-based computation in the backend (RenewalReminderJob,
        // AutoRenewalJob, ExpireSubscriptionsJob all use timeProvider.GetUtcNow() for the same reason) - kept
        // deliberately consistent with those rather than switching to the workspace's configured TimeZoneId,
        // which nothing else in the backend uses for date math either. A user far from UTC may see a renewal
        // due "today" on their own calendar drop in/out of this window near their local midnight; that's the
        // accepted trade-off of one canonical "today" shared by every scheduled job and this query, rather
        // than the dashboard alone drifting from what the actual (UTC-scheduled) reminder emails use.
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var windowEnd = today.AddDays(UpcomingRenewalWindowDays);

        var statusCounts = subscriptions
            .Where(s => s.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial)
            .GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.ToList());

        var billableSubscriptions = statusCounts.Values.SelectMany(g => g).ToList();

        var estimatedMonthlySpend = billableSubscriptions.Sum(s => BudgetSpendCalculator.NormalizeAndConvertToPeriod(
            s.Amount, s.Frequency, s.CustomIntervalDays, BudgetPeriod.Monthly, s.CurrencyCode, targetCurrencyCode, exchangeRateProvider));

        var upcomingRenewals = subscriptions
            .Where(s => s.NextRenewalDate is not null && s.NextRenewalDate >= today && s.NextRenewalDate <= windowEnd)
            .OrderBy(s => s.NextRenewalDate)
            .Take(UpcomingRenewalListSize)
            .Select(s => new UpcomingRenewalDto(
                s.Id, s.Name, s.Amount, s.CurrencyCode, s.NextRenewalDate!.Value, s.NextRenewalDate!.Value.DayNumber - today.DayNumber))
            .ToList();

        var spendByFrequency = billableSubscriptions
            .GroupBy(s => s.Frequency)
            .Select(g => new FrequencyBreakdownDto(g.Key, g.Count()))
            .OrderByDescending(entry => entry.Count)
            .ToList();

        var summary = new DashboardSummaryDto(
            TotalSubscriptions: subscriptions.Count,
            ActiveCount: statusCounts.GetValueOrDefault(SubscriptionStatus.Active)?.Count ?? 0,
            TrialCount: statusCounts.GetValueOrDefault(SubscriptionStatus.Trial)?.Count ?? 0,
            EstimatedMonthlySpend: estimatedMonthlySpend,
            UpcomingRenewals: upcomingRenewals,
            SpendByFrequency: spendByFrequency);

        return Result.Success(summary);
    }
}
