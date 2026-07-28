using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Budgets;

/// <summary>
/// Normalizes a subscription's billing amount to a budget period (monthly/yearly), annualizing first so
/// weekly/quarterly/custom-interval cycles all compare on the same basis. Shared between the interactive
/// GetBudgets query (so the UI shows live spend) and BudgetAlertJob (so the alert email uses the same number).
/// </summary>
public static class BudgetSpendCalculator
{
    private const double AverageDaysPerMonth = 30.4368;
    private const double AverageDaysPerYear = 365.25;

    public static decimal NormalizeToPeriod(decimal amount, BillingFrequency frequency, int? customIntervalDays, BudgetPeriod period)
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
