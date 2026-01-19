using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Interaction : TenantAuditableEntity
{
    public Guid? CustomerId { get; set; }
    
    public Guid? ContactId { get; set; }
    
    public Guid? LeadId { get; set; }
    
    public Guid? OpportunityId { get; set; }
    
    public InteractionType Type { get; set; } = InteractionType.Note;
    
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTime InteractionDate { get; set; } = DateTime.UtcNow;
    
    public int? DurationMinutes { get; set; }
    
    public InteractionDirection Direction { get; set; } = InteractionDirection.Outbound;
    
    public InteractionOutcome Outcome { get; set; } = InteractionOutcome.None;
    
    public Guid? PerformedByUserId { get; set; }
    
    public string? CustomFields { get; set; } // JSON
    
    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual Contact? Contact { get; set; }
    public virtual Lead? Lead { get; set; }
    public virtual Opportunity? Opportunity { get; set; }
    public virtual User? PerformedByUser { get; set; }
}

public enum InteractionType
{
    Note = 0,
    Call = 1,
    Email = 2,
    Meeting = 3,
    Task = 4,
    SMS = 5,
    Chat = 6,
    SocialMedia = 7
}

public enum InteractionDirection
{
    Inbound = 0,
    Outbound = 1
}

public enum InteractionOutcome
{
    None = 0,
    Positive = 1,
    Neutral = 2,
    Negative = 3,
    FollowUpRequired = 4
}
