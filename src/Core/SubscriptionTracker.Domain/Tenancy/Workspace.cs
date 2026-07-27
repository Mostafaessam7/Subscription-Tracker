using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Tenancy.Events;

namespace SubscriptionTracker.Domain.Tenancy;

public sealed class Workspace : AuditableAggregateRoot<Guid>
{
    private readonly List<WorkspaceMember> _members = [];

    private Workspace(Guid id, string name, Guid ownerId, WorkspaceSettings settings)
        : base(id)
    {
        Name = name;
        OwnerId = ownerId;
        Settings = settings;
    }

    private Workspace()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public WorkspaceSettings Settings { get; private set; } = WorkspaceSettings.Default();

    public IReadOnlyCollection<WorkspaceMember> Members => _members.AsReadOnly();

    public static Result<Workspace> Create(
        string name, Guid ownerId, Guid ownerRoleId, DateTimeOffset occurredOnUtc, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Workspace>(Error.Validation("Workspace.EmptyName", "Workspace name cannot be empty."));
        }

        var workspace = new Workspace(id ?? Guid.NewGuid(), name.Trim(), ownerId, WorkspaceSettings.Default());
        var ownerMember = WorkspaceMember.Invite(workspace.Id, ownerId, ownerRoleId, occurredOnUtc);
        ownerMember.Activate(occurredOnUtc);
        workspace._members.Add(ownerMember);

        workspace.RaiseDomainEvent(new WorkspaceCreated(workspace.Id, ownerId, workspace.Name));

        return workspace;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workspace name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public void UpdateSettings(WorkspaceSettings settings) => Settings = settings;

    public Result<WorkspaceMember> InviteMember(Guid userId, Guid roleId, DateTimeOffset occurredOnUtc)
    {
        if (_members.Any(m => m.UserId == userId && m.Status != WorkspaceMemberStatus.Removed))
        {
            return Result.Failure<WorkspaceMember>(
                Error.Conflict("Workspace.MemberAlreadyExists", "This user is already a member of the workspace."));
        }

        var member = WorkspaceMember.Invite(Id, userId, roleId, occurredOnUtc);
        _members.Add(member);

        RaiseDomainEvent(new WorkspaceMemberInvited(Id, member.Id, userId, roleId));

        return member;
    }

    public Result AcceptInvitation(Guid memberId, DateTimeOffset occurredOnUtc)
    {
        var member = _members.FirstOrDefault(m => m.Id == memberId);
        if (member is null)
        {
            return Result.Failure(Error.NotFound("Workspace.MemberNotFound", "Workspace member was not found."));
        }

        member.Activate(occurredOnUtc);
        return Result.Success();
    }

    public Result RemoveMember(Guid memberId, DateTimeOffset occurredOnUtc)
    {
        var member = _members.FirstOrDefault(m => m.Id == memberId);
        if (member is null)
        {
            return Result.Failure(Error.NotFound("Workspace.MemberNotFound", "Workspace member was not found."));
        }

        if (member.UserId == OwnerId)
        {
            return Result.Failure(Error.Failure("Workspace.CannotRemoveOwner", "The workspace owner cannot be removed."));
        }

        member.Remove(occurredOnUtc);
        return Result.Success();
    }

    public Result ChangeMemberRole(Guid memberId, Guid roleId)
    {
        var member = _members.FirstOrDefault(m => m.Id == memberId);
        if (member is null)
        {
            return Result.Failure(Error.NotFound("Workspace.MemberNotFound", "Workspace member was not found."));
        }

        member.ChangeRole(roleId);
        return Result.Success();
    }
}
