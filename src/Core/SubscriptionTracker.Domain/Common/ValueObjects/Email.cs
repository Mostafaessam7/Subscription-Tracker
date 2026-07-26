using System.Text.RegularExpressions;

namespace SubscriptionTracker.Domain.Common.ValueObjects;

public sealed partial class Email : ValueObject
{
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(Error.Validation("Email.Empty", "Email cannot be empty."));
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 256 || !EmailRegex().IsMatch(normalized))
        {
            return Result.Failure<Email>(Error.Validation("Email.InvalidFormat", "Email format is invalid."));
        }

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
