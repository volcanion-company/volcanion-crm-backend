using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Contract : TenantAuditableEntity
{
    [Required]
    [MaxLength(50)]
    public string ContractNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public Guid? CustomerId { get; set; }
    
    public Guid? OrderId { get; set; }
    
    public Guid? ContactId { get; set; }
    
    public ContractType Type { get; set; } = ContractType.Service;
    
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public DateTime? SignedDate { get; set; }
    
    public DateTime? RenewalDate { get; set; }
    
    public bool AutoRenew { get; set; } = false;
    
    public int? RenewalPeriodMonths { get; set; }
    
    public int? NoticePeriodDays { get; set; }
    
    public decimal Value { get; set; }
    
    public BillingFrequency BillingFrequency { get; set; } = BillingFrequency.Monthly;
    
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";
    
    public string? Terms { get; set; }
    
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? DocumentUrl { get; set; }
    
    public Guid? AssignedToUserId { get; set; }
    
    public Guid? SignedByUserId { get; set; }
    
    public string? CustomFields { get; set; } // JSON
    
    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual Order? Order { get; set; }
    public virtual Contact? Contact { get; set; }
    public virtual User? AssignedToUser { get; set; }
    public virtual User? SignedByUser { get; set; }
}

public enum ContractType
{
    Service = 0,
    Subscription = 1,
    Support = 2,
    License = 3,
    Maintenance = 4,
    Other = 5
}

public enum ContractStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Active = 3,
    Expired = 4,
    Cancelled = 5,
    Renewed = 6
}

public enum BillingFrequency
{
    OneTime = 0,
    Monthly = 1,
    Quarterly = 2,
    SemiAnnually = 3,
    Annually = 4
}
