namespace SubscriptionTracker.Application.Abstractions;

/// <summary>
/// Converts an amount between currencies so Budgets can sum spend across a workspace's subscriptions even when
/// they aren't all in the budget's own currency. No live FX API is wired up (out of scope - would need a paid
/// provider and a refresh/caching story); the only implementation today reads a static, manually-maintained rate
/// table from config (see Infrastructure.Financial.StaticExchangeRateProvider). Kept as an abstraction so a real
/// live-rate provider can be swapped in later without touching Budgets application code.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Returns the multiplier to convert 1 unit of <paramref name="fromCurrencyCode"/> into
    /// <paramref name="toCurrencyCode"/>, or <see langword="null"/> if either currency has no known rate
    /// (callers should treat that subscription's spend as unconvertible rather than guessing).
    /// </summary>
    decimal? GetRate(string fromCurrencyCode, string toCurrencyCode);
}
