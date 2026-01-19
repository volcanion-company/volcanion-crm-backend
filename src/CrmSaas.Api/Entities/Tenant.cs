using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Tenant : AuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Identifier { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Subdomain { get; set; }
    
    [MaxLength(500)]
    public string? ConnectionString { get; set; }
    
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    
    public TenantPlan Plan { get; set; } = TenantPlan.Free;
    
    public DateTime? PlanExpiryDate { get; set; }
    
    public int MaxUsers { get; set; } = 5;
    
    public long MaxStorageBytes { get; set; } = 1073741824; // 1GB
    
    [MaxLength(500)]
    public string? LogoUrl { get; set; }
    
    [MaxLength(50)]
    public string? PrimaryColor { get; set; }
    
    [MaxLength(100)]
    public string? TimeZone { get; set; } = "UTC";
    
    [MaxLength(10)]
    public string? Culture { get; set; } = "en-US";
    
    public string? Settings { get; set; } // JSON configuration
    
    // Navigation
    public virtual ICollection<User> Users { get; set; } = [];
    public virtual ICollection<Role> Roles { get; set; } = [];
}

public enum TenantStatus
{
    Pending = 0,
    Active = 1,
    Suspended = 2,
    Cancelled = 3
}

public enum TenantPlan
{
    Free = 0,
    Starter = 1,
    Professional = 2,
    Enterprise = 3
}
