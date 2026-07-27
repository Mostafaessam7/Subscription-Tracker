using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Contracts.Catalog;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Catalog.Tags.CreateTag;
using SubscriptionTracker.Application.Catalog.Tags.DeleteTag;
using SubscriptionTracker.Application.Catalog.Tags.GetTags;
using SubscriptionTracker.Application.Catalog.Tags.UpdateTag;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tags")]
[Authorize]
public sealed class TagsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Catalog.View)]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTagsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Create(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTagCommand(request.Name, request.Color);
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedActionResult(this, nameof(GetTags), id => new { id });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateTagRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTagCommand(id, request.Name, request.Color);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteTagCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
