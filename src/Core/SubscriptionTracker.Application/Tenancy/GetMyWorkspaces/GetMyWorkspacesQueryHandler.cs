using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Domain.Tenancy.Specifications;

namespace SubscriptionTracker.Application.Tenancy.GetMyWorkspaces;

/// <summary>
/// Lists every workspace the current user is an active member of, so the frontend can offer a workspace
/// switcher. Complements RefreshTokenCommand, which already accepts a target WorkspaceId and re-issues an
/// access token scoped to it (validated as an active membership) - this query is what lets the UI discover
/// which workspace ids are actually valid to switch to.
/// </summary>
public sealed class GetMyWorkspacesQueryHandler(
    IRepository<Workspace, Guid> workspaceRepository, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetMyWorkspacesQuery, IReadOnlyList<MyWorkspaceSummaryDto>>
{
    public async Task<Result<IReadOnlyList<MyWorkspaceSummaryDto>>> Handle(
        GetMyWorkspacesQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Success<IReadOnlyList<MyWorkspaceSummaryDto>>([]);
        }

        var userId = currentUserService.UserId.Value;

        var workspaces = await workspaceRepository.ListAsync(new WorkspacesByMemberUserIdSpecification(userId), cancellationToken);

        var roleIds = workspaces
            .Select(w => w.Members.First(m => m.UserId == userId && m.Status == WorkspaceMemberStatus.Active).RoleId)
            .Distinct()
            .ToList();

        var roleNames = await dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var summaries = workspaces
            .Select(w =>
            {
                var member = w.Members.First(m => m.UserId == userId && m.Status == WorkspaceMemberStatus.Active);
                return new MyWorkspaceSummaryDto(
                    w.Id,
                    w.Name,
                    roleNames.GetValueOrDefault(member.RoleId, "Unknown"),
                    w.OwnerId == userId,
                    w.Id == currentUserService.WorkspaceId);
            })
            .OrderByDescending(s => s.IsCurrent)
            .ThenByDescending(s => s.IsOwner)
            .ThenBy(s => s.Name)
            .ToList();

        return Result.Success<IReadOnlyList<MyWorkspaceSummaryDto>>(summaries);
    }
}
