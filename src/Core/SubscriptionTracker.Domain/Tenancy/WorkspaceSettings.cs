using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Tenancy;

public sealed class WorkspaceSettings : ValueObject
{
    private WorkspaceSettings(string defaultCurrencyCode, string timeZoneId, string locale)
    {
        DefaultCurrencyCode = defaultCurrencyCode;
        TimeZoneId = timeZoneId;
        Locale = locale;
    }

    public string DefaultCurrencyCode { get; }
    public string TimeZoneId { get; }
    public string Locale { get; }

    public static WorkspaceSettings Default() => new("USD", "UTC", "en-US");

    public static Result<WorkspaceSettings> Create(string defaultCurrencyCode, string timeZoneId, string locale)
    {
        if (string.IsNullOrWhiteSpace(defaultCurrencyCode) || defaultCurrencyCode.Trim().Length != 3)
        {
            return Result.Failure<WorkspaceSettings>(
                Error.Validation("WorkspaceSettings.InvalidCurrency", "Default currency code must be a 3-letter ISO 4217 code."));
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return Result.Failure<WorkspaceSettings>(
                Error.Validation("WorkspaceSettings.InvalidTimeZone", "Time zone id cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            return Result.Failure<WorkspaceSettings>(
                Error.Validation("WorkspaceSettings.InvalidLocale", "Locale cannot be empty."));
        }

        return new WorkspaceSettings(defaultCurrencyCode.Trim().ToUpperInvariant(), timeZoneId.Trim(), locale.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DefaultCurrencyCode;
        yield return TimeZoneId;
        yield return Locale;
    }
}
