using FluentValidation;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.UpdatePaymentMethod;

public sealed class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
{
    public UpdatePaymentMethodCommandValidator()
    {
        RuleFor(p => p.Label).NotEmpty().MaximumLength(100);
    }
}
