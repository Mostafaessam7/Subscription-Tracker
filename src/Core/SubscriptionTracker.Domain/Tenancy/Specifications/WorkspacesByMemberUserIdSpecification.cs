using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Tenancy.Specifications;

public sealed class WorkspacesByMemberUserIdSpecification : Specification<Workspace>
{
    public WorkspacesByMemberUserIdSpecification(Guid userId)
    {
        AddCriteria(w => w.Members.Any(m => m.UserId == userId && m.Status == WorkspaceMemberStatus.Active));
        AddInclude(w => w.Members);
    }
}
