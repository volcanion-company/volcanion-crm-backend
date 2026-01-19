using CrmSaas.Api.Data;
using CrmSaas.Api.DTOs.Calendar;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Services;

public interface IActivityReminderService
{
    Task<ActivityReminderDto> CreateReminderAsync(CreateActivityReminderDto dto, CancellationToken cancellationToken = default);
    Task<List<ActivityReminderDto>> GetActivityRemindersAsync(string activityId, CancellationToken cancellationToken = default);
    Task<List<ActivityReminderDto>> GetUserRemindersAsync(string userId, CancellationToken cancellationToken = default);
    Task DeleteReminderAsync(string id, CancellationToken cancellationToken = default);
    
    // Background job method
    Task SendDueRemindersAsync(CancellationToken cancellationToken = default);
}

public class ActivityReminderService : IActivityReminderService
{
    private readonly TenantDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ActivityReminderService> _logger;

    public ActivityReminderService(
        TenantDbContext context,
        ICurrentUserService currentUser,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<ActivityReminderService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ActivityReminderDto> CreateReminderAsync(CreateActivityReminderDto dto, CancellationToken cancellationToken = default)
    {
        var activityGuid = Guid.Parse(dto.ActivityId);
        var activity = await _context.Activities
            .FirstOrDefaultAsync(a => a.Id == activityGuid, cancellationToken)
            ?? throw new KeyNotFoundException($"Activity {dto.ActivityId} not found");

        // Parse reminder type
        if (!Enum.TryParse<ActivityReminderType>(dto.Type, out var reminderType))
            throw new ArgumentException($"Invalid reminder type: {dto.Type}");

        // Calculate scheduled time
        var scheduledFor = CalculateScheduledTime(activity, reminderType, dto.MinutesBefore);

        var reminder = new ActivityReminder
        {
            ActivityId = activityGuid,
            Type = reminderType,
            MinutesBefore = dto.MinutesBefore,
            IsSent = false,
            ScheduledFor = scheduledFor,
            SendEmail = dto.SendEmail,
            SendNotification = dto.SendNotification,
            SendSms = dto.SendSms,
            RecipientUserId = activity.AssignedToUserId,
            RecipientEmail = dto.RecipientEmail,
            RecipientPhone = dto.RecipientPhone
        };

        _context.ActivityReminders.Add(reminder);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created activity reminder {ReminderId} for activity {ActivityId}, scheduled for {ScheduledTime}",
            reminder.Id, dto.ActivityId, scheduledFor);

        return MapToDto(reminder);
    }

    public async Task<List<ActivityReminderDto>> GetActivityRemindersAsync(string activityId, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(activityId);
        var reminders = await _context.ActivityReminders
            .Where(r => r.ActivityId == guid)
            .OrderBy(r => r.ScheduledFor)
            .ToListAsync(cancellationToken);

        return reminders.Select(MapToDto).ToList();
    }

    public async Task<List<ActivityReminderDto>> GetUserRemindersAsync(string userId, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(userId);
        var reminders = await _context.ActivityReminders
            .Where(r => r.RecipientUserId == guid && !r.IsSent && r.ScheduledFor <= DateTime.UtcNow.AddDays(7))
            .OrderBy(r => r.ScheduledFor)
            .ToListAsync(cancellationToken);

        return reminders.Select(MapToDto).ToList();
    }

    public async Task DeleteReminderAsync(string id, CancellationToken cancellationToken = default)
    {
        var guid = Guid.Parse(id);
        var reminder = await _context.ActivityReminders
            .FirstOrDefaultAsync(r => r.Id == guid, cancellationToken)
            ?? throw new KeyNotFoundException($"Activity reminder {id} not found");

        _context.ActivityReminders.Remove(reminder);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted activity reminder {ReminderId}", id);
    }

    public async Task SendDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        // Get reminders that are due (scheduled time has passed and not yet sent)
        var dueReminders = await _context.ActivityReminders
            .Include(r => r.Activity)
            .Where(r => !r.IsSent && r.ScheduledFor <= DateTime.UtcNow)
            .Take(100) // Process 100 at a time
            .ToListAsync(cancellationToken);

        if (!dueReminders.Any())
        {
            _logger.LogDebug("No due activity reminders found");
            return;
        }

        _logger.LogInformation("Processing {Count} due activity reminders", dueReminders.Count);

        foreach (var reminder in dueReminders)
        {
            try
            {
                await SendReminderAsync(reminder, cancellationToken);
                
                reminder.IsSent = true;
                reminder.SentAt = DateTime.UtcNow;
                
                _logger.LogInformation("Sent activity reminder {ReminderId} for activity {ActivityId}",
                    reminder.Id, reminder.ActivityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send activity reminder {ReminderId}", reminder.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SendReminderAsync(ActivityReminder reminder, CancellationToken cancellationToken)
    {
        if (reminder.Activity == null)
            throw new InvalidOperationException("Activity not loaded for reminder");

        var activity = reminder.Activity;
        var subject = $"Reminder: {activity.Subject}";
        var message = BuildReminderMessage(activity, reminder);

        // Send in-app notification
        if (reminder.SendNotification && reminder.RecipientUserId.HasValue)
        {
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                UserId = reminder.RecipientUserId.Value,
                Type = NotificationType.ActivityReminder,
                Title = subject,
                Message = message,
                Priority = (NotificationPriority)(int)activity.Priority,
                RelatedEntityType = "Activity",
                RelatedEntityId = activity.Id,
                Channels = NotificationChannel.InApp
            }, cancellationToken);
        }

        // Send email
        if (reminder.SendEmail && !string.IsNullOrEmpty(reminder.RecipientEmail))
        {
            await _emailService.SendEmailAsync(
                to: reminder.RecipientEmail,
                subject: subject,
                body: message,
                isHtml: true,
                cancellationToken: cancellationToken
            );
        }

        // Send SMS (placeholder - would need SMS service integration)
        if (reminder.SendSms && !string.IsNullOrEmpty(reminder.RecipientPhone))
        {
            _logger.LogWarning("SMS reminder requested but SMS service not implemented. Phone: {Phone}", reminder.RecipientPhone);
            // TODO: Implement SMS sending via Twilio or similar service
        }
    }

    private string BuildReminderMessage(Activity activity, ActivityReminder reminder)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<h3>Activity Reminder</h3>");
        sb.AppendLine($"<p><strong>Subject:</strong> {activity.Subject}</p>");
        sb.AppendLine($"<p><strong>Type:</strong> {activity.Type}</p>");
        sb.AppendLine($"<p><strong>Priority:</strong> {activity.Priority}</p>");
        
        if (activity.StartDate.HasValue)
            sb.AppendLine($"<p><strong>Start:</strong> {activity.StartDate:yyyy-MM-dd HH:mm}</p>");
        
        if (activity.DueDate.HasValue)
            sb.AppendLine($"<p><strong>Due:</strong> {activity.DueDate:yyyy-MM-dd HH:mm}</p>");
        
        if (!string.IsNullOrEmpty(activity.Description))
            sb.AppendLine($"<p><strong>Description:</strong> {activity.Description}</p>");

        sb.AppendLine($"<p><em>This reminder was scheduled {reminder.MinutesBefore} minutes before the activity.</em></p>");

        return sb.ToString();
    }

    private DateTime? CalculateScheduledTime(Activity activity, ActivityReminderType type, int minutesBefore)
    {
        var referenceTime = type switch
        {
            ActivityReminderType.ActivityStart => activity.StartDate,
            ActivityReminderType.ActivityDeadline => activity.DueDate,
            ActivityReminderType.Custom => activity.StartDate ?? activity.DueDate,
            _ => null
        };

        if (!referenceTime.HasValue)
            return null;

        return referenceTime.Value.AddMinutes(-minutesBefore);
    }

    private ActivityReminderDto MapToDto(ActivityReminder reminder)
    {
        return new ActivityReminderDto
        {
            Id = reminder.Id.ToString(),
            ActivityId = reminder.ActivityId.ToString(),
            Type = reminder.Type.ToString(),
            MinutesBefore = reminder.MinutesBefore,
            IsSent = reminder.IsSent,
            SentAt = reminder.SentAt,
            ScheduledFor = reminder.ScheduledFor,
            SendEmail = reminder.SendEmail,
            SendNotification = reminder.SendNotification,
            SendSms = reminder.SendSms,
            RecipientEmail = reminder.RecipientEmail,
            RecipientPhone = reminder.RecipientPhone
        };
    }
}
