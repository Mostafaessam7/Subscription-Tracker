using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Catalog;

public sealed class PaymentMethod : AuditableAggregateRoot<Guid>
{
    private PaymentMethod(Guid id, Guid workspaceId, PaymentMethodType type, string label, string? maskedDetails, bool isDefault)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Type = type;
        Label = label;
        MaskedDetails = maskedDetails;
        IsDefault = isDefault;
    }

    private PaymentMethod()
    {
    }

    public Guid WorkspaceId { get; private set; }
    public PaymentMethodType Type { get; private set; }
    public string Label { get; private set; } = string.Empty;

    /// <summary>Non-sensitive display fragment only (e.g. "Visa •••• 4242"). Never store full card/account numbers.</summary>
    public string? MaskedDetails { get; private set; }

    public bool IsDefault { get; private set; }

    public static Result<PaymentMethod> Create(
        Guid workspaceId, PaymentMethodType type, string label, string? maskedDetails = null, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return Result.Failure<PaymentMethod>(Error.Validation("PaymentMethod.EmptyLabel", "Payment method label cannot be empty."));
        }

        return new PaymentMethod(Guid.NewGuid(), workspaceId, type, label.Trim(), maskedDetails, isDefault);
    }

    public void MarkAsDefault() => IsDefault = true;

    public void UnmarkAsDefault() => IsDefault = false;

    public Result Rename(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return Result.Failure(Error.Validation("PaymentMethod.EmptyLabel", "Payment method label cannot be empty."));
        }

        Label = label.Trim();
        return Result.Success();
    }
}
