using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Budgets.GetBudgets;

public sealed class GetBudgetsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetBudgetsQuery, IReadOnlyList<BudgetDto>>
{
    public async Task<Result<IReadOnlyList<BudgetDto>>> Handle(GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var budgets = await dbContext.Budgets
            .Where(b => b.WorkspaceId == currentUserService.WorkspaceId)
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
        {
            return Result.Success<IReadOnlyList<BudgetDto>>([]);
        }

        var subscriptions = await dbContext.Subscriptions
            .Where(s => s.WorkspaceId == currentUserService.WorkspaceId)
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
            .Select(s => new
            {
                s.CategoryId,
                Amount = s.Price.Amount,
                CurrencyCode = s.Price.CurrencyCode,
                Frequency = s.BillingCycle.Frequency,
                s.BillingCycle.CustomIntervalDays,
            })
            .ToListAsync(cancellationToken);

        var dtos = budgets.Select(budget =>
        {
            var currentSpend = subscriptions
                .Where(s => s.CurrencyCode == budget.Amount.CurrencyCode)
                .Where(s => budget.CategoryId is null || s.CategoryId == budget.CategoryId)
                .Sum(s => BudgetSpendCalculator.NormalizeToPeriod(s.Amount, s.Frequency, s.CustomIntervalDays, budget.Period));

            var currentSpendMoney = Money.Create(currentSpend, budget.Amount.CurrencyCode);
            var hasExceeded = currentSpendMoney.IsSuccess && budget.HasExceededThreshold(currentSpendMoney.Value);

            return new BudgetDto(
                budget.Id, budget.Name, budget.Amount.Amount, budget.Amount.CurrencyCode, budget.Period,
                budget.CategoryId, budget.AlertThresholdPercentage, currentSpend, hasExceeded);
        }).ToList();

        return Result.Success<IReadOnlyList<BudgetDto>>(dtos);
    }
}
