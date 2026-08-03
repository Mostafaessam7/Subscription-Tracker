using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Tenancy.GetAssignableRoles;

public sealed class GetAssignableRolesQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetAssignableRolesQuery, IReadOnlyList<AssignableRoleDto>>
{
    public async Task<Result<IReadOnlyList<AssignableRoleDto>>> Handle(
        GetAssignableRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles
            .Where(r => r.IsSystemRole || r.WorkspaceId == currentUserService.WorkspaceId)
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.Name)
            .Select(r => new AssignableRoleDto(r.Id, r.Name, r.Description))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AssignableRoleDto>>(roles);
    }
}
