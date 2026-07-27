using FluentValidation;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Provider).NotEmpty().MaximumLength(200);
        RuleFor(c => c.LogoUrl).MaximumLength(2048);
        RuleFor(c => c.WebsiteUrl).MaximumLength(2048);
        RuleFor(c => c.Notes).MaximumLength(2000);
        RuleFor(c => c.Amount).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CurrencyCode).NotEmpty().Length(3);
        RuleFor(c => c.BillingFrequency).IsInEnum();

        RuleFor(c => c.CustomIntervalDays)
            .NotNull()
            .GreaterThan(0)
            .When(c => c.BillingFrequency == BillingFrequency.Custom)
            .WithMessage("Custom billing cycles require a positive interval in days.");

        RuleFor(c => c.TrialEndDate)
            .GreaterThanOrEqualTo(c => c.StartDate)
            .When(c => c.TrialEndDate is not null)
            .WithMessage("Trial end date cannot be before the start date.");
    }
}
