using Microsoft.AspNetCore.Authorization;

namespace SubscriptionTracker.Api.Authorization;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
