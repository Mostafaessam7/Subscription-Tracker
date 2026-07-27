using FluentValidation;

namespace SubscriptionTracker.Application.Subscriptions.UpdateSubscription;

public sealed class UpdateSubscriptionCommandValidator : AbstractValidator<UpdateSubscriptionCommand>
{
    public UpdateSubscriptionCommandValidator()
    {
        RuleFor(c => c.SubscriptionId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Provider).NotEmpty().MaximumLength(200);
        RuleFor(c => c.LogoUrl).MaximumLength(2048);
        RuleFor(c => c.WebsiteUrl).MaximumLength(2048);
        RuleFor(c => c.Notes).MaximumLength(2000);
        RuleFor(c => c.Amount).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CurrencyCode).NotEmpty().Length(3);
    }
}
