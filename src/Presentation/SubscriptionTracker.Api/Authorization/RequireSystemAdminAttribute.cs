using Microsoft.AspNetCore.Authorization;

namespace SubscriptionTracker.Api.Authorization;

public sealed class RequireSystemAdminAttribute : AuthorizeAttribute
{
    public RequireSystemAdminAttribute() => Policy = DependencyInjection.SystemAdminPolicy;
}
