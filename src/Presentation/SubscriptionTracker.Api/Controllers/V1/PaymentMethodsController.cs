using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Contracts.Catalog;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Catalog.PaymentMethods.CreatePaymentMethod;
using SubscriptionTracker.Application.Catalog.PaymentMethods.DeletePaymentMethod;
using SubscriptionTracker.Application.Catalog.PaymentMethods.GetPaymentMethods;
using SubscriptionTracker.Application.Catalog.PaymentMethods.UpdatePaymentMethod;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payment-methods")]
[Authorize]
public sealed class PaymentMethodsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Catalog.View)]
    public async Task<IActionResult> GetPaymentMethods(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentMethodsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Create(CreatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePaymentMethodCommand(request.Type, request.Label, request.MaskedDetails, request.IsDefault);
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedActionResult(this, nameof(GetPaymentMethods), id => new { id });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePaymentMethodCommand(id, request.Label, request.IsDefault);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Catalog.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeletePaymentMethodCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
