namespace CrmSaas.Api.Entities;

/// <summary>
/// Calendar sync configuration for integrating with external calendar systems
/// </summary>
public class CalendarSyncConfiguration : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }
    
    public required CalendarProvider Provider { get; set; }
    public required string ProviderAccountEmail { get; set; }
    
    // OAuth tokens (encrypted in production)
    public required string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    
    // Sync settings
    public bool IsActive { get; set; }
    public bool SyncToExternal { get; set; } // Push CRM activities to external calendar
    public bool SyncFromExternal { get; set; } // Pull external events to CRM
    public DateTime? LastSyncAt { get; set; }
    public CalendarSyncStatus Status { get; set; }
    public string? LastSyncError { get; set; }
    
    // Sync filters
    public string? CalendarIds { get; set; } // Comma-separated calendar IDs to sync
    public int SyncDaysBack { get; set; } = 30;
    public int SyncDaysForward { get; set; } = 90;
    
    // Statistics
    public int TotalEventsSynced { get; set; }
    public int TotalEventsCreated { get; set; }
    public int TotalEventsUpdated { get; set; }
    public int FailedSyncCount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Mapping between CRM activities and external calendar events
/// </summary>
public class CalendarEventMapping : BaseEntity
{
    public required Guid CalendarSyncConfigurationId { get; set; }
    public CalendarSyncConfiguration? CalendarSyncConfiguration { get; set; }
    
    // CRM side
    public Guid? ActivityId { get; set; }
    public Activity? Activity { get; set; }
    
    // External calendar side
    public required string ExternalEventId { get; set; }
    public required string ExternalCalendarId { get; set; }
    public required CalendarProvider Provider { get; set; }
    
    // Sync metadata
    public required CalendarSyncDirection Direction { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public string? SyncHash { get; set; } // Hash to detect changes
    public CalendarEventSyncStatus SyncStatus { get; set; }
    public string? LastSyncError { get; set; }
    
    // Event details snapshot (for conflict resolution)
    public required string EventTitle { get; set; }
    public DateTime EventStartAt { get; set; }
    public DateTime EventEndAt { get; set; }
    public bool IsAllDay { get; set; }
}

/// <summary>
/// Activity reminder settings
/// </summary>
public class ActivityReminder : BaseEntity
{
    public required Guid ActivityId { get; set; }
    public Activity? Activity { get; set; }
    
    public required ActivityReminderType Type { get; set; }
    public required int MinutesBefore { get; set; } // 15, 30, 60, 1440 (1 day), etc.
    
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    
    // Reminder channels
    public bool SendEmail { get; set; }
    public bool SendNotification { get; set; }
    public bool SendSms { get; set; }
    
    // Recipient
    public Guid? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
}

public enum CalendarProvider
{
    GoogleCalendar,
    MicrosoftOutlook,
    Microsoft365,
    AppleCalendar,
    Other
}

public enum CalendarSyncStatus
{
    Active,
    Paused,
    Error,
    TokenExpired,
    Disconnected
}

public enum CalendarSyncDirection
{
    CrmToExternal, // CRM activity → External calendar event
    ExternalToCrm, // External event → CRM activity
    Bidirectional  // Two-way sync
}

public enum CalendarEventSyncStatus
{
    Synced,
    PendingSync,
    SyncFailed,
    Conflict,
    Deleted
}

public enum ActivityReminderType
{
    ActivityStart,
    ActivityDeadline,
    Custom
}
