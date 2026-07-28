using SubscriptionTracker.Domain.Budgets;

namespace SubscriptionTracker.Api.Contracts.Budgets;

public sealed record CreateBudgetRequest(
    string Name, decimal Amount, string CurrencyCode, BudgetPeriod Period, Guid? CategoryId, int AlertThresholdPercentage);

public sealed record UpdateBudgetRequest(decimal Amount, string CurrencyCode, int AlertThresholdPercentage);
