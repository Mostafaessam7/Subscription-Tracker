using FluentAssertions;
using Microsoft.Extensions.Options;
using SubscriptionTracker.Infrastructure.Financial;

namespace SubscriptionTracker.Application.UnitTests.Financial;

public class StaticExchangeRateProviderTests
{
    private static StaticExchangeRateProvider CreateProvider(string baseCurrency, Dictionary<string, decimal> rates)
    {
        var options = new ExchangeRatesOptions { BaseCurrency = baseCurrency, Rates = new Dictionary<string, decimal>(rates, StringComparer.OrdinalIgnoreCase) };
        return new StaticExchangeRateProvider(Options.Create(options));
    }

    [Fact]
    public void GetRate_ForTheSameCurrency_ShouldReturnOneRegardlessOfConfiguration()
    {
        var provider = CreateProvider("USD", new Dictionary<string, decimal>());

        provider.GetRate("EUR", "EUR").Should().Be(1m);
    }

    [Fact]
    public void GetRate_BetweenTwoNonBaseCurrencies_ShouldConvertThroughTheBase()
    {
        // USD base: 1 USD = 0.92 EUR = 149.5 JPY, so 1 EUR = 149.5/0.92 JPY.
        var provider = CreateProvider("USD", new Dictionary<string, decimal> { ["EUR"] = 0.92m, ["JPY"] = 149.5m });

        var rate = provider.GetRate("EUR", "JPY");

        rate.Should().NotBeNull();
        rate!.Value.Should().BeApproximately(149.5m / 0.92m, 0.001m);
    }

    [Fact]
    public void GetRate_FromTheBaseCurrencyToAConfiguredCurrency_ShouldUseItsRateDirectly()
    {
        var provider = CreateProvider("USD", new Dictionary<string, decimal> { ["EUR"] = 0.92m });

        provider.GetRate("USD", "EUR").Should().Be(0.92m);
    }

    [Fact]
    public void GetRate_ForACurrencyNotInTheTable_ShouldReturnNull()
    {
        var provider = CreateProvider("USD", new Dictionary<string, decimal> { ["EUR"] = 0.92m });

        provider.GetRate("USD", "XYZ").Should().BeNull();
        provider.GetRate("XYZ", "USD").Should().BeNull();
    }

    [Fact]
    public void GetRate_IsCaseInsensitive()
    {
        var provider = CreateProvider("USD", new Dictionary<string, decimal> { ["EUR"] = 0.92m });

        provider.GetRate("usd", "eur").Should().Be(0.92m);
    }
}
