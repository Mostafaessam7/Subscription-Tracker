using Microsoft.AspNetCore.Authorization;

namespace SubscriptionTracker.Api.Authorization;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionCode)
    {
        Policy = $"Permission:{permissionCode}";
    }
}
