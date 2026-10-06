namespace Bookline.Api.Controllers;

using Bookline.Application.Notifications.DTOs;
using Bookline.Application.Notifications.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationHandlers _handlers;

    public NotificationsController(NotificationHandlers handlers)
    {
        _handlers = handlers;
    }

    [HttpGet("logs")]
    public async Task<ActionResult<Bookline.Application.Common.Models.PagedResult<NotificationLogDto>>> GetLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? channel = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _handlers.Handle(
            new GetNotificationLogsQuery(pageNumber, pageSize, status, channel),
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<NotificationSettingDto>> GetSettings(CancellationToken cancellationToken = default)
    {
        var result = await _handlers.Handle(new GetNotificationSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("settings")]
    public async Task<ActionResult<NotificationSettingDto>> UpdateSettings(
        [FromBody] UpdateNotificationSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _handlers.Handle(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("send-test")]
    public async Task<ActionResult> SendTest(
        [FromBody] SendTestNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        await _handlers.Handle(command, cancellationToken);
        return Ok(new { message = "Test notification dispatched successfully." });
    }
}
