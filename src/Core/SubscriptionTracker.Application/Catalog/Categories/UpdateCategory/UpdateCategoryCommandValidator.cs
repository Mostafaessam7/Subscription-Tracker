using FluentValidation;

namespace SubscriptionTracker.Application.Catalog.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Color).MaximumLength(20);
        RuleFor(c => c.Icon).MaximumLength(50);
    }
}
