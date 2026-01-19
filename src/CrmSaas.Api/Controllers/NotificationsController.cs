using Asp.Versioning;
using CrmSaas.Api.Common;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSaas.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's notifications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId ?? Guid.Empty;
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("User not authenticated"));
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(
                userId, unreadOnly, pageSize, cancellationToken);

            return Ok(ApiResponse.Ok(notifications));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve notifications"));
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId ?? Guid.Empty;
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("User not authenticated"));
            }

            var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);

            return Ok(ApiResponse.Ok(new { count }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve unread count"));
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.MarkAsReadAsync(id, cancellationToken);
            return Ok(ApiResponse.Ok("Notification marked as read"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, ApiResponse.Fail("Failed to mark notification as read"));
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId ?? Guid.Empty;
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("User not authenticated"));
            }

            await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
            return Ok(ApiResponse.Ok("All notifications marked as read"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, ApiResponse.Fail("Failed to mark all notifications as read"));
        }
    }

    /// <summary>
    /// Send test notification (admin only)
    /// </summary>
    [HttpPost("test")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> SendTestNotification(
        [FromBody] TestNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = request.UserId ?? _currentUserService.UserId ?? Guid.Empty;

            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                UserId = userId,
                Title = request.Title ?? "Test Notification",
                Message = request.Message ?? "This is a test notification",
                Type = NotificationType.System,
                Priority = NotificationPriority.Normal,
                Channels = request.Channels ?? NotificationChannel.InApp
            }, cancellationToken);

            return Ok(ApiResponse.Ok("Test notification sent"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return StatusCode(500, ApiResponse.Fail("Failed to send test notification"));
        }
    }
}

public class TestNotificationRequest
{
    public Guid? UserId { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public NotificationChannel? Channels { get; set; }
}
