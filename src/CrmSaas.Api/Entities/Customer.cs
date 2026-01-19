using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Customer : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public CustomerType Type { get; set; } = CustomerType.Individual;
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(20)]
    public string? Mobile { get; set; }
    
    [MaxLength(500)]
    public string? Website { get; set; }
    
    // Individual fields
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [MaxLength(50)]
    public string? Title { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    // Business fields
    [MaxLength(200)]
    public string? CompanyName { get; set; }
    
    [MaxLength(50)]
    public string? TaxId { get; set; }
    
    [MaxLength(100)]
    public string? Industry { get; set; }
    
    public int? EmployeeCount { get; set; }
    
    public decimal? AnnualRevenue { get; set; }
    
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
    
    // CRM fields
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    
    public CustomerSource Source { get; set; } = CustomerSource.Direct;
    
    [MaxLength(100)]
    public string? SourceDetail { get; set; }
    
    public Guid? AssignedToUserId { get; set; }
    
    public decimal? LifetimeValue { get; set; }
    
    public int? CreditScore { get; set; }
    
    [MaxLength(50)]
    public string? CustomerCode { get; set; }
    
    public string? Tags { get; set; } // JSON array
    
    public string? CustomFields { get; set; } // JSON
    
    public string? Notes { get; set; }
    
    // Navigation
    public virtual User? AssignedToUser { get; set; }
    public virtual ICollection<Contact> Contacts { get; set; } = [];
    public virtual ICollection<Interaction> Interactions { get; set; } = [];
    public virtual ICollection<Lead> Leads { get; set; } = [];
    public virtual ICollection<Opportunity> Opportunities { get; set; } = [];
    public virtual ICollection<Quotation> Quotations { get; set; } = [];
    public virtual ICollection<Order> Orders { get; set; } = [];
    public virtual ICollection<Contract> Contracts { get; set; } = [];
    public virtual ICollection<Ticket> Tickets { get; set; } = [];
}

public enum CustomerType
{
    Individual = 0,
    Business = 1
}

public enum CustomerStatus
{
    Prospect = 0,
    Active = 1,
    Inactive = 2,
    Churned = 3
}

public enum CustomerSource
{
    Direct = 0,
    Referral = 1,
    Website = 2,
    SocialMedia = 3,
    Advertisement = 4,
    TradeShow = 5,
    Partner = 6,
    Other = 7
}
