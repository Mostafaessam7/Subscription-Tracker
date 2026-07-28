using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Identity.GetSessions;
using SubscriptionTracker.Application.Identity.RevokeSession;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sessions")]
[Authorize]
public sealed class SessionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSessionsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await sender.Send(new RevokeSessionCommand(id, ipAddress), cancellationToken);
        return result.ToActionResult(this);
    }
}
