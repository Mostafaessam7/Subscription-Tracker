using FluentAssertions;
using SubscriptionTracker.Application.Budgets.CreateBudget;
using SubscriptionTracker.Domain.Budgets;

namespace SubscriptionTracker.Application.UnitTests.Budgets;

public class CreateBudgetCommandValidatorTests
{
    private readonly CreateBudgetCommandValidator _validator = new();

    private static CreateBudgetCommand ValidCommand() =>
        new("Monthly subscriptions", 200m, "USD", BudgetPeriod.Monthly, null, 80);

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithZeroAmount_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Amount = 0 });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithThresholdOutOfRange_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { AlertThresholdPercentage = 101 });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Name = "" });

        result.IsValid.Should().BeFalse();
    }
}
