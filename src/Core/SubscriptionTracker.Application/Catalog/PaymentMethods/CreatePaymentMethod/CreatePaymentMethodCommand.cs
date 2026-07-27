using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.CreatePaymentMethod;

public sealed record CreatePaymentMethodCommand(
    PaymentMethodType Type, string Label, string? MaskedDetails, bool IsDefault) : ICommand<Guid>;
