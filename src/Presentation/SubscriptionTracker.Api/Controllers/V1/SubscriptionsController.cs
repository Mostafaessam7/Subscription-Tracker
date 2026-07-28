using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Contracts.Subscriptions;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Application.Subscriptions.CancelSubscription;
using SubscriptionTracker.Application.Subscriptions.CreateSubscription;
using SubscriptionTracker.Application.Subscriptions.DeleteAttachment;
using SubscriptionTracker.Application.Subscriptions.DownloadAttachment;
using SubscriptionTracker.Application.Subscriptions.GetSubscriptionById;
using SubscriptionTracker.Application.Subscriptions.GetSubscriptions;
using SubscriptionTracker.Application.Subscriptions.PauseSubscription;
using SubscriptionTracker.Application.Subscriptions.ResumeSubscription;
using SubscriptionTracker.Application.Subscriptions.UpdateSubscription;
using SubscriptionTracker.Application.Subscriptions.UploadAttachment;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscriptions")]
[Authorize]
public sealed class SubscriptionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Subscriptions.View)]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? tagId = null,
        [FromQuery] SubscriptionStatus? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSubscriptionsQuery(pageNumber, pageSize, searchTerm, categoryId, tagId, status, sortBy, sortDescending);
        var result = await sender.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Subscriptions.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSubscriptionByIdQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [HasPermission(Permissions.Subscriptions.Create)]
    public async Task<IActionResult> Create(CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSubscriptionCommand(
            request.Name, request.Provider, request.LogoUrl, request.WebsiteUrl, request.Notes,
            request.CategoryId, request.PaymentMethodId, request.Amount, request.CurrencyCode,
            request.BillingFrequency, request.CustomIntervalDays, request.StartDate, request.TrialEndDate,
            request.AutoRenewal, request.TagIds);

        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedActionResult(this, nameof(GetById), id => new { id });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Subscriptions.Edit)]
    public async Task<IActionResult> Update(Guid id, UpdateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSubscriptionCommand(
            id, request.Name, request.Provider, request.LogoUrl, request.WebsiteUrl, request.Notes,
            request.CategoryId, request.PaymentMethodId, request.Amount, request.CurrencyCode, request.TagIds);

        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Subscriptions.Cancel)]
    public async Task<IActionResult> Cancel(Guid id, CancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelSubscriptionCommand(id, request.EffectiveDate, request.Reason), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/pause")]
    [HasPermission(Permissions.Subscriptions.Edit)]
    public async Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PauseSubscriptionCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/resume")]
    [HasPermission(Permissions.Subscriptions.Edit)]
    public async Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResumeSubscriptionCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/attachments")]
    [HasPermission(Permissions.Subscriptions.Edit)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var command = new UploadAttachmentCommand(id, file.FileName, file.ContentType, stream.ToArray());
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [HasPermission(Permissions.Subscriptions.View)]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DownloadAttachmentQuery(id, attachmentId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [HasPermission(Permissions.Subscriptions.Edit)]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteAttachmentCommand(id, attachmentId), cancellationToken);
        return result.ToActionResult(this);
    }
}
