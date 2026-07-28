using FluentAssertions;
using SubscriptionTracker.Application.Budgets;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.UnitTests.Budgets;

public class BudgetSpendCalculatorTests
{
    [Fact]
    public void NormalizeToPeriod_MonthlySubscriptionAgainstMonthlyBudget_ShouldReturnSameAmount()
    {
        var result = BudgetSpendCalculator.NormalizeToPeriod(10m, BillingFrequency.Monthly, null, BudgetPeriod.Monthly);

        result.Should().BeApproximately(10m, 0.01m);
    }

    [Fact]
    public void NormalizeToPeriod_YearlySubscriptionAgainstMonthlyBudget_ShouldDivideByTwelve()
    {
        var result = BudgetSpendCalculator.NormalizeToPeriod(120m, BillingFrequency.Yearly, null, BudgetPeriod.Monthly);

        result.Should().BeApproximately(10m, 0.01m);
    }

    [Fact]
    public void NormalizeToPeriod_MonthlySubscriptionAgainstYearlyBudget_ShouldMultiplyByTwelve()
    {
        var result = BudgetSpendCalculator.NormalizeToPeriod(10m, BillingFrequency.Monthly, null, BudgetPeriod.Yearly);

        result.Should().BeApproximately(120m, 0.5m);
    }

    [Fact]
    public void NormalizeToPeriod_CustomFrequencyWithoutIntervalDays_ShouldReturnZero()
    {
        var result = BudgetSpendCalculator.NormalizeToPeriod(50m, BillingFrequency.Custom, null, BudgetPeriod.Monthly);

        result.Should().Be(0m);
    }
}
