using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CrmSaas.Api.Services;

public interface INotificationService
{
    Task SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task SendFromTemplateAsync(string templateCode, Guid userId, Dictionary<string, string> placeholders, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, int pageSize = 50, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteOldNotificationsAsync(int daysOld = 30, CancellationToken cancellationToken = default);
}

public class NotificationService : INotificationService
{
    private readonly TenantDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        TenantDbContext context,
        IEmailService emailService,
        ICurrentUserService currentUserService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check user preferences
            var preferences = await GetUserPreferencesAsync(request.UserId, request.Type, cancellationToken);
            if (!preferences.IsEnabled)
            {
                _logger.LogDebug("Notification type {Type} is disabled for user {UserId}", request.Type, request.UserId);
                return;
            }

            // Apply user channel preferences
            var channels = request.Channels & preferences.EnabledChannels;
            if (channels == NotificationChannel.None)
            {
                _logger.LogDebug("No enabled channels for user {UserId}", request.UserId);
                return;
            }

            // Check quiet hours
            if (IsInQuietHours(preferences))
            {
                channels &= ~(NotificationChannel.Sms | NotificationChannel.Push); // Only allow InApp & Email during quiet hours
                _logger.LogDebug("User {UserId} is in quiet hours, limited channels", request.UserId);
            }

            // Create notification record
            var notification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Priority = request.Priority,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                ActionUrl = request.ActionUrl,
                Data = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
                Channels = channels,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            // Send through channels
            await DeliverNotificationAsync(notification, request.EmailData, cancellationToken);

            _logger.LogInformation("Notification {NotificationId} sent to user {UserId} via {Channels}",
                notification.Id, request.UserId, channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task SendFromTemplateAsync(
        string templateCode,
        Guid userId,
        Dictionary<string, string> placeholders,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive, cancellationToken);

        if (template == null)
        {
            _logger.LogWarning("Notification template {TemplateCode} not found", templateCode);
            return;
        }

        var title = ReplacePlaceholders(template.SubjectTemplate, placeholders);
        var message = ReplacePlaceholders(template.BodyTemplate, placeholders);
        var smsText = !string.IsNullOrEmpty(template.SmsTemplate)
            ? ReplacePlaceholders(template.SmsTemplate, placeholders)
            : null;

        var request = new NotificationRequest
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = template.Type,
            Priority = template.Priority,
            Channels = template.DefaultChannels,
            EmailData = new EmailData
            {
                Subject = title,
                Body = message,
                IsHtml = true
            }
        };

        await SendNotificationAsync(request, cancellationToken);
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(
        Guid userId,
        bool unreadOnly = false,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        if (unreadNotifications.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task DeleteOldNotificationsAsync(int daysOld = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var oldNotifications = await _context.Notifications
            .Where(n => n.CreatedAt < cutoffDate || (n.ExpiresAt != null && n.ExpiresAt < DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        _context.Notifications.RemoveRange(oldNotifications);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} old notifications", oldNotifications.Count);
    }

    #region Private Methods

    private async Task<UserNotificationPreference> GetUserPreferencesAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var preference = await _context.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type, cancellationToken);

        // Return default preferences if not set
        return preference ?? new UserNotificationPreference
        {
            UserId = userId,
            NotificationType = type,
            IsEnabled = true,
            EnabledChannels = NotificationChannel.InApp | NotificationChannel.Email
        };
    }

    private bool IsInQuietHours(UserNotificationPreference preferences)
    {
        if (string.IsNullOrEmpty(preferences.QuietHoursStart) ||
            string.IsNullOrEmpty(preferences.QuietHoursEnd))
        {
            return false;
        }

        var now = DateTime.UtcNow.TimeOfDay;
        if (TimeSpan.TryParse(preferences.QuietHoursStart, out var start) &&
            TimeSpan.TryParse(preferences.QuietHoursEnd, out var end))
        {
            if (start < end)
            {
                return now >= start && now < end;
            }
            else // Overnight quiet hours
            {
                return now >= start || now < end;
            }
        }

        return false;
    }

    private async Task DeliverNotificationAsync(
        Notification notification,
        EmailData? emailData,
        CancellationToken cancellationToken)
    {
        // InApp notification is already saved in database

        // Send email if enabled
        if (notification.Channels.HasFlag(NotificationChannel.Email))
        {
            try
            {
                var user = await _context.Users.FindAsync(new object[] { notification.UserId }, cancellationToken);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        emailData?.Subject ?? notification.Title,
                        emailData?.Body ?? notification.Message ?? string.Empty,
                        emailData?.IsHtml ?? false,
                        cancellationToken);

                    notification.EmailStatus = DeliveryStatus.Sent;
                }
                else
                {
                    notification.EmailStatus = DeliveryStatus.Skipped;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email for notification {NotificationId}", notification.Id);
                notification.EmailStatus = DeliveryStatus.Failed;
            }
        }

        // Send SMS if enabled (placeholder for future implementation)
        if (notification.Channels.HasFlag(NotificationChannel.Sms))
        {
            notification.SmsStatus = DeliveryStatus.Skipped; // Not implemented yet
        }

        // Send Push if enabled (placeholder for future implementation)
        if (notification.Channels.HasFlag(NotificationChannel.Push))
        {
            notification.PushStatus = DeliveryStatus.Skipped; // Not implemented yet
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var placeholder in placeholders)
        {
            result = Regex.Replace(result, $@"\{{\{{{placeholder.Key}\}}\}}", placeholder.Value, RegexOptions.IgnoreCase);
        }
        return result;
    }

    #endregion
}

#region Models

public class NotificationRequest
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationChannel Channels { get; set; } = NotificationChannel.InApp;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? ActionUrl { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public EmailData? EmailData { get; set; }
}

public class EmailData
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
}

#endregion
