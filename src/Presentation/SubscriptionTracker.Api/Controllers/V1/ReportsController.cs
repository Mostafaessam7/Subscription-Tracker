using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Application.Reports.ExportSubscriptionsCsv;
using SubscriptionTracker.Application.Reports.ExportSubscriptionsExcel;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("subscriptions/csv")]
    [HasPermission(Permissions.Reports.Export)]
    public async Task<IActionResult> ExportSubscriptionsCsv(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? tagId,
        [FromQuery] SubscriptionStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ExportSubscriptionsCsvQuery(searchTerm, categoryId, tagId, status), cancellationToken);

        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : NotFound();
    }

    [HttpGet("subscriptions/excel")]
    [HasPermission(Permissions.Reports.Export)]
    public async Task<IActionResult> ExportSubscriptionsExcel(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? tagId,
        [FromQuery] SubscriptionStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ExportSubscriptionsExcelQuery(searchTerm, categoryId, tagId, status), cancellationToken);

        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName)
            : NotFound();
    }
}
