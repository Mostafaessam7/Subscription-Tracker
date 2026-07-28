using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Budgets;

namespace SubscriptionTracker.Application.Budgets.CreateBudget;

public sealed record CreateBudgetCommand(
    string Name,
    decimal Amount,
    string CurrencyCode,
    BudgetPeriod Period,
    Guid? CategoryId,
    int AlertThresholdPercentage) : ICommand<Guid>;
