using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Auditing;

/// <summary>
/// Immutable record of a mutating command's outcome, staged by AuditLoggingBehavior and persisted in the same
/// SaveChanges call as the command it describes (see IAuditLogWriter). Never mutated after creation.
/// </summary>
public sealed class AuditLogEntry : AggregateRoot<Guid>
{
    private AuditLogEntry(
        Guid id,
        Guid? workspaceId,
        Guid? userId,
        string? userEmail,
        string action,
        Guid? entityId,
        bool isSuccess,
        string? errorCode,
        string? details,
        DateTimeOffset occurredAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        UserEmail = userEmail;
        Action = action;
        EntityId = entityId;
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Details = details;
        OccurredAtUtc = occurredAtUtc;
    }

    private AuditLogEntry()
    {
    }

    public Guid? WorkspaceId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditLogEntry Create(
        Guid? workspaceId,
        Guid? userId,
        string? userEmail,
        string action,
        Guid? entityId,
        bool isSuccess,
        string? errorCode,
        string? details,
        DateTimeOffset occurredAtUtc) =>
        new(Guid.NewGuid(), workspaceId, userId, userEmail, action, entityId, isSuccess, errorCode, details, occurredAtUtc);
}
