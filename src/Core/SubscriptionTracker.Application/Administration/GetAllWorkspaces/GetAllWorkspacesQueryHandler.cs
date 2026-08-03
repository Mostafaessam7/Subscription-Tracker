using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Administration.GetAllWorkspaces;

/// <summary>
/// Deliberately bypasses the workspace-scoped abstraction other query handlers use (dbContext.Workspaces/Users
/// are never tenant-filtered by ApplicationDbContext's global query filters - see that class's
/// ApplyTenantIsolationFilters for why - so this is a plain unrestricted read, gated at the API layer by
/// [RequireSystemAdmin] instead of by workspace membership).
/// </summary>
public sealed class GetAllWorkspacesQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetAllWorkspacesQuery, IReadOnlyList<AdminWorkspaceSummaryDto>>
{
    public async Task<Result<IReadOnlyList<AdminWorkspaceSummaryDto>>> Handle(
        GetAllWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var workspaces = await dbContext.Workspaces
            .Select(w => new { w.Id, w.Name, w.OwnerId, w.CreatedAtUtc, MemberCount = w.Members.Count })
            .ToListAsync(cancellationToken);

        var ownerIds = workspaces.Select(w => w.OwnerId).Distinct().ToList();
        var ownerEmails = await dbContext.Users
            .Where(u => ownerIds.Contains(u.Id))
            .Select(u => new { u.Id, Email = u.Email.Value })
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var summaries = workspaces
            .Select(w => new AdminWorkspaceSummaryDto(
                w.Id, w.Name, ownerEmails.GetValueOrDefault(w.OwnerId, "unknown"), w.MemberCount, w.CreatedAtUtc))
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToList();

        return Result.Success<IReadOnlyList<AdminWorkspaceSummaryDto>>(summaries);
    }
}
