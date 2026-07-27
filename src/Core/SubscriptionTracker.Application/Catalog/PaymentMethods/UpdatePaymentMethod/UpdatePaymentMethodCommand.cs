using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.UpdatePaymentMethod;

public sealed record UpdatePaymentMethodCommand(Guid PaymentMethodId, string Label, bool IsDefault) : ICommand;
