using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Domain.Subscriptions;

public sealed class BillingCycle : ValueObject
{
    private BillingCycle(BillingFrequency frequency, int? customIntervalDays)
    {
        Frequency = frequency;
        CustomIntervalDays = customIntervalDays;
    }

    public BillingFrequency Frequency { get; }

    /// <summary>Only populated when <see cref="Frequency"/> is <see cref="BillingFrequency.Custom"/>.</summary>
    public int? CustomIntervalDays { get; }

    public static Result<BillingCycle> Create(BillingFrequency frequency, int? customIntervalDays = null)
    {
        if (frequency == BillingFrequency.Custom)
        {
            if (customIntervalDays is null or <= 0)
            {
                return Result.Failure<BillingCycle>(
                    Error.Validation("BillingCycle.InvalidCustomInterval", "Custom billing cycles require a positive interval in days."));
            }

            return new BillingCycle(frequency, customIntervalDays);
        }

        return new BillingCycle(frequency, null);
    }

    public DateOnly CalculateNextRenewalDate(DateOnly fromDate) => Frequency switch
    {
        BillingFrequency.Weekly => fromDate.AddDays(7),
        BillingFrequency.Monthly => fromDate.AddMonths(1),
        BillingFrequency.Quarterly => fromDate.AddMonths(3),
        BillingFrequency.Yearly => fromDate.AddYears(1),
        BillingFrequency.Custom => fromDate.AddDays(CustomIntervalDays!.Value),
        BillingFrequency.Lifetime => fromDate,
        _ => throw new ArgumentOutOfRangeException(nameof(fromDate), Frequency, "Unsupported billing frequency."),
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Frequency;
        yield return CustomIntervalDays;
    }

    public override string ToString() =>
        Frequency == BillingFrequency.Custom ? $"Every {CustomIntervalDays} day(s)" : Frequency.ToString();
}
