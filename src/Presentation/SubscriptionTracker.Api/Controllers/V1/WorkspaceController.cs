using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Contracts.Tenancy;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Tenancy.AcceptInvitation;
using SubscriptionTracker.Application.Tenancy.ChangeMemberRole;
using SubscriptionTracker.Application.Tenancy.GetAssignableRoles;
using SubscriptionTracker.Application.Tenancy.GetMyWorkspace;
using SubscriptionTracker.Application.Tenancy.GetPendingInvitations;
using SubscriptionTracker.Application.Tenancy.InviteMember;
using SubscriptionTracker.Application.Tenancy.RemoveMember;
using SubscriptionTracker.Application.Tenancy.UpdateWorkspaceSettings;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/workspace")]
[Authorize]
public sealed class WorkspaceController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyWorkspace(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyWorkspaceQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("assignable-roles")]
    public async Task<IActionResult> GetAssignableRoles(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAssignableRolesQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> GetPendingInvitations(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPendingInvitationsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("settings")]
    [HasPermission(Permissions.Workspace.ManageSettings)]
    public async Task<IActionResult> UpdateSettings(UpdateWorkspaceSettingsRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateWorkspaceSettingsCommand(request.DefaultCurrencyCode, request.TimeZoneId, request.Locale);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("members")]
    [HasPermission(Permissions.Workspace.ManageMembers)]
    public async Task<IActionResult> InviteMember(InviteMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new InviteMemberCommand(request.Email, request.RoleId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("members/{memberId:guid}/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AcceptInvitationCommand(memberId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("members/{memberId:guid}/role")]
    [HasPermission(Permissions.Workspace.ManageMembers)]
    public async Task<IActionResult> ChangeMemberRole(Guid memberId, ChangeMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeMemberRoleCommand(memberId, request.RoleId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("members/{memberId:guid}")]
    [HasPermission(Permissions.Workspace.ManageMembers)]
    public async Task<IActionResult> RemoveMember(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveMemberCommand(memberId), cancellationToken);
        return result.ToActionResult(this);
    }
}
