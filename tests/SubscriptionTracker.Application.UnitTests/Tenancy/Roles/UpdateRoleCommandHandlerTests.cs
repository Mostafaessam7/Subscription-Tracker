using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Tenancy.Roles.UpdateRole;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.UnitTests.Tenancy.Roles;

public class UpdateRoleCommandHandlerTests
{
    private readonly IRepository<Role, Guid> _roleRepository = Substitute.For<IRepository<Role, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    private readonly UpdateRoleCommandHandler _handler;

    public UpdateRoleCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new UpdateRoleCommandHandler(_roleRepository, _currentUserService);
    }

    private Role CreateCustomRole(params string[] permissions)
    {
        var role = Role.Create("Custom", "desc", _workspaceId).Value;
        foreach (var permission in permissions)
        {
            role.GrantPermission(permission);
        }

        return role;
    }

    [Fact]
    public async Task Handle_ShouldRenameAndReconcilePermissions()
    {
        var role = CreateCustomRole(Permissions.Budgets.View, Permissions.Catalog.View);
        _roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var command = new UpdateRoleCommand(
            role.Id, "Renamed", "new desc", [Permissions.Budgets.View, Permissions.Reports.Export]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        role.Name.Should().Be("Renamed");
        role.PermissionCodes.Should().BeEquivalentTo([Permissions.Budgets.View, Permissions.Reports.Export]);
        _roleRepository.Received(1).Update(role);
    }

    [Fact]
    public async Task Handle_ForSystemRole_ShouldFail()
    {
        var systemRole = Role.Create("Member", null, workspaceId: null, isSystemRole: true).Value;
        _roleRepository.GetByIdAsync(systemRole.Id, Arg.Any<CancellationToken>()).Returns(systemRole);

        var result = await _handler.Handle(
            new UpdateRoleCommand(systemRole.Id, "Hacked", null, []), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UpdateRole.SystemRoleImmutable");
    }

    [Fact]
    public async Task Handle_ForRoleInAnotherWorkspace_ShouldFailWithNotFound()
    {
        var foreignRole = Role.Create("Foreign", null, Guid.NewGuid()).Value;
        _roleRepository.GetByIdAsync(foreignRole.Id, Arg.Any<CancellationToken>()).Returns(foreignRole);

        var result = await _handler.Handle(
            new UpdateRoleCommand(foreignRole.Id, "Renamed", null, []), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UpdateRole.NotFound");
    }
}
