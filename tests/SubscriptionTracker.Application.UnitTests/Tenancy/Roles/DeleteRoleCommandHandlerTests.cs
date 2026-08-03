using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Tenancy.Roles.DeleteRole;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.UnitTests.Tenancy.Roles;

public class DeleteRoleCommandHandlerTests
{
    private readonly IRepository<Role, Guid> _roleRepository = Substitute.For<IRepository<Role, Guid>>();
    private readonly IRepository<Workspace, Guid> _workspaceRepository = Substitute.For<IRepository<Workspace, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();

    private readonly DeleteRoleCommandHandler _handler;

    public DeleteRoleCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new DeleteRoleCommandHandler(_roleRepository, _workspaceRepository, _currentUserService);
    }

    private Workspace CreateWorkspace(Guid ownerRoleId) =>
        Workspace.Create("Acme", _ownerId, ownerRoleId, DateTimeOffset.UtcNow, _workspaceId).Value;

    [Fact]
    public async Task Handle_ForUnusedCustomRole_ShouldDelete()
    {
        var role = Role.Create("Custom", null, _workspaceId).Value;
        var workspace = CreateWorkspace(Guid.NewGuid());

        _roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _workspaceRepository.GetByIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);

        var result = await _handler.Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _roleRepository.Received(1).Remove(role);
    }

    [Fact]
    public async Task Handle_ForRoleAssignedToAMember_ShouldFailWithConflict()
    {
        var role = Role.Create("Custom", null, _workspaceId).Value;
        var workspace = CreateWorkspace(role.Id);

        _roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _workspaceRepository.GetByIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);

        var result = await _handler.Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeleteRole.InUse");
        _roleRepository.DidNotReceive().Remove(Arg.Any<Role>());
    }

    [Fact]
    public async Task Handle_ForSystemRole_ShouldFail()
    {
        var systemRole = Role.Create("Viewer", null, workspaceId: null, isSystemRole: true).Value;
        _roleRepository.GetByIdAsync(systemRole.Id, Arg.Any<CancellationToken>()).Returns(systemRole);

        var result = await _handler.Handle(new DeleteRoleCommand(systemRole.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeleteRole.SystemRoleImmutable");
    }
}
