using SubscriptionTracker.Domain.Budgets;

namespace SubscriptionTracker.Application.Budgets;

public sealed record BudgetDto(
    Guid Id,
    string Name,
    decimal Amount,
    string CurrencyCode,
    BudgetPeriod Period,
    Guid? CategoryId,
    int AlertThresholdPercentage,
    decimal CurrentSpend,
    bool HasExceededThreshold);
