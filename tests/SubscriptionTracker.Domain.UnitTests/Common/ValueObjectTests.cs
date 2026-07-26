using FluentAssertions;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.UnitTests.Common;

public class ValueObjectTests
{
    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;
        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Equals_WithSameComponents_ShouldBeTrue()
    {
        var a = new Money(10, "USD");
        var b = new Money(10, "USD");

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentComponents_ShouldBeFalse()
    {
        var a = new Money(10, "USD");
        var b = new Money(10, "EUR");

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameComponents_ShouldMatch()
    {
        var a = new Money(10, "USD");
        var b = new Money(10, "USD");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
