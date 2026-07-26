namespace SubscriptionTracker.Domain.Common.ValueObjects;

public sealed class Money : ValueObject
{
    private Money(decimal amount, string currencyCode)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
    }

    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public static Result<Money> Create(decimal amount, string currencyCode)
    {
        if (amount < 0)
        {
            return Result.Failure<Money>(Error.Validation("Money.NegativeAmount", "Amount cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
        {
            return Result.Failure<Money>(Error.Validation("Money.InvalidCurrencyCode", "Currency code must be a 3-letter ISO 4217 code."));
        }

        return new Money(amount, currencyCode.Trim().ToUpperInvariant());
    }

    public static Money Zero(string currencyCode) => new(0, currencyCode.Trim().ToUpperInvariant());

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, CurrencyCode);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, CurrencyCode);

    private void EnsureSameCurrency(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
        {
            throw new InvalidOperationException(
                $"Cannot operate on money values with different currencies: {CurrencyCode} and {other.CurrencyCode}.");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyCode;
    }

    public override string ToString() => $"{Amount:0.00} {CurrencyCode}";
}
