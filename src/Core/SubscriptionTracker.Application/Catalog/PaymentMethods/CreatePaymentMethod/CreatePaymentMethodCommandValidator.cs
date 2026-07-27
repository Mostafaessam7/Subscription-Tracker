using FluentValidation;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.CreatePaymentMethod;

public sealed class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        RuleFor(p => p.Type).IsInEnum();
        RuleFor(p => p.Label).NotEmpty().MaximumLength(100);
        RuleFor(p => p.MaskedDetails).MaximumLength(100);
    }
}
