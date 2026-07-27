using System.Security.Claims;
using SubscriptionTracker.Application.Abstractions;

namespace SubscriptionTracker.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? WorkspaceId
    {
        get
        {
            var value = Principal?.FindFirstValue("workspace_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool HasPermission(string permissionCode) =>
        Principal?.Claims.Any(c => c.Type == "permission" && c.Value == permissionCode) ?? false;
}
