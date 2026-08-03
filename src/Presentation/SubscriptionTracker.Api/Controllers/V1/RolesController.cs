using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Contracts.Tenancy;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Tenancy.Roles.CreateRole;
using SubscriptionTracker.Application.Tenancy.Roles.DeleteRole;
using SubscriptionTracker.Application.Tenancy.Roles.GetPermissionCatalog;
using SubscriptionTracker.Application.Tenancy.Roles.GetWorkspaceRoles;
using SubscriptionTracker.Application.Tenancy.Roles.UpdateRole;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize]
[HasPermission(Permissions.Workspace.ManageRoles)]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWorkspaceRoles(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkspaceRolesQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissionCatalog(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPermissionCatalogQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(request.Name, request.Description, request.PermissionCodes);
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedActionResult(this, nameof(GetWorkspaceRoles), id => new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateRoleCommand(id, request.Name, request.Description, request.PermissionCodes);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteRoleCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
