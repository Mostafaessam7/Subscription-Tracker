using FluentValidation;

namespace SubscriptionTracker.Application.Catalog.Tags.CreateTag;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(t => t.Name).NotEmpty().MaximumLength(50);
        RuleFor(t => t.Color).MaximumLength(20);
    }
}
