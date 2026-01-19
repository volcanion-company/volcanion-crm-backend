using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Activity : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public ActivityType Type { get; set; } = ActivityType.Task;
    
    public ActivityStatus Status { get; set; } = ActivityStatus.NotStarted;
    
    public ActivityPriority Priority { get; set; } = ActivityPriority.Medium;
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public DateTime? CompletedDate { get; set; }
    
    public int? DurationMinutes { get; set; }
    
    public Guid? AssignedToUserId { get; set; }
    
    // Related entities
    public Guid? CustomerId { get; set; }
    
    public Guid? ContactId { get; set; }
    
    public Guid? LeadId { get; set; }
    
    public Guid? OpportunityId { get; set; }
    
    public Guid? TicketId { get; set; }
    
    // Recurrence
    public bool IsRecurring { get; set; } = false;
    
    public RecurrencePattern? RecurrencePattern { get; set; }
    
    public int? RecurrenceInterval { get; set; }
    
    public DateTime? RecurrenceEndDate { get; set; }
    
    public Guid? ParentActivityId { get; set; }
    
    public string? Tags { get; set; } // JSON array
    
    public string? CustomFields { get; set; } // JSON
    
    // Navigation
    public virtual User? AssignedToUser { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Contact? Contact { get; set; }
    public virtual Lead? Lead { get; set; }
    public virtual Opportunity? Opportunity { get; set; }
    public virtual Ticket? Ticket { get; set; }
    public virtual Activity? ParentActivity { get; set; }
    public virtual ICollection<Reminder> Reminders { get; set; } = [];
}

public class Reminder : TenantAuditableEntity
{
    public Guid? ActivityId { get; set; }
    
    [MaxLength(200)]
    public string? Subject { get; set; }
    
    public string? Message { get; set; }
    
    public DateTime ReminderDate { get; set; }
    
    public ReminderMethod Method { get; set; } = ReminderMethod.InApp;
    
    public bool IsSent { get; set; } = false;
    
    public DateTime? SentAt { get; set; }
    
    public Guid? UserId { get; set; }
    
    // Navigation
    public virtual Activity? Activity { get; set; }
    public virtual User? User { get; set; }
}

public enum ActivityType
{
    Task = 0,
    Call = 1,
    Meeting = 2,
    Email = 3,
    FollowUp = 4,
    Demo = 5,
    Other = 6
}

public enum ActivityStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Waiting = 3,
    Deferred = 4,
    Cancelled = 5
}

public enum ActivityPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public enum RecurrencePattern
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Yearly = 3
}

public enum ReminderMethod
{
    InApp = 0,
    Email = 1,
    SMS = 2,
    Push = 3
}
