using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Contracts.Budgets;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Budgets.CreateBudget;
using SubscriptionTracker.Application.Budgets.DeleteBudget;
using SubscriptionTracker.Application.Budgets.GetBudgets;
using SubscriptionTracker.Application.Budgets.UpdateBudget;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/budgets")]
[Authorize]
public sealed class BudgetsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Budgets.View)]
    public async Task<IActionResult> GetBudgets(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBudgetsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [HasPermission(Permissions.Budgets.Manage)]
    public async Task<IActionResult> Create(CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateBudgetCommand(
            request.Name, request.Amount, request.CurrencyCode, request.Period, request.CategoryId, request.AlertThresholdPercentage);
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedActionResult(this, nameof(GetBudgets), id => new { id });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Budgets.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateBudgetCommand(id, request.Amount, request.CurrencyCode, request.AlertThresholdPercentage);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Budgets.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBudgetCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
