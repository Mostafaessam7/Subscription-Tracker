using FluentValidation;

namespace SubscriptionTracker.Application.Tenancy.UpdateWorkspaceSettings;

public sealed class UpdateWorkspaceSettingsCommandValidator : AbstractValidator<UpdateWorkspaceSettingsCommand>
{
    public UpdateWorkspaceSettingsCommandValidator()
    {
        RuleFor(c => c.DefaultCurrencyCode).NotEmpty().Length(3);
        RuleFor(c => c.TimeZoneId).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Locale).NotEmpty().MaximumLength(20);
    }
}
