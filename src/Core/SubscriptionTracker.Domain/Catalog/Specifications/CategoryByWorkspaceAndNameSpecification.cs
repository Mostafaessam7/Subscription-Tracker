using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Catalog.Specifications;

public sealed class CategoryByWorkspaceAndNameSpecification : Specification<Category>
{
    public CategoryByWorkspaceAndNameSpecification(Guid workspaceId, string name)
    {
        AddCriteria(c => c.WorkspaceId == workspaceId && c.Name == name);
    }
}
