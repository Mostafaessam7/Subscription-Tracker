using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Tenancy.AcceptInvitation;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.UnitTests.Tenancy;

public class AcceptInvitationCommandHandlerTests
{
    private readonly IRepository<Workspace, Guid> _workspaceRepository = Substitute.For<IRepository<Workspace, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly AcceptInvitationCommandHandler _handler;
    private readonly Guid _ownerId = Guid.NewGuid();

    public AcceptInvitationCommandHandlerTests()
    {
        _handler = new AcceptInvitationCommandHandler(_workspaceRepository, _currentUserService, TimeProvider.System);
    }

    private (Workspace Workspace, Guid MemberId) CreateWorkspaceWithInvitedMember(Guid invitedUserId)
    {
        var workspace = Workspace.Create("Acme", _ownerId, Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
        var member = workspace.InviteMember(invitedUserId, Guid.NewGuid(), DateTimeOffset.UtcNow).Value;
        return (workspace, member.Id);
    }

    [Fact]
    public async Task Handle_WhenInvitationBelongsToCurrentUser_ShouldActivateMembership()
    {
        var invitedUserId = Guid.NewGuid();
        var (workspace, memberId) = CreateWorkspaceWithInvitedMember(invitedUserId);

        _currentUserService.UserId.Returns(invitedUserId);
        _workspaceRepository.FirstOrDefaultAsync(Arg.Any<Specification<Workspace>>(), Arg.Any<CancellationToken>())
            .Returns(workspace);

        var result = await _handler.Handle(new AcceptInvitationCommand(memberId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workspace.Members.Single(m => m.Id == memberId).Status.Should().Be(WorkspaceMemberStatus.Active);
        _workspaceRepository.Received(1).Update(workspace);
    }

    [Fact]
    public async Task Handle_WhenInvitationBelongsToSomeoneElse_ShouldFailWithForbidden()
    {
        var invitedUserId = Guid.NewGuid();
        var (workspace, memberId) = CreateWorkspaceWithInvitedMember(invitedUserId);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _workspaceRepository.FirstOrDefaultAsync(Arg.Any<Specification<Workspace>>(), Arg.Any<CancellationToken>())
            .Returns(workspace);

        var result = await _handler.Handle(new AcceptInvitationCommand(memberId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AcceptInvitation.NotYourInvitation");
    }
}
