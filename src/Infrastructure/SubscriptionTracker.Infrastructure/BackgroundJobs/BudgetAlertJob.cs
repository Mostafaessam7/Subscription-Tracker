using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs daily and emails the workspace owner when estimated recurring spend (subscription costs normalized to the
/// budget's period) crosses a budget's alert threshold. Only subscriptions billed in the budget's own currency are
/// counted - no currency conversion is performed.
/// </summary>
[DisallowConcurrentExecution]
public sealed class BudgetAlertJob(IApplicationDbContext dbContext, IEmailSender emailSender, ILogger<BudgetAlertJob> logger) : IJob
{
    private const double AverageDaysPerMonth = 30.4368;
    private const double AverageDaysPerYear = 365.25;

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        var budgets = await dbContext.Budgets.ToListAsync(cancellationToken);
        if (budgets.Count == 0)
        {
            return;
        }

        var subscriptions = await dbContext.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
            .Select(s => new
            {
                s.WorkspaceId,
                s.CategoryId,
                Amount = s.Price.Amount,
                CurrencyCode = s.Price.CurrencyCode,
                Frequency = s.BillingCycle.Frequency,
                s.BillingCycle.CustomIntervalDays,
            })
            .ToListAsync(cancellationToken);

        var workspaceOwners = await dbContext.Workspaces
            .Select(w => new { w.Id, w.OwnerId })
            .ToDictionaryAsync(w => w.Id, w => w.OwnerId, cancellationToken);

        var alertsSent = 0;

        foreach (var budget in budgets)
        {
            var spent = subscriptions
                .Where(s => s.WorkspaceId == budget.WorkspaceId && s.CurrencyCode == budget.Amount.CurrencyCode)
                .Where(s => budget.CategoryId is null || s.CategoryId == budget.CategoryId)
                .Sum(s => NormalizeToPeriod(s.Amount, s.Frequency, s.CustomIntervalDays, budget.Period));

            var spentMoney = Money.Create(spent, budget.Amount.CurrencyCode);
            if (spentMoney.IsFailure || !budget.HasExceededThreshold(spentMoney.Value))
            {
                continue;
            }

            if (!workspaceOwners.TryGetValue(budget.WorkspaceId, out var ownerId))
            {
                continue;
            }

            var owner = await dbContext.Users
                .Where(u => u.Id == ownerId)
                .Select(u => new { Email = u.Email.Value, u.FirstName })
                .FirstOrDefaultAsync(cancellationToken);

            if (owner is null)
            {
                continue;
            }

            await emailSender.SendBudgetOverspendAlertAsync(
                owner.Email, owner.FirstName, budget.Name, spent, budget.Amount.Amount, budget.Amount.CurrencyCode, cancellationToken);
            alertsSent++;
        }

        if (alertsSent > 0)
        {
            logger.LogInformation("Sent {Count} budget overspend alert(s)", alertsSent);
        }
    }

    private static decimal NormalizeToPeriod(decimal amount, BillingFrequency frequency, int? customIntervalDays, BudgetPeriod period)
    {
        var occurrencesPerYear = frequency switch
        {
            BillingFrequency.Weekly => 52.1786,
            BillingFrequency.Monthly => 12.0,
            BillingFrequency.Quarterly => 4.0,
            BillingFrequency.Yearly => 1.0,
            BillingFrequency.Custom when customIntervalDays is > 0 => AverageDaysPerYear / customIntervalDays.Value,
            _ => 0.0,
        };

        var periodsPerYear = period == BudgetPeriod.Yearly ? 1.0 : AverageDaysPerYear / AverageDaysPerMonth;

        return (decimal)((double)amount * occurrencesPerYear / periodsPerYear);
    }
}
