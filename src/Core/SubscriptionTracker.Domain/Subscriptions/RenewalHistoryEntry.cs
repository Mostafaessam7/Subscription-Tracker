using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Domain.Subscriptions;

public sealed class RenewalHistoryEntry : Entity<Guid>
{
    private RenewalHistoryEntry(
        Guid id, Guid subscriptionId, DateTimeOffset renewedAtUtc, Money amountCharged, DateOnly previousRenewalDate, DateOnly newRenewalDate)
        : base(id)
    {
        SubscriptionId = subscriptionId;
        RenewedAtUtc = renewedAtUtc;
        AmountCharged = amountCharged;
        PreviousRenewalDate = previousRenewalDate;
        NewRenewalDate = newRenewalDate;
    }

    private RenewalHistoryEntry()
    {
    }

    public Guid SubscriptionId { get; private set; }
    public DateTimeOffset RenewedAtUtc { get; private set; }
    public Money AmountCharged { get; private set; } = null!;
    public DateOnly PreviousRenewalDate { get; private set; }
    public DateOnly NewRenewalDate { get; private set; }

    internal static RenewalHistoryEntry Create(
        Guid subscriptionId, DateTimeOffset renewedAtUtc, Money amountCharged, DateOnly previousRenewalDate, DateOnly newRenewalDate) =>
        new(Guid.NewGuid(), subscriptionId, renewedAtUtc, amountCharged, previousRenewalDate, newRenewalDate);
}
