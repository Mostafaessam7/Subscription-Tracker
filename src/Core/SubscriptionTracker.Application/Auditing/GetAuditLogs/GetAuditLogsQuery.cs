using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Models;

namespace SubscriptionTracker.Application.Auditing.GetAuditLogs;

public sealed record GetAuditLogsQuery(int PageNumber = 1, int PageSize = 20) : IQuery<PagedList<AuditLogEntryDto>>;
