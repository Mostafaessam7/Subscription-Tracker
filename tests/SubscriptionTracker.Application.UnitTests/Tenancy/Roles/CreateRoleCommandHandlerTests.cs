using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Tenancy.Roles.CreateRole;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.UnitTests.Tenancy.Roles;

public class CreateRoleCommandHandlerTests
{
    private readonly IRepository<Role, Guid> _roleRepository = Substitute.For<IRepository<Role, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new CreateRoleCommandHandler(_roleRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateRoleWithGrantedPermissions()
    {
        var command = new CreateRoleCommand(
            "Billing Manager", "Can manage budgets and view reports",
            [Permissions.Budgets.Manage, Permissions.Reports.View]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _roleRepository.Received(1).Add(Arg.Is<Role>(r =>
            r.Name == "Billing Manager" &&
            r.WorkspaceId == _workspaceId &&
            !r.IsSystemRole &&
            r.PermissionCodes.Contains(Permissions.Budgets.Manage) &&
            r.PermissionCodes.Contains(Permissions.Reports.View)));
    }

    [Fact]
    public async Task Handle_WithUnknownPermissionCode_ShouldFail()
    {
        var command = new CreateRoleCommand("Bad Role", null, ["not-a-real-permission"]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Role.UnknownPermission");
        _roleRepository.DidNotReceive().Add(Arg.Any<Role>());
    }

    [Fact]
    public async Task Handle_WithoutActiveWorkspace_ShouldFail()
    {
        _currentUserService.WorkspaceId.Returns((Guid?)null);

        var result = await _handler.Handle(new CreateRoleCommand("Role", null, []), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreateRole.NoActiveWorkspace");
    }
}
