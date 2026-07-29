namespace SubscriptionTracker.Application.Auditing.GetAuditLogs;

public sealed record AuditLogEntryDto(
    Guid Id,
    string? UserEmail,
    string Action,
    Guid? EntityId,
    bool IsSuccess,
    string? ErrorCode,
    string? Details,
    DateTimeOffset OccurredAtUtc);
