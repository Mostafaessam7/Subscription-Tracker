using FluentValidation;

namespace SubscriptionTracker.Application.Catalog.Categories.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Color).MaximumLength(20);
        RuleFor(c => c.Icon).MaximumLength(50);
    }
}
