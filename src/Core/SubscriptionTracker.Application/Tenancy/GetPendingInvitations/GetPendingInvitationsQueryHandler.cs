using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Domain.Tenancy.Specifications;

namespace SubscriptionTracker.Application.Tenancy.GetPendingInvitations;

public sealed class GetPendingInvitationsQueryHandler(
    IRepository<Workspace, Guid> workspaceRepository, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetPendingInvitationsQuery, IReadOnlyList<PendingInvitationDto>>
{
    public async Task<Result<IReadOnlyList<PendingInvitationDto>>> Handle(
        GetPendingInvitationsQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Success<IReadOnlyList<PendingInvitationDto>>([]);
        }

        var workspaces = await workspaceRepository.ListAsync(
            new WorkspacesByInvitedUserIdSpecification(currentUserService.UserId.Value), cancellationToken);

        if (workspaces.Count == 0)
        {
            return Result.Success<IReadOnlyList<PendingInvitationDto>>([]);
        }

        var roleIds = workspaces
            .SelectMany(w => w.Members.Where(m => m.UserId == currentUserService.UserId && m.Status == WorkspaceMemberStatus.Invited))
            .Select(m => m.RoleId)
            .ToList();

        var roleNames = await dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var invitations = workspaces
            .SelectMany(w => w.Members
                .Where(m => m.UserId == currentUserService.UserId && m.Status == WorkspaceMemberStatus.Invited)
                .Select(m => new PendingInvitationDto(w.Id, w.Name, m.Id, roleNames.GetValueOrDefault(m.RoleId, "Unknown"))))
            .ToList();

        return Result.Success<IReadOnlyList<PendingInvitationDto>>(invitations);
    }
}
