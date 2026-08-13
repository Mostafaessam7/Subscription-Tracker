namespace SubscriptionTracker.Infrastructure.Financial;

/// <summary>
/// Static, manually-maintained exchange rate table (config key "ExchangeRates"). <see cref="Rates"/> maps a
/// 3-letter currency code to how many units of that currency equal one unit of <see cref="BaseCurrency"/>
/// (standard "X per base" forex quoting convention) - e.g. with BaseCurrency "USD", an entry "EUR": 0.92 means
/// 1 USD = 0.92 EUR. A currency absent from the table has no known rate; StaticExchangeRateProvider returns
/// null for it rather than guessing.
/// </summary>
public sealed class ExchangeRatesOptions
{
    public const string SectionName = "ExchangeRates";

    public string BaseCurrency { get; init; } = "USD";

    public Dictionary<string, decimal> Rates { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
