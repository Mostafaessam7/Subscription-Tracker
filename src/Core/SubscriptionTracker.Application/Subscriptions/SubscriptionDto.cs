using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Subscriptions;

public sealed record SubscriptionDto(
    Guid Id,
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
    DateOnly? NextRenewalDate,
    DateOnly? EndDate,
    bool AutoRenewal,
    SubscriptionStatus Status,
    IReadOnlyCollection<Guid> TagIds,
    IReadOnlyCollection<Guid> SharedUserIds);
