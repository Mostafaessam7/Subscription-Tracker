using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Tenancy.InviteMember;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.UnitTests.Tenancy;

public class InviteMemberCommandHandlerTests
{
    private readonly IRepository<Workspace, Guid> _workspaceRepository = Substitute.For<IRepository<Workspace, Guid>>();
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly IRepository<Role, Guid> _roleRepository = Substitute.For<IRepository<Role, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly InviteMemberCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _ownerRoleId = Guid.NewGuid();

    public InviteMemberCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new InviteMemberCommandHandler(
            _workspaceRepository, _userRepository, _roleRepository, _currentUserService, TimeProvider.System);
    }

    private Workspace CreateWorkspace() => Workspace.Create("Acme", _ownerId, _ownerRoleId, DateTimeOffset.UtcNow, _workspaceId).Value;

    [Fact]
    public async Task Handle_WithUnregisteredEmail_ShouldFailWithNotFound()
    {
        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(new InviteMemberCommand("nobody@example.com", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InviteMember.UserNotFound");
    }

    [Fact]
    public async Task Handle_WithValidUserAndRole_ShouldInviteAndPersist()
    {
        var invitedUser = User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;
        var role = Role.Create("Member", null, workspaceId: null, isSystemRole: true).Value;
        var workspace = CreateWorkspace();

        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>()).Returns(invitedUser);
        _roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _workspaceRepository.GetByIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);

        var result = await _handler.Handle(new InviteMemberCommand("jane@example.com", role.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workspace.Members.Should().Contain(m => m.UserId == invitedUser.Id && m.RoleId == role.Id);
        _workspaceRepository.Received(1).Update(workspace);
    }

    [Fact]
    public async Task Handle_WithRoleFromAnotherWorkspace_ShouldFailWithNotFound()
    {
        var invitedUser = User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;
        var foreignRole = Role.Create("Custom", null, workspaceId: Guid.NewGuid()).Value;

        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>()).Returns(invitedUser);
        _roleRepository.GetByIdAsync(foreignRole.Id, Arg.Any<CancellationToken>()).Returns(foreignRole);

        var result = await _handler.Handle(new InviteMemberCommand("jane@example.com", foreignRole.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InviteMember.RoleNotFound");
    }
}
