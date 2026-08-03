using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Tenancy.Roles.GetWorkspaceRoles;

public sealed class GetWorkspaceRolesQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetWorkspaceRolesQuery, IReadOnlyList<RoleDetailDto>>
{
    public async Task<Result<IReadOnlyList<RoleDetailDto>>> Handle(GetWorkspaceRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles
            .Where(r => r.IsSystemRole || r.WorkspaceId == currentUserService.WorkspaceId)
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.Name)
            .Select(r => new RoleDetailDto(r.Id, r.Name, r.Description, r.IsSystemRole, r.PermissionCodes))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RoleDetailDto>>(roles);
    }
}
