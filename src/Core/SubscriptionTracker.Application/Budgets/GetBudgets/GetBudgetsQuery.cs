using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Budgets.GetBudgets;

public sealed record GetBudgetsQuery : IQuery<IReadOnlyList<BudgetDto>>;
