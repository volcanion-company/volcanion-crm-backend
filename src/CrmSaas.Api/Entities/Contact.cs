using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Contact : TenantAuditableEntity
{
    public Guid? CustomerId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(20)]
    public string? Mobile { get; set; }
    
    [MaxLength(100)]
    public string? JobTitle { get; set; }
    
    [MaxLength(100)]
    public string? Department { get; set; }
    
    public bool IsPrimary { get; set; } = false;
    
    public ContactStatus Status { get; set; } = ContactStatus.Active;
    
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
    
    // Social
    [MaxLength(200)]
    public string? LinkedInUrl { get; set; }
    
    [MaxLength(200)]
    public string? TwitterHandle { get; set; }
    
    public string? CustomFields { get; set; } // JSON
    
    public string? Notes { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<Interaction> Interactions { get; set; } = [];
}

public enum ContactStatus
{
    Active = 0,
    Inactive = 1,
    Unsubscribed = 2
}
