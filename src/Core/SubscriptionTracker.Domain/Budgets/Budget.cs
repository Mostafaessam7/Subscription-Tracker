using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Domain.Budgets;

public sealed class Budget : AuditableAggregateRoot<Guid>
{
    private Budget(Guid id, Guid workspaceId, string name, Money amount, BudgetPeriod period, Guid? categoryId, int alertThresholdPercentage)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Amount = amount;
        Period = period;
        CategoryId = categoryId;
        AlertThresholdPercentage = alertThresholdPercentage;
    }

    private Budget()
    {
    }

    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public BudgetPeriod Period { get; private set; }

    /// <summary>When null, this budget applies to total spending across all categories.</summary>
    public Guid? CategoryId { get; private set; }

    public int AlertThresholdPercentage { get; private set; }

    public static Result<Budget> Create(
        Guid workspaceId, string name, Money amount, BudgetPeriod period, Guid? categoryId = null, int alertThresholdPercentage = 80)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Budget>(Error.Validation("Budget.EmptyName", "Budget name cannot be empty."));
        }

        if (alertThresholdPercentage is <= 0 or > 100)
        {
            return Result.Failure<Budget>(
                Error.Validation("Budget.InvalidThreshold", "Alert threshold must be between 1 and 100."));
        }

        return new Budget(Guid.NewGuid(), workspaceId, name.Trim(), amount, period, categoryId, alertThresholdPercentage);
    }

    public void UpdateAmount(Money amount) => Amount = amount;

    public void UpdateThreshold(int alertThresholdPercentage)
    {
        if (alertThresholdPercentage is > 0 and <= 100)
        {
            AlertThresholdPercentage = alertThresholdPercentage;
        }
    }

    /// <summary>Evaluates whether the given spend amount has crossed the alert threshold for this budget.</summary>
    public bool HasExceededThreshold(Money spentSoFar)
    {
        if (spentSoFar.CurrencyCode != Amount.CurrencyCode || Amount.Amount == 0)
        {
            return false;
        }

        var spentPercentage = spentSoFar.Amount / Amount.Amount * 100m;
        return spentPercentage >= AlertThresholdPercentage;
    }
}
