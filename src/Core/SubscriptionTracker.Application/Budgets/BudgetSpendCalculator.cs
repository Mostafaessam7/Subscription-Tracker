using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Budgets;

/// <summary>
/// Normalizes a subscription's billing amount to a budget period (monthly/yearly), annualizing first so
/// weekly/quarterly/custom-interval cycles all compare on the same basis. Shared between the interactive
/// GetBudgets query (so the UI shows live spend), BudgetAlertJob (so the alert email uses the same number),
/// and GetDashboardSummaryQuery (so the dashboard's estimated spend agrees with both).
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

    /// <summary>
    /// NormalizeToPeriod plus currency conversion into <paramref name="targetCurrencyCode"/> via
    /// <paramref name="exchangeRateProvider"/> - the exact "convert cross-currency subscriptions instead of
    /// skipping them" logic that used to be copy-pasted independently in GetBudgetsQueryHandler and
    /// BudgetAlertJob. A currency with no known rate contributes 0, same as being excluded outright.
    /// </summary>
    public static decimal NormalizeAndConvertToPeriod(
        decimal amount, BillingFrequency frequency, int? customIntervalDays, BudgetPeriod period,
        string sourceCurrencyCode, string targetCurrencyCode, IExchangeRateProvider exchangeRateProvider)
    {
        var rate = sourceCurrencyCode == targetCurrencyCode
            ? 1m
            : exchangeRateProvider.GetRate(sourceCurrencyCode, targetCurrencyCode) ?? 0m;

        return rate * NormalizeToPeriod(amount, frequency, customIntervalDays, period);
    }
}
