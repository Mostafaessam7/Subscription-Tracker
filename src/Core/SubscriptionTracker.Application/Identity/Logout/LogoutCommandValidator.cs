using FluentValidation;

namespace SubscriptionTracker.Application.Identity.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}
