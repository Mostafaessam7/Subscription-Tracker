using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.GetPaymentMethods;

public sealed record GetPaymentMethodsQuery : IQuery<IReadOnlyList<PaymentMethodDto>>;
