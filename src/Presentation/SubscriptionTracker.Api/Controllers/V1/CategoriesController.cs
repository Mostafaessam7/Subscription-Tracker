using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Contracts.Catalog;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Catalog.Categories.CreateCategory;
using SubscriptionTracker.Application.Catalog.Categories.DeleteCategory;
using SubscriptionTracker.Application.Catalog.Categories.GetCategories;
using SubscriptionTracker.Application.Catalog.Categories.UpdateCategory;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/categories")]
[Authorize]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Catalog.View)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCategoriesQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(request.Name, request.Color, request.Icon);
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedActionResult(this, nameof(GetCategories), id => new { id });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(id, request.Name, request.Color, request.Icon);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
