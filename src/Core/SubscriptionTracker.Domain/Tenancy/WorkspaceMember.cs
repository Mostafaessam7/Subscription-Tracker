using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Tenancy;

public sealed class WorkspaceMember : Entity<Guid>
{
    private WorkspaceMember(Guid id, Guid workspaceId, Guid userId, Guid roleId, DateTimeOffset invitedAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        RoleId = roleId;
        InvitedAtUtc = invitedAtUtc;
        Status = WorkspaceMemberStatus.Invited;
    }

    private WorkspaceMember()
    {
    }

    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public WorkspaceMemberStatus Status { get; private set; }
    public DateTimeOffset InvitedAtUtc { get; private set; }
    public DateTimeOffset? JoinedAtUtc { get; private set; }
    public DateTimeOffset? RemovedAtUtc { get; private set; }

    internal static WorkspaceMember Invite(Guid workspaceId, Guid userId, Guid roleId, DateTimeOffset occurredOnUtc) =>
        new(Guid.NewGuid(), workspaceId, userId, roleId, occurredOnUtc);

    internal void Activate(DateTimeOffset occurredOnUtc)
    {
        Status = WorkspaceMemberStatus.Active;
        JoinedAtUtc = occurredOnUtc;
    }

    internal void ChangeRole(Guid roleId) => RoleId = roleId;

    internal void Remove(DateTimeOffset occurredOnUtc)
    {
        Status = WorkspaceMemberStatus.Removed;
        RemovedAtUtc = occurredOnUtc;
    }
}
