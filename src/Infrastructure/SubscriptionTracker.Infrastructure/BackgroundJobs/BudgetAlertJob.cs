using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Budgets;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Notifications;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs daily and emails the workspace owner when estimated recurring spend (subscription costs normalized to the
/// budget's period) crosses a budget's alert threshold. Subscriptions in a different currency than the budget are
/// converted via IExchangeRateProvider (same static rate table GetBudgetsQuery uses, so the UI and this alert can't
/// disagree); a currency with no known rate contributes 0, same as being excluded outright.
/// </summary>
[DisallowConcurrentExecution]
public sealed class BudgetAlertJob(
    IApplicationDbContext dbContext, IEmailSender emailSender, INotificationPublisher notificationPublisher,
    IExchangeRateProvider exchangeRateProvider, ILogger<BudgetAlertJob> logger) : IJob
{
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
                .Where(s => s.WorkspaceId == budget.WorkspaceId)
                .Where(s => budget.CategoryId is null || s.CategoryId == budget.CategoryId)
                .Sum(s =>
                {
                    var rate = s.CurrencyCode == budget.Amount.CurrencyCode
                        ? 1m
                        : exchangeRateProvider.GetRate(s.CurrencyCode, budget.Amount.CurrencyCode) ?? 0m;
                    return rate * BudgetSpendCalculator.NormalizeToPeriod(s.Amount, s.Frequency, s.CustomIntervalDays, budget.Period);
                });

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

            await notificationPublisher.PublishAsync(
                budget.WorkspaceId, ownerId, NotificationType.BudgetAlert,
                "Budget threshold exceeded",
                $"\"{budget.Name}\" is at {spent:0.##} of {budget.Amount.Amount:0.##} {budget.Amount.CurrencyCode}.",
                budget.Id, cancellationToken);

            alertsSent++;
        }

        if (alertsSent > 0)
        {
            logger.LogInformation("Sent {Count} budget overspend alert(s)", alertsSent);
        }
    }
}
