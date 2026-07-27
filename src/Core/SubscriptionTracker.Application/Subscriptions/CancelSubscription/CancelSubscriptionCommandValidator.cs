using FluentValidation;

namespace SubscriptionTracker.Application.Subscriptions.CancelSubscription;

public sealed class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionCommandValidator()
    {
        RuleFor(c => c.SubscriptionId).NotEmpty();
        RuleFor(c => c.Reason).MaximumLength(500);
    }
}
