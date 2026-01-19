using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Product : TenantAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Sku { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";
    
    public decimal? CostPrice { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    [MaxLength(50)]
    public string? Unit { get; set; } = "Each";
    
    public decimal? TaxRate { get; set; }
    
    public string? CustomFields { get; set; } // JSON
    
    // Navigation
    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = [];
    public virtual ICollection<OrderItem> OrderItems { get; set; } = [];
}

public class Quotation : TenantAuditableEntity
{
    [Required]
    [MaxLength(50)]
    public string QuotationNumber { get; set; } = string.Empty;
    
    public Guid? CustomerId { get; set; }
    
    public Guid? OpportunityId { get; set; }
    
    public Guid? ContactId { get; set; }
    
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    
    public DateTime QuotationDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiryDate { get; set; }
    
    public decimal SubTotal { get; set; }
    
    public decimal DiscountAmount { get; set; }
    
    public decimal DiscountPercent { get; set; }
    
    public decimal TaxAmount { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";
    
    public decimal? ExchangeRate { get; set; } = 1;
    
    [MaxLength(500)]
    public string? BillingAddress { get; set; }
    
    [MaxLength(500)]
    public string? ShippingAddress { get; set; }
    
    public string? Terms { get; set; }
    
    public string? Notes { get; set; }
    
    public Guid? AssignedToUserId { get; set; }
    
    public Guid? ConvertedToOrderId { get; set; }
    
    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual Opportunity? Opportunity { get; set; }
    public virtual Contact? Contact { get; set; }
    public virtual User? AssignedToUser { get; set; }
    public virtual Order? ConvertedToOrder { get; set; }
    public virtual ICollection<QuotationItem> Items { get; set; } = [];
}

public class QuotationItem : BaseEntity
{
    public Guid QuotationId { get; set; }
    
    public Guid? ProductId { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    public decimal Quantity { get; set; } = 1;
    
    public decimal UnitPrice { get; set; }
    
    public decimal DiscountPercent { get; set; }
    
    public decimal DiscountAmount { get; set; }
    
    public decimal TaxPercent { get; set; }
    
    public decimal TaxAmount { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public int SortOrder { get; set; } = 0;
    
    // Navigation
    public virtual Quotation? Quotation { get; set; }
    public virtual Product? Product { get; set; }
}

public enum QuotationStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    Converted = 5
}

public enum DiscountType
{
    None = 0,
    Percentage = 1,
    FixedAmount = 2
}
