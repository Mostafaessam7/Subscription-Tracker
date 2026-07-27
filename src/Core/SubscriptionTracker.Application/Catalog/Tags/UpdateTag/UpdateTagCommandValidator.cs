using FluentValidation;

namespace SubscriptionTracker.Application.Catalog.Tags.UpdateTag;

public sealed class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagCommandValidator()
    {
        RuleFor(t => t.Name).NotEmpty().MaximumLength(50);
        RuleFor(t => t.Color).MaximumLength(20);
    }
}
