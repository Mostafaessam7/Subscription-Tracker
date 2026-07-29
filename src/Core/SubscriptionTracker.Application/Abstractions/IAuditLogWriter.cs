using SubscriptionTracker.Domain.Auditing;

namespace SubscriptionTracker.Application.Abstractions;

/// <summary>
/// Stages an audit entry on the current DbContext's change tracker without saving - the entry is persisted
/// together with the command it describes by UnitOfWorkBehavior's single SaveChangesAsync call.
/// </summary>
public interface IAuditLogWriter
{
    void Stage(AuditLogEntry entry);
}
