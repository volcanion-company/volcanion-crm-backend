using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Ticket : TenantAuditableEntity
{
    [Required]
    [MaxLength(50)]
    public string TicketNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public Guid? CustomerId { get; set; }
    
    public Guid? ContactId { get; set; }
    
    public TicketStatus Status { get; set; } = TicketStatus.New;
    
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    
    public TicketType Type { get; set; } = TicketType.Question;
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    [MaxLength(100)]
    public string? SubCategory { get; set; }
    
    public Guid? AssignedToUserId { get; set; }
    
    public Guid? SlaId { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public DateTime? FirstResponseDate { get; set; }
    
    public DateTime? ResolvedDate { get; set; }
    
    public DateTime? ClosedDate { get; set; }
    
    public int? SatisfactionRating { get; set; } // 1-5
    
    public string? SatisfactionComment { get; set; }
    
    public bool SlaBreached { get; set; } = false;
    
    /// <summary>
    /// Target time for first response (calculated from priority)
    /// </summary>
    public DateTime? FirstResponseTarget { get; set; }
    
    /// <summary>
    /// Target time for resolution (calculated from priority)
    /// </summary>
    public DateTime? ResolutionTarget { get; set; }
    
    /// <summary>
    /// SLA is paused (e.g., waiting for customer)
    /// </summary>
    public bool SlaPaused { get; set; } = false;
    
    /// <summary>
    /// Total time SLA has been paused (in minutes)
    /// </summary>
    public int SlaPausedMinutes { get; set; } = 0;
    
    /// <summary>
    /// When SLA was last paused
    /// </summary>
    public DateTime? SlaPausedAt { get; set; }
    
    /// <summary>
    /// Reason for SLA pause
    /// </summary>
    [MaxLength(500)]
    public string? SlaPauseReason { get; set; }
    
    /// <summary>
    /// Number of times ticket has been escalated
    /// </summary>
    public int EscalationCount { get; set; } = 0;
    
    /// <summary>
    /// Last escalation date
    /// </summary>
    public DateTime? LastEscalatedAt { get; set; }
    
    /// <summary>
    /// User/Team ticket was escalated to
    /// </summary>
    public Guid? EscalatedToUserId { get; set; }
    
    [MaxLength(100)]
    public string? Channel { get; set; } // Email, Phone, Web, Chat, etc.
    
    public string? Tags { get; set; } // JSON array
    
    public string? CustomFields { get; set; } // JSON
    
    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual Contact? Contact { get; set; }
    public virtual User? AssignedToUser { get; set; }
    public virtual Sla? Sla { get; set; }
    public virtual ICollection<TicketComment> Comments { get; set; } = [];
}

public class TicketComment : TenantAuditableEntity
{
    public Guid TicketId { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    public bool IsInternal { get; set; } = false;
    
    public bool IsFromCustomer { get; set; } = false;
    
    public Guid? AuthorUserId { get; set; }
    
    [MaxLength(100)]
    public string? AuthorName { get; set; }
    
    [MaxLength(100)]
    public string? AuthorEmail { get; set; }
    
    // Navigation
    public virtual Ticket? Ticket { get; set; }
    public virtual User? AuthorUser { get; set; }
}

public class Sla : TenantAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsDefault { get; set; } = false;
    
    // Response times in minutes
    public int FirstResponseTimeLow { get; set; } = 480; // 8 hours
    
    public int FirstResponseTimeMedium { get; set; } = 240; // 4 hours
    
    public int FirstResponseTimeHigh { get; set; } = 60; // 1 hour
    
    public int FirstResponseTimeCritical { get; set; } = 15; // 15 minutes
    
    // Resolution times in minutes
    public int ResolutionTimeLow { get; set; } = 2880; // 48 hours
    
    public int ResolutionTimeMedium { get; set; } = 1440; // 24 hours
    
    public int ResolutionTimeHigh { get; set; } = 480; // 8 hours
    
    public int ResolutionTimeCritical { get; set; } = 120; // 2 hours
    
    public string? BusinessHours { get; set; } // JSON - defines working hours
    
    // Navigation
    public virtual ICollection<Ticket> Tickets { get; set; } = [];
}

public enum TicketStatus
{
    New = 0,
    Open = 1,
    InProgress = 2,
    Pending = 3,
    OnHold = 4,
    Resolved = 5,
    Closed = 6,
    Reopened = 7
}

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum TicketType
{
    Question = 0,
    Problem = 1,
    Incident = 2,
    FeatureRequest = 3,
    Task = 4
}

public enum TicketChannel
{
    Email = 0,
    Phone = 1,
    Web = 2,
    Chat = 3,
    Api = 4,
    SocialMedia = 5,
    InPerson = 6
}
