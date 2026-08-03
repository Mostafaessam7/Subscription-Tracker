using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Notifications.GetMyNotifications;
using SubscriptionTracker.Application.Notifications.GetUnreadNotificationCount;
using SubscriptionTracker.Application.Notifications.MarkAllNotificationsAsRead;
using SubscriptionTracker.Application.Notifications.MarkNotificationAsRead;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int pageNumber, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var query = new GetMyNotificationsQuery(pageNumber == 0 ? 1 : pageNumber, pageSize == 0 ? 20 : pageSize);
        var result = await sender.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return result.ToActionResult(this);
    }
}
