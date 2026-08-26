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
///
/// Only ever pulls the subscriptions this handler actually needs row-by-row in .NET (the Active/Trial ones,
/// for the currency-conversion sum and the frequency breakdown - conversion isn't SQL-translatable since
/// IExchangeRateProvider's rate table lives in config, not the database). Everything that a plain SQL
/// aggregate can answer - the total count, and the "next 5 upcoming" list - is computed by EF Core as
/// COUNT/ORDER BY/TOP directly against the database instead of first materializing every row, so a workspace
/// with a large history of cancelled/expired subscriptions doesn't pay to load all of them on every dashboard
/// view (originally this loaded literally every subscription in the workspace, unconditionally).
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
        var workspaceSubscriptions = dbContext.Subscriptions.Where(s => s.WorkspaceId == currentUserService.WorkspaceId);

        var totalCount = await workspaceSubscriptions.CountAsync(cancellationToken);

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

        // Only the Active/Trial rows are ever billable, so this is the one set that has to be materialized
        // for the in-memory currency conversion + frequency grouping below - still a real reduction over
        // loading cancelled/expired/paused subscriptions too, which contribute to neither KPI.
        var billableSubscriptions = await workspaceSubscriptions
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
            .Select(s => new
            {
                s.Status,
                Amount = s.Price.Amount,
                CurrencyCode = s.Price.CurrencyCode,
                Frequency = s.BillingCycle.Frequency,
                s.BillingCycle.CustomIntervalDays,
            })
            .ToListAsync(cancellationToken);

        var estimatedMonthlySpend = billableSubscriptions.Sum(s => BudgetSpendCalculator.NormalizeAndConvertToPeriod(
            s.Amount, s.Frequency, s.CustomIntervalDays, BudgetPeriod.Monthly, s.CurrencyCode, targetCurrencyCode, exchangeRateProvider));

        var upcomingRenewals = await workspaceSubscriptions
            .Where(s => (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
                && s.NextRenewalDate != null && s.NextRenewalDate >= today && s.NextRenewalDate <= windowEnd)
            .OrderBy(s => s.NextRenewalDate)
            .Take(UpcomingRenewalListSize)
            .Select(s => new { s.Id, s.Name, Amount = s.Price.Amount, CurrencyCode = s.Price.CurrencyCode, s.NextRenewalDate })
            .ToListAsync(cancellationToken);

        var spendByFrequency = billableSubscriptions
            .GroupBy(s => s.Frequency)
            .Select(g => new FrequencyBreakdownDto(g.Key, g.Count()))
            .OrderByDescending(entry => entry.Count)
            .ToList();

        var summary = new DashboardSummaryDto(
            TotalSubscriptions: totalCount,
            ActiveCount: billableSubscriptions.Count(s => s.Status == SubscriptionStatus.Active),
            TrialCount: billableSubscriptions.Count(s => s.Status == SubscriptionStatus.Trial),
            EstimatedMonthlySpend: estimatedMonthlySpend,
            UpcomingRenewals: upcomingRenewals
                .Select(s => new UpcomingRenewalDto(s.Id, s.Name, s.Amount, s.CurrencyCode, s.NextRenewalDate!.Value, s.NextRenewalDate!.Value.DayNumber - today.DayNumber))
                .ToList(),
            SpendByFrequency: spendByFrequency);

        return Result.Success(summary);
    }
}
