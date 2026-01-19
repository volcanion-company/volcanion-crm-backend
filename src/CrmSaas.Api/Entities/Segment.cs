using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

/// <summary>
/// Customer/Lead segment for targeted marketing campaigns
/// </summary>
public class Segment : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Entity type to segment (Customer, Lead, Contact)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = "Customer";
    
    /// <summary>
    /// Filter criteria in JSON format
    /// </summary>
    public string FilterCriteria { get; set; } = "{}";
    
    /// <summary>
    /// Segment type: Static or Dynamic
    /// Static: Members manually added
    /// Dynamic: Members automatically calculated from filters
    /// </summary>
    public SegmentType Type { get; set; } = SegmentType.Dynamic;
    
    /// <summary>
    /// Current member count (cached)
    /// </summary>
    public int MemberCount { get; set; } = 0;
    
    /// <summary>
    /// Last time segment was calculated
    /// </summary>
    public DateTime? LastCalculatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<CampaignMember> CampaignMembers { get; set; } = new List<CampaignMember>();
}

public enum SegmentType
{
    Static = 0,     // Manually managed
    Dynamic = 1     // Auto-calculated from filters
}

/// <summary>
/// Represents a filter condition for segmentation
/// </summary>
public class SegmentFilter
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "Equals";
    public object? Value { get; set; }
    public string LogicOperator { get; set; } = "And"; // And/Or
}

/// <summary>
/// Segment filter criteria container
/// </summary>
public class SegmentCriteria
{
    public List<SegmentFilter> Filters { get; set; } = new();
    public string LogicOperator { get; set; } = "And"; // All/Any
}
