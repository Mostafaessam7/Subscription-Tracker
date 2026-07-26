using FluentAssertions;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Domain.UnitTests.Subscriptions;

public class BillingCycleTests
{
    [Theory]
    [InlineData(BillingFrequency.Weekly, "2026-01-01", "2026-01-08")]
    [InlineData(BillingFrequency.Monthly, "2026-01-01", "2026-02-01")]
    [InlineData(BillingFrequency.Quarterly, "2026-01-01", "2026-04-01")]
    [InlineData(BillingFrequency.Yearly, "2026-01-01", "2027-01-01")]
    public void CalculateNextRenewalDate_ShouldAddCorrectInterval(BillingFrequency frequency, string from, string expected)
    {
        var cycle = BillingCycle.Create(frequency).Value;

        var next = cycle.CalculateNextRenewalDate(DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture));

        next.Should().Be(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CalculateNextRenewalDate_ForCustomFrequency_ShouldAddConfiguredDays()
    {
        var cycle = BillingCycle.Create(BillingFrequency.Custom, customIntervalDays: 45).Value;

        var next = cycle.CalculateNextRenewalDate(new DateOnly(2026, 1, 1));

        next.Should().Be(new DateOnly(2026, 2, 15));
    }

    [Fact]
    public void CalculateNextRenewalDate_ForLifetime_ShouldNotChange()
    {
        var cycle = BillingCycle.Create(BillingFrequency.Lifetime).Value;
        var from = new DateOnly(2026, 1, 1);

        var next = cycle.CalculateNextRenewalDate(from);

        next.Should().Be(from);
    }

    [Fact]
    public void Create_CustomFrequencyWithoutInterval_ShouldFail()
    {
        var result = BillingCycle.Create(BillingFrequency.Custom);

        result.IsFailure.Should().BeTrue();
    }
}
