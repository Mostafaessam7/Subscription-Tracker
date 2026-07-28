using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Tenancy.Specifications;

public sealed class WorkspacesByInvitedUserIdSpecification : Specification<Workspace>
{
    public WorkspacesByInvitedUserIdSpecification(Guid userId)
    {
        AddCriteria(w => w.Members.Any(m => m.UserId == userId && m.Status == WorkspaceMemberStatus.Invited));
        AddInclude(w => w.Members);
    }
}
