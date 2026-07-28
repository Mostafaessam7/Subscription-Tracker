using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Tenancy.Specifications;

public sealed class WorkspaceByMemberIdSpecification : Specification<Workspace>
{
    public WorkspaceByMemberIdSpecification(Guid memberId)
    {
        AddCriteria(w => w.Members.Any(m => m.Id == memberId));
        AddInclude(w => w.Members);
    }
}
