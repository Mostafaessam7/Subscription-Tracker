using FluentAssertions;
using SubscriptionTracker.Application.Subscriptions.CreateSubscription;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.UnitTests.Subscriptions;

public class CreateSubscriptionCommandValidatorTests
{
    private readonly CreateSubscriptionCommandValidator _validator = new();

    private static CreateSubscriptionCommand ValidCommand() => new(
        "Netflix", "Netflix Inc.", null, null, null, null, null,
        9.99m, "USD", BillingFrequency.Monthly, null, new DateOnly(2026, 1, 1), null, true, null);

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNegativeAmount_ShouldFail()
    {
        var command = ValidCommand() with { Amount = -1 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_CustomFrequencyWithoutInterval_ShouldFail()
    {
        var command = ValidCommand() with { BillingFrequency = BillingFrequency.Custom, CustomIntervalDays = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TrialEndBeforeStartDate_ShouldFail()
    {
        var command = ValidCommand() with { TrialEndDate = new DateOnly(2025, 12, 1) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var command = ValidCommand() with { Name = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
