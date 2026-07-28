using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Budgets.DeleteBudget;

public sealed record DeleteBudgetCommand(Guid BudgetId) : ICommand;
