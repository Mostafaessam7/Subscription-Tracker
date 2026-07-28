using FluentValidation;

namespace SubscriptionTracker.Application.Budgets.UpdateBudget;

public sealed class UpdateBudgetCommandValidator : AbstractValidator<UpdateBudgetCommand>
{
    public UpdateBudgetCommandValidator()
    {
        RuleFor(b => b.Amount).GreaterThan(0);
        RuleFor(b => b.CurrencyCode).NotEmpty().Length(3);
        RuleFor(b => b.AlertThresholdPercentage).InclusiveBetween(1, 100);
    }
}
