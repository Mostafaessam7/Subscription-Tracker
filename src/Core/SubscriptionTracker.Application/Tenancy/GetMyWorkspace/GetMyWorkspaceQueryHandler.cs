using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.GetMyWorkspace;

public sealed class GetMyWorkspaceQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetMyWorkspaceQuery, WorkspaceDto>
{
    public async Task<Result<WorkspaceDto>> Handle(GetMyWorkspaceQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure<WorkspaceDto>(
                Error.Unauthorized("GetMyWorkspace.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var workspace = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == currentUserService.WorkspaceId, cancellationToken);

        if (workspace is null)
        {
            return Result.Failure<WorkspaceDto>(Error.NotFound("GetMyWorkspace.NotFound", "Workspace was not found."));
        }

        var activeMembers = workspace.Members.Where(m => m.Status != WorkspaceMemberStatus.Removed).ToList();
        var userIds = activeMembers.Select(m => m.UserId).ToList();
        var roleIds = activeMembers.Select(m => m.RoleId).ToList();

        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Email = u.Email.Value, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var roles = await dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Name })
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var memberDtos = activeMembers
            .Where(m => users.ContainsKey(m.UserId))
            .Select(m => new WorkspaceMemberDto(
                m.Id,
                m.UserId,
                users[m.UserId].Email,
                users[m.UserId].FirstName,
                users[m.UserId].LastName,
                m.RoleId,
                roles.GetValueOrDefault(m.RoleId, "Unknown"),
                m.Status.ToString()))
            .ToList();

        var dto = new WorkspaceDto(
            workspace.Id, workspace.Name, workspace.OwnerId,
            workspace.Settings.DefaultCurrencyCode, workspace.Settings.TimeZoneId, workspace.Settings.Locale,
            memberDtos);

        return Result.Success(dto);
    }
}
