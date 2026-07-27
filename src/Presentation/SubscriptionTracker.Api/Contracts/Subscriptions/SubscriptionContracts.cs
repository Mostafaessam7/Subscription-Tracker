using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Api.Contracts.Subscriptions;

public sealed record CreateSubscriptionRequest(
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
    IReadOnlyCollection<Guid>? TagIds);

public sealed record UpdateSubscriptionRequest(
    string Name,
    string Provider,
    string? LogoUrl,
    string? WebsiteUrl,
    string? Notes,
    Guid? CategoryId,
    Guid? PaymentMethodId,
    decimal Amount,
    string CurrencyCode,
    IReadOnlyCollection<Guid>? TagIds);

public sealed record CancelSubscriptionRequest(DateOnly EffectiveDate, string? Reason);
