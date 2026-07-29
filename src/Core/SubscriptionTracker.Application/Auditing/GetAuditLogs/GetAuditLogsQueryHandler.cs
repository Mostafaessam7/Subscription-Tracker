using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Models;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Auditing.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetAuditLogsQuery, PagedList<AuditLogEntryDto>>
{
    public async Task<Result<PagedList<AuditLogEntryDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogs
            .Where(e => e.WorkspaceId == currentUserService.WorkspaceId)
            .OrderByDescending(e => e.OccurredAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new AuditLogEntryDto(e.Id, e.UserEmail, e.Action, e.EntityId, e.IsSuccess, e.ErrorCode, e.Details, e.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedList<AuditLogEntryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
