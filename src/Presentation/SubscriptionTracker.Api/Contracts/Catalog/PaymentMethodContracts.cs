using SubscriptionTracker.Domain.Catalog;

namespace SubscriptionTracker.Api.Contracts.Catalog;

public sealed record CreatePaymentMethodRequest(PaymentMethodType Type, string Label, string? MaskedDetails, bool IsDefault);

public sealed record UpdatePaymentMethodRequest(string Label, bool IsDefault);
