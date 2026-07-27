using SubscriptionTracker.Domain.Catalog;

namespace SubscriptionTracker.Application.Catalog;

public sealed record PaymentMethodDto(Guid Id, PaymentMethodType Type, string Label, string? MaskedDetails, bool IsDefault);
