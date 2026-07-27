using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Subscriptions.UpdateSubscription;

public sealed record UpdateSubscriptionCommand(
    Guid SubscriptionId,
    string Name,
    string Provider,
    string? LogoUrl,
    string? WebsiteUrl,
    string? Notes,
    Guid? CategoryId,
    Guid? PaymentMethodId,
    decimal Amount,
    string CurrencyCode,
    IReadOnlyCollection<Guid>? TagIds) : ICommand;
