using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Tenancy.Roles.GetWorkspaceRoles;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Tenancy.Roles;

public class GetWorkspaceRolesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetWorkspaceRolesQueryHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public GetWorkspaceRolesQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _dbContext = new ApplicationDbContext(options, _currentUserService);
        _handler = new GetWorkspaceRolesQueryHandler(_dbContext, _currentUserService);
    }

    [Fact]
    public async Task Handle_ShouldReturnSystemRolesAndWorkspaceOwnRoles_WithPermissionCodesProjected()
    {
        var systemRole = Role.Create("Viewer", "Read-only", workspaceId: null, isSystemRole: true).Value;
        systemRole.GrantPermission(Permissions.Subscriptions.View);

        var ownRole = Role.Create("Custom", "Custom role", _workspaceId).Value;
        ownRole.GrantPermission(Permissions.Budgets.Manage);

        var otherWorkspaceRole = Role.Create("Other", null, Guid.NewGuid()).Value;

        _dbContext.Roles.AddRange(systemRole, ownRole, otherWorkspaceRole);
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetWorkspaceRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().NotContain(r => r.Id == otherWorkspaceRole.Id);

        var projectedSystemRole = result.Value.Should().ContainSingle(r => r.Id == systemRole.Id).Subject;
        projectedSystemRole.Permissions.Should().Contain(Permissions.Subscriptions.View);

        var projectedOwnRole = result.Value.Should().ContainSingle(r => r.Id == ownRole.Id).Subject;
        projectedOwnRole.Permissions.Should().Contain(Permissions.Budgets.Manage);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
