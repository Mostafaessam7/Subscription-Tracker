using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Tenancy.GetMyWorkspaces;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Domain.Tenancy.Specifications;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Tenancy;

public class GetMyWorkspacesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRepository<Workspace, Guid> _workspaceRepository = Substitute.For<IRepository<Workspace, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly GetMyWorkspacesQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetMyWorkspacesQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _currentUserService.UserId.Returns(_userId);
        _dbContext = new ApplicationDbContext(options, _currentUserService);
        _handler = new GetMyWorkspacesQueryHandler(_workspaceRepository, _dbContext, _currentUserService);
    }

    private static (Workspace Workspace, Role Role) CreateOwnedWorkspace(string name, Guid ownerId)
    {
        var role = Role.Create("Owner", null, Guid.NewGuid()).Value;
        var workspace = Workspace.Create(name, ownerId, role.Id, DateTimeOffset.UtcNow).Value;
        return (workspace, role);
    }

    [Fact]
    public async Task Handle_WhenUnauthenticated_ShouldReturnEmptyList()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(new GetMyWorkspacesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldListEveryActiveMembership_MarkingOwnershipAndCurrentWorkspace()
    {
        var (ownedWorkspace, ownerRole) = CreateOwnedWorkspace("My Workspace", _userId);

        var otherOwnerId = Guid.NewGuid();
        var (memberWorkspace, _) = CreateOwnedWorkspace("Acme Corp", otherOwnerId);
        var viewerRole = Role.Create("Viewer", null, memberWorkspace.Id, isSystemRole: true).Value;
        var invitedMember = memberWorkspace.InviteMember(_userId, viewerRole.Id, DateTimeOffset.UtcNow).Value;
        memberWorkspace.AcceptInvitation(invitedMember.Id, DateTimeOffset.UtcNow);

        _dbContext.Roles.AddRange(ownerRole, viewerRole);
        await _dbContext.SaveChangesAsync();

        _workspaceRepository
            .ListAsync(Arg.Any<WorkspacesByMemberUserIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns([ownedWorkspace, memberWorkspace]);

        _currentUserService.WorkspaceId.Returns(memberWorkspace.Id);

        var result = await _handler.Handle(new GetMyWorkspacesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var current = result.Value.Should().ContainSingle(w => w.Id == memberWorkspace.Id).Subject;
        current.IsCurrent.Should().BeTrue();
        current.IsOwner.Should().BeFalse();
        current.RoleName.Should().Be("Viewer");

        var owned = result.Value.Should().ContainSingle(w => w.Id == ownedWorkspace.Id).Subject;
        owned.IsCurrent.Should().BeFalse();
        owned.IsOwner.Should().BeTrue();
        owned.RoleName.Should().Be("Owner");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
