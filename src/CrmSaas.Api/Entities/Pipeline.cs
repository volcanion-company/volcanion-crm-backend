using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Pipeline : TenantAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsDefault { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    public int SortOrder { get; set; } = 0;
    
    // Navigation
    public virtual ICollection<PipelineStage> Stages { get; set; } = [];
    public virtual ICollection<Opportunity> Opportunities { get; set; } = [];
}

public class PipelineStage : TenantAuditableEntity
{
    public Guid PipelineId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public int SortOrder { get; set; } = 0;
    
    public int Probability { get; set; } = 0; // 0-100%
    
    public bool IsWon { get; set; } = false;
    
    public bool IsLost { get; set; } = false;
    
    [MaxLength(20)]
    public string? Color { get; set; }
    
    // Navigation
    public virtual Pipeline? Pipeline { get; set; }
    public virtual ICollection<Opportunity> Opportunities { get; set; } = [];
}
