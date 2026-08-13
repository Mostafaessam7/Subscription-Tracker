using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Dashboard.GetDashboardSummary;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet("summary")]
    [HasPermission(Permissions.Subscriptions.View)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return result.ToActionResult(this);
    }
}
