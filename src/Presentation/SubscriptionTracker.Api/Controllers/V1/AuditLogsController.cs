using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Auditing.GetAuditLogs;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize]
public sealed class AuditLogsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Workspace.ManageSettings)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int pageNumber, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var query = new GetAuditLogsQuery(
            pageNumber == 0 ? 1 : pageNumber,
            pageSize == 0 ? 20 : pageSize);
        var result = await sender.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
