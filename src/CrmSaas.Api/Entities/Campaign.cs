using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Campaign : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public CampaignType Type { get; set; } = CampaignType.Email;
    
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public decimal? Budget { get; set; }
    
    public decimal? ActualCost { get; set; }
    
    [MaxLength(10)]
    public string? Currency { get; set; } = "USD";
    
    public decimal? ExpectedRevenue { get; set; }
    
    public int? ExpectedLeads { get; set; }
    
    public int? ExpectedConversions { get; set; }
    
    // Actual metrics
    public int TotalSent { get; set; } = 0;
    
    public int TotalDelivered { get; set; } = 0;
    
    public int TotalOpened { get; set; } = 0;
    
    public int TotalClicked { get; set; } = 0;
    
    public int TotalBounced { get; set; } = 0;
    
    public int TotalUnsubscribed { get; set; } = 0;
    
    public int TotalLeadsGenerated { get; set; } = 0;
    
    public int TotalConversions { get; set; } = 0;
    
    public decimal? ActualRevenue { get; set; }
    
    public Guid? OwnerId { get; set; }
    
    [MaxLength(100)]
    public string? TargetAudience { get; set; }
    
    public string? Tags { get; set; } // JSON array
    
    public string? CustomFields { get; set; } // JSON
    
    // Navigation
    public virtual User? Owner { get; set; }
    public virtual ICollection<CampaignMember> Members { get; set; } = [];
    public virtual ICollection<CommunicationTemplate> Templates { get; set; } = [];
    public virtual ICollection<Opportunity> Opportunities { get; set; } = [];
}

public class CampaignMember : TenantAuditableEntity
{
    public Guid CampaignId { get; set; }
    
    public Guid? LeadId { get; set; }
    
    public Guid? ContactId { get; set; }
    
    public CampaignMemberStatus Status { get; set; } = CampaignMemberStatus.Sent;
    
    public DateTime? SentAt { get; set; }
    
    public DateTime? OpenedAt { get; set; }
    
    public DateTime? ClickedAt { get; set; }
    
    public DateTime? RespondedAt { get; set; }
    
    public DateTime? ConvertedAt { get; set; }
    
    [MaxLength(500)]
    public string? Response { get; set; }
    
    // Navigation
    public virtual Campaign? Campaign { get; set; }
    public virtual Lead? Lead { get; set; }
    public virtual Contact? Contact { get; set; }
}

public class CommunicationTemplate : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public CommunicationType Type { get; set; } = CommunicationType.Email;
    
    [MaxLength(200)]
    public string? Subject { get; set; }
    
    public string? Body { get; set; }
    
    public string? HtmlBody { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public Guid? CampaignId { get; set; }
    
    public string? Variables { get; set; } // JSON - available placeholders
    
    // Navigation
    public virtual Campaign? Campaign { get; set; }
}

public enum CampaignType
{
    Email = 0,
    SMS = 1,
    Social = 2,
    Event = 3,
    Webinar = 4,
    Advertisement = 5,
    Other = 6
}

public enum CampaignStatus
{
    Draft = 0,
    Scheduled = 1,
    InProgress = 2,
    Paused = 3,
    Completed = 4,
    Cancelled = 5
}

public enum CampaignMemberStatus
{
    Pending = 0,
    Sent = 1,
    Opened = 2,
    Clicked = 3,
    Responded = 4,
    Converted = 5,
    Unsubscribed = 6,
    Bounced = 7
}

public enum CommunicationType
{
    Email = 0,
    SMS = 1,
    PushNotification = 2,
    InApp = 3
}

public enum CampaignChannel
{
    Email = 0,
    SMS = 1,
    Social = 2,
    Web = 3,
    Mobile = 4,
    Print = 5,
    Television = 6,
    Radio = 7,
    Other = 8
}
