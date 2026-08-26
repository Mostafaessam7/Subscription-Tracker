using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Administration.DisableUser;
using SubscriptionTracker.Application.Administration.EnableUser;
using SubscriptionTracker.Application.Administration.GetAllUsers;
using SubscriptionTracker.Application.Administration.GetAllWorkspaces;
using SubscriptionTracker.Application.Administration.GetSystemHealth;
using SubscriptionTracker.Application.Administration.TriggerBackgroundJob;

namespace SubscriptionTracker.Api.Controllers.V1;

/// <summary>Cross-tenant system administration - not scoped to any single workspace, gated by
/// [RequireSystemAdmin] (the `system_admin` JWT claim) rather than a per-workspace permission.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize]
[RequireSystemAdmin]
public sealed class AdminController(ISender sender) : ControllerBase
{
    [HttpGet("workspaces")]
    public async Task<IActionResult> GetAllWorkspaces(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllWorkspacesQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllUsersQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("users/{id:guid}/disable")]
    public async Task<IActionResult> DisableUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DisableUserCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("users/{id:guid}/enable")]
    public async Task<IActionResult> EnableUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new EnableUserCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetSystemHealth(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSystemHealthQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Fires one of the scheduled Quartz jobs (renewal-reminder, auto-renewal,
    /// expire-subscriptions, budget-alert, purge-soft-deleted) immediately instead of waiting for its cron
    /// schedule - mainly useful for verifying the in-app/email notification paths on demand.</summary>
    [HttpPost("jobs/{jobName}/trigger")]
    public async Task<IActionResult> TriggerJob(string jobName, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new TriggerBackgroundJobCommand(jobName), cancellationToken);
        return result.ToActionResult(this);
    }
}
