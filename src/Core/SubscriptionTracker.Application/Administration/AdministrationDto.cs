namespace SubscriptionTracker.Application.Administration;

public sealed record AdminWorkspaceSummaryDto(
    Guid Id, string Name, string OwnerEmail, int MemberCount, DateTimeOffset CreatedAtUtc);

public sealed record AdminUserSummaryDto(
    Guid Id, string Email, string FirstName, string LastName, string Status,
    bool IsSystemAdmin, bool IsEmailVerified, DateTimeOffset CreatedAtUtc);

public sealed record SystemHealthDto(
    int TotalUsers, int TotalWorkspaces, int TotalSubscriptions, int ActiveSubscriptions, int TotalBudgets);
