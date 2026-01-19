using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Opportunity : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public Guid? CustomerId { get; set; }
    
    public Guid? PrimaryContactId { get; set; }
    
    public Guid PipelineId { get; set; }
    
    public Guid StageId { get; set; }
    
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;
    
    public decimal Amount { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";
    
    public int Probability { get; set; } = 0; // 0-100%
    
    public decimal WeightedAmount => Amount * Probability / 100;
    
    public DateTime? ExpectedCloseDate { get; set; }
    
    public DateTime? ActualCloseDate { get; set; }
    
    public Guid? AssignedToUserId { get; set; }
    
    [MaxLength(200)]
    public string? LossReason { get; set; }
    
    [MaxLength(200)]
    public string? WinReason { get; set; }
    
    [MaxLength(200)]
    public string? CompetitorName { get; set; }
    
    public OpportunityType Type { get; set; } = OpportunityType.NewBusiness;
    
    public OpportunityPriority Priority { get; set; } = OpportunityPriority.Medium;
    
    public Guid? SourceLeadId { get; set; }
    
    public Guid? SourceCampaignId { get; set; }
    
    public string? Tags { get; set; } // JSON array
    
    public string? CustomFields { get; set; } // JSON
    
    public string? Description { get; set; }
    
    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual Contact? PrimaryContact { get; set; }
    public virtual Pipeline? Pipeline { get; set; }
    public virtual PipelineStage? Stage { get; set; }
    public virtual User? AssignedToUser { get; set; }
    public virtual Lead? SourceLead { get; set; }
    public virtual Campaign? SourceCampaign { get; set; }
    public virtual ICollection<Interaction> Interactions { get; set; } = [];
    public virtual ICollection<Quotation> Quotations { get; set; } = [];
    public virtual ICollection<Activity> Activities { get; set; } = [];
}

public enum OpportunityStatus
{
    Open = 0,
    Won = 1,
    Lost = 2
}

public enum OpportunityType
{
    NewBusiness = 0,
    ExistingBusiness = 1,
    Renewal = 2,
    Upsell = 3
}

public enum OpportunityPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}
