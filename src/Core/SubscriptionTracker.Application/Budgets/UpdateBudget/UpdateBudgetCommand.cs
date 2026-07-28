using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Budgets.UpdateBudget;

public sealed record UpdateBudgetCommand(Guid BudgetId, decimal Amount, string CurrencyCode, int AlertThresholdPercentage) : ICommand;
