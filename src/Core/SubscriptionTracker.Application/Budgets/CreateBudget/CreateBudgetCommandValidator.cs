using FluentValidation;

namespace SubscriptionTracker.Application.Budgets.CreateBudget;

public sealed class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(b => b.Name).NotEmpty().MaximumLength(100);
        RuleFor(b => b.Amount).GreaterThan(0);
        RuleFor(b => b.CurrencyCode).NotEmpty().Length(3);
        RuleFor(b => b.Period).IsInEnum();
        RuleFor(b => b.AlertThresholdPercentage).InclusiveBetween(1, 100);
    }
}
