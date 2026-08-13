using Microsoft.Extensions.Options;
using SubscriptionTracker.Application.Abstractions;

namespace SubscriptionTracker.Infrastructure.Financial;

/// <summary>See <see cref="ExchangeRatesOptions"/> for the config shape and quoting convention.</summary>
public sealed class StaticExchangeRateProvider(IOptions<ExchangeRatesOptions> options) : IExchangeRateProvider
{
    public decimal? GetRate(string fromCurrencyCode, string toCurrencyCode)
    {
        if (string.Equals(fromCurrencyCode, toCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var config = options.Value;
        var baseCurrency = config.BaseCurrency;

        // A currency equal to the configured base is implicitly worth exactly 1 base unit even if it isn't
        // (redundantly) listed in Rates itself.
        var fromRate = string.Equals(fromCurrencyCode, baseCurrency, StringComparison.OrdinalIgnoreCase)
            ? 1m
            : config.Rates.GetValueOrDefault(fromCurrencyCode, 0m);
        var toRate = string.Equals(toCurrencyCode, baseCurrency, StringComparison.OrdinalIgnoreCase)
            ? 1m
            : config.Rates.GetValueOrDefault(toCurrencyCode, 0m);

        if (fromRate == 0m || toRate == 0m)
        {
            return null;
        }

        // fromRate/toRate are "units of that currency per 1 base unit", so converting 1 unit of `from` into
        // base is (1 / fromRate), then into `to` is that times toRate.
        return toRate / fromRate;
    }
}
