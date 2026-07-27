using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.DeletePaymentMethod;

public sealed record DeletePaymentMethodCommand(Guid PaymentMethodId) : ICommand;
