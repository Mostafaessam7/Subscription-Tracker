using FluentAssertions;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Domain.Tenancy.Events;

namespace SubscriptionTracker.Domain.UnitTests.Tenancy;

public class WorkspaceTests
{
    [Fact]
    public void Create_ShouldAddOwnerAsActiveMemberAndRaiseEvent()
    {
        var ownerId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var workspace = Workspace.Create("Acme Inc.", ownerId, roleId, DateTimeOffset.UtcNow).Value;

        workspace.Members.Should().ContainSingle(m => m.UserId == ownerId && m.Status == WorkspaceMemberStatus.Active);
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceCreated);
    }

    [Fact]
    public void InviteMember_WhenAlreadyMember_ShouldFail()
    {
        var ownerId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme Inc.", ownerId, roleId, DateTimeOffset.UtcNow).Value;

        var result = workspace.InviteMember(ownerId, roleId, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void InviteMember_ThenAcceptInvitation_ShouldActivateMember()
    {
        var workspace = Workspace.Create("Acme Inc.", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
        var newUserId = Guid.NewGuid();

        var inviteResult = workspace.InviteMember(newUserId, Guid.NewGuid(), DateTimeOffset.UtcNow);
        inviteResult.IsSuccess.Should().BeTrue();

        var acceptResult = workspace.AcceptInvitation(inviteResult.Value.Id, DateTimeOffset.UtcNow);

        acceptResult.IsSuccess.Should().BeTrue();
        workspace.Members.Single(m => m.UserId == newUserId).Status.Should().Be(WorkspaceMemberStatus.Active);
    }

    [Fact]
    public void RemoveMember_WhenOwner_ShouldFail()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme Inc.", ownerId, Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
        var ownerMemberId = workspace.Members.Single().Id;

        var result = workspace.RemoveMember(ownerMemberId, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveMember_WhenRegularMember_ShouldSucceed()
    {
        var workspace = Workspace.Create("Acme Inc.", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
        var memberId = workspace.InviteMember(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow).Value.Id;

        var result = workspace.RemoveMember(memberId, DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        workspace.Members.Single(m => m.Id == memberId).Status.Should().Be(WorkspaceMemberStatus.Removed);
    }
}
