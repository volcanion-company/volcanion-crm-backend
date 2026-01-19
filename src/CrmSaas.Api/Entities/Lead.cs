using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Lead : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    // Contact info
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(20)]
    public string? Mobile { get; set; }
    
    [MaxLength(200)]
    public string? CompanyName { get; set; }
    
    [MaxLength(100)]
    public string? JobTitle { get; set; }
    
    [MaxLength(100)]
    public string? Industry { get; set; }
    
    public int? EmployeeCount { get; set; }
    
    // Address
    [MaxLength(500)]
    public string? AddressLine1 { get; set; }
    
    [MaxLength(500)]
    public string? AddressLine2 { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? State { get; set; }
    
    [MaxLength(20)]
    public string? PostalCode { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    // Lead management
    public LeadStatus Status { get; set; } = LeadStatus.New;
    
    public LeadSource Source { get; set; } = LeadSource.Website;
    
    [MaxLength(200)]
    public string? SourceDetail { get; set; }
    
    public int Score { get; set; } = 0;
    
    public LeadRating Rating { get; set; } = LeadRating.Cold;
    
    public Guid? AssignedToUserId { get; set; }
    
    public DateTime? AssignedAt { get; set; }
    
    public Guid? ConvertedToCustomerId { get; set; }
    
    public Guid? ConvertedToOpportunityId { get; set; }
    
    public DateTime? ConvertedAt { get; set; }
    
    public Guid? ConvertedByUserId { get; set; }
    
    public decimal? EstimatedValue { get; set; }
    
    [MaxLength(10)]
    public string? Currency { get; set; } = "USD";
    
    public string? Tags { get; set; } // JSON array
    
    public string? CustomFields { get; set; } // JSON
    
    public string? Description { get; set; }
    
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    // Navigation
    public virtual User? AssignedToUser { get; set; }
    public virtual Customer? ConvertedToCustomer { get; set; }
    public virtual Opportunity? ConvertedToOpportunity { get; set; }
    public virtual User? ConvertedByUser { get; set; }
    public virtual ICollection<Interaction> Interactions { get; set; } = [];
    public virtual ICollection<Activity> Activities { get; set; } = [];
}

public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Unqualified = 3,
    Converted = 4,
    Lost = 5
}

public enum LeadSource
{
    Website = 0,
    Referral = 1,
    SocialMedia = 2,
    Email = 3,
    Phone = 4,
    TradeShow = 5,
    Partner = 6,
    Advertisement = 7,
    ColdCall = 8,
    Other = 9
}

public enum LeadRating
{
    Cold = 0,
    Warm = 1,
    Hot = 2
}
