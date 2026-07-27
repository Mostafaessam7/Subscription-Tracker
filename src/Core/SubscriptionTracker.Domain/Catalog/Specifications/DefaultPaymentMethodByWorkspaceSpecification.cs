using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Catalog.Specifications;

public sealed class DefaultPaymentMethodByWorkspaceSpecification : Specification<PaymentMethod>
{
    public DefaultPaymentMethodByWorkspaceSpecification(Guid workspaceId)
    {
        AddCriteria(p => p.WorkspaceId == workspaceId && p.IsDefault);
    }
}
