namespace CrmSaas.Api.DTOs.Calendar;

/// <summary>
/// Calendar sync configuration DTO
/// </summary>
public class CalendarSyncConfigurationDto
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ProviderAccountEmail { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool SyncToExternal { get; set; }
    public bool SyncFromExternal { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string Status { get; set; } = null!;
    public string? LastSyncError { get; set; }
    public List<string>? CalendarIds { get; set; }
    public int SyncDaysBack { get; set; }
    public int SyncDaysForward { get; set; }
    public int TotalEventsSynced { get; set; }
    public int TotalEventsCreated { get; set; }
    public int TotalEventsUpdated { get; set; }
    public int FailedSyncCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create calendar sync configuration
/// </summary>
public class CreateCalendarSyncConfigurationDto
{
    public required string Provider { get; set; } // "GoogleCalendar", "MicrosoftOutlook", "Microsoft365"
    public required string AuthorizationCode { get; set; } // OAuth authorization code
    public string? RedirectUri { get; set; }
    public bool SyncToExternal { get; set; } = true;
    public bool SyncFromExternal { get; set; } = true;
    public List<string>? CalendarIds { get; set; }
    public int SyncDaysBack { get; set; } = 30;
    public int SyncDaysForward { get; set; } = 90;
}

/// <summary>
/// Update calendar sync configuration
/// </summary>
public class UpdateCalendarSyncConfigurationDto
{
    public bool? IsActive { get; set; }
    public bool? SyncToExternal { get; set; }
    public bool? SyncFromExternal { get; set; }
    public List<string>? CalendarIds { get; set; }
    public int? SyncDaysBack { get; set; }
    public int? SyncDaysForward { get; set; }
}

/// <summary>
/// Calendar event mapping DTO
/// </summary>
public class CalendarEventMappingDto
{
    public string Id { get; set; } = null!;
    public string? ActivityId { get; set; }
    public string ExternalEventId { get; set; } = null!;
    public string ExternalCalendarId { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string Direction { get; set; } = null!;
    public DateTime LastSyncedAt { get; set; }
    public string SyncStatus { get; set; } = null!;
    public string? LastSyncError { get; set; }
    public string EventTitle { get; set; } = null!;
    public DateTime EventStartAt { get; set; }
    public DateTime EventEndAt { get; set; }
    public bool IsAllDay { get; set; }
}

/// <summary>
/// Activity reminder DTO
/// </summary>
public class ActivityReminderDto
{
    public string Id { get; set; } = null!;
    public string ActivityId { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int MinutesBefore { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public bool SendEmail { get; set; }
    public bool SendNotification { get; set; }
    public bool SendSms { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
}

/// <summary>
/// Create activity reminder
/// </summary>
public class CreateActivityReminderDto
{
    public required string ActivityId { get; set; }
    public required string Type { get; set; } // "ActivityStart", "ActivityDeadline", "Custom"
    public required int MinutesBefore { get; set; }
    public bool SendEmail { get; set; } = true;
    public bool SendNotification { get; set; } = true;
    public bool SendSms { get; set; } = false;
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
}

/// <summary>
/// Calendar sync result
/// </summary>
public class CalendarSyncResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int EventsCreated { get; set; }
    public int EventsUpdated { get; set; }
    public int EventsDeleted { get; set; }
    public int EventsFailed { get; set; }
    public DateTime SyncedAt { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// iCal export options
/// </summary>
public class ICalExportOptionsDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? ActivityIds { get; set; }
    public string? ActivityType { get; set; } // "Call", "Meeting", "Task", etc.
    public string? AssignedToUserId { get; set; }
    public bool IncludeCompleted { get; set; } = false;
}

/// <summary>
/// OAuth authorization URL response
/// </summary>
public class CalendarAuthorizationUrlDto
{
    public required string AuthorizationUrl { get; set; }
    public required string State { get; set; }
}

/// <summary>
/// Calendar list item
/// </summary>
public class ExternalCalendarDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
    public string? TimeZone { get; set; }
    public string? BackgroundColor { get; set; }
}
