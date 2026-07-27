using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Catalog.Specifications;

public sealed class TagByWorkspaceAndNameSpecification : Specification<Tag>
{
    public TagByWorkspaceAndNameSpecification(Guid workspaceId, string name)
    {
        AddCriteria(t => t.WorkspaceId == workspaceId && t.Name == name);
    }
}
