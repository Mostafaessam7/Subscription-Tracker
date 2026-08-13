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
/// the "estimated monthly spend" figure is computed the same way everywhere in the app.
/// </summary>
public sealed class GetDashboardSummaryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private const int UpcomingRenewalWindowDays = 30;
    private const int UpcomingRenewalListSize = 5;

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

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var windowEnd = today.AddDays(UpcomingRenewalWindowDays);

        var billableSubscriptions = subscriptions
            .Where(s => s.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial)
            .ToList();

        var estimatedMonthlySpend = billableSubscriptions.Sum(s =>
            BudgetSpendCalculator.NormalizeToPeriod(s.Amount, s.Frequency, s.CustomIntervalDays, BudgetPeriod.Monthly));

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
            ActiveCount: subscriptions.Count(s => s.Status == SubscriptionStatus.Active),
            TrialCount: subscriptions.Count(s => s.Status == SubscriptionStatus.Trial),
            EstimatedMonthlySpend: estimatedMonthlySpend,
            UpcomingRenewals: upcomingRenewals,
            SpendByFrequency: spendByFrequency);

        return Result.Success(summary);
    }
}
