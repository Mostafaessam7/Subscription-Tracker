namespace SubscriptionTracker.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? WorkspaceId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permissionCode);
}
