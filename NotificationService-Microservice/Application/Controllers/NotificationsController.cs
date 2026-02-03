using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using NotificationService.Application.Features.Notifications.Commands.MarkAsRead;
using NotificationService.Application.Features.Notifications.Commands.MarkAllAsRead;
using NotificationService.Application.Features.Notifications.Commands.DeleteNotification;
using NotificationService.Application.Features.Notifications.Queries.GetUserNotifications;
using NotificationService.Application.Features.Notifications.Queries.GetNotificationById;
using NotificationService.Application.Features.Notifications.Queries.GetUnreadCount;

namespace NotificationService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get current user's notifications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead, [FromQuery] int take = 50)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetUserNotificationsQuery
        {
            UserId = _currentUserService.UserId.Value,
            IsRead = isRead,
            Take = take,
            IsStaff = _currentUserService.IsInRole("Staff")
        };

        var notifications = await _mediator.Send(query);
        return Ok(notifications);
    }

    /// <summary>
    /// Get notification by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotificationById(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetNotificationByIdQuery
        {
            NotificationId = id,
            UserId = _currentUserService.UserId.Value
        };

        var notification = await _mediator.Send(query);
        
        if (notification == null)
        {
            return NotFound();
        }

        return Ok(notification);
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetUnreadCountQuery
        {
            UserId = _currentUserService.UserId.Value,
            IsStaff = _currentUserService.IsInRole("Staff")
        };

        var count = await _mediator.Send(query);
        return Ok(new { count });
    }

    /// <summary>
    /// Create a new notification
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationCommand command)
    {
        var notificationId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetNotificationById), new { id = notificationId }, new { id = notificationId });
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPut("{id}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var command = new MarkNotificationAsReadCommand
        {
            NotificationId = id,
            UserId = _currentUserService.UserId.Value
        };

        var result = await _mediator.Send(command);
        
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var command = new MarkAllAsReadCommand
        {
            UserId = _currentUserService.UserId.Value
        };

        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var command = new DeleteNotificationCommand
        {
            NotificationId = id,
            UserId = _currentUserService.UserId.Value
        };

        var result = await _mediator.Send(command);
        
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
