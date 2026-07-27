using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Subscriptions.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    string Name,
    string Provider,
    string? LogoUrl,
    string? WebsiteUrl,
    string? Notes,
    Guid? CategoryId,
    Guid? PaymentMethodId,
    decimal Amount,
    string CurrencyCode,
    BillingFrequency BillingFrequency,
    int? CustomIntervalDays,
    DateOnly StartDate,
    DateOnly? TrialEndDate,
    bool AutoRenewal,
    IReadOnlyCollection<Guid>? TagIds) : ICommand<Guid>;
