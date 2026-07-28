using FluentValidation;

namespace SubscriptionTracker.Application.Identity.DisableTwoFactor;

public sealed class DisableTwoFactorCommandValidator : AbstractValidator<DisableTwoFactorCommand>
{
    public DisableTwoFactorCommandValidator()
    {
        RuleFor(c => c.Code).NotEmpty().Length(6);
    }
}
