using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Auditing;

namespace SubscriptionTracker.Infrastructure.Persistence;

internal sealed class AuditLogWriter(ApplicationDbContext dbContext) : IAuditLogWriter
{
    public void Stage(AuditLogEntry entry) => dbContext.AuditLogs.Add(entry);
}
