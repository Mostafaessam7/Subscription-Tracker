using FluentValidation;

namespace SubscriptionTracker.Application.Identity.EnableTwoFactor;

public sealed class EnableTwoFactorCommandValidator : AbstractValidator<EnableTwoFactorCommand>
{
    public EnableTwoFactorCommandValidator()
    {
        RuleFor(c => c.Secret).NotEmpty();
        RuleFor(c => c.Code).NotEmpty().Length(6);
    }
}
