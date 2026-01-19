using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

/// <summary>
/// Notification sent to users
/// </summary>
public class Notification : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Message { get; set; }

    public NotificationType Type { get; set; }

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// User who should receive this notification
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Related entity type (e.g., "Opportunity", "Ticket")
    /// </summary>
    [MaxLength(100)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Related entity ID for navigation
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Action URL or route to navigate to
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Additional data as JSON
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Has user read this notification?
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When user marked as read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Delivery channels used
    /// </summary>
    public NotificationChannel Channels { get; set; }

    /// <summary>
    /// Email delivery status
    /// </summary>
    public DeliveryStatus EmailStatus { get; set; } = DeliveryStatus.Pending;

    /// <summary>
    /// SMS delivery status
    /// </summary>
    public DeliveryStatus SmsStatus { get; set; } = DeliveryStatus.Pending;

    /// <summary>
    /// Push notification delivery status
    /// </summary>
    public DeliveryStatus PushStatus { get; set; } = DeliveryStatus.Pending;

    /// <summary>
    /// When notification expires (for cleanup)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}

/// <summary>
/// Notification templates for consistent messaging
/// </summary>
public class NotificationTemplate : TenantAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public NotificationType Type { get; set; }

    /// <summary>
    /// Subject/Title template with placeholders
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Message body template with placeholders (supports HTML for email)
    /// </summary>
    [Required]
    public string BodyTemplate { get; set; } = string.Empty;

    /// <summary>
    /// SMS template (plain text only)
    /// </summary>
    [MaxLength(500)]
    public string? SmsTemplate { get; set; }

    /// <summary>
    /// Default channels to send
    /// </summary>
    public NotificationChannel DefaultChannels { get; set; } = NotificationChannel.InApp;

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Available placeholders as JSON array (e.g., ["UserName", "OpportunityName"])
    /// </summary>
    public string? AvailablePlaceholders { get; set; }
}

/// <summary>
/// User notification preferences
/// </summary>
public class UserNotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }

    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// Enabled delivery channels for this notification type
    /// </summary>
    public NotificationChannel EnabledChannels { get; set; } = NotificationChannel.InApp;

    /// <summary>
    /// Is this notification type enabled?
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Quiet hours start (e.g., "22:00")
    /// </summary>
    [MaxLength(5)]
    public string? QuietHoursStart { get; set; }

    /// <summary>
    /// Quiet hours end (e.g., "08:00")
    /// </summary>
    [MaxLength(5)]
    public string? QuietHoursEnd { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}

#region Enums

[Flags]
public enum NotificationChannel
{
    None = 0,
    InApp = 1,
    Email = 2,
    Sms = 4,
    Push = 8
}

public enum NotificationType
{
    System = 0,
    TaskAssigned = 1,
    TaskDue = 2,
    TaskCompleted = 3,
    OpportunityWon = 4,
    OpportunityLost = 5,
    TicketAssigned = 6,
    TicketStatusChanged = 7,
    TicketCommentAdded = 8,
    LeadAssigned = 9,
    LeadConverted = 10,
    ActivityReminder = 11,
    WorkflowAction = 12,
    MentionedInComment = 13,
    RecordShared = 14,
    TicketEscalated = 15,
    SlaViolation = 16,
    SlaBreached = 17,
    Custom = 99
}

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum DeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Skipped = 3
}

#endregion
