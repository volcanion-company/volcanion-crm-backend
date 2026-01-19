using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

public class Order : TenantAuditableEntity
{
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;
    
    public Guid? CustomerId { get; set; }
    
    public Guid? QuotationId { get; set; }
    
    public Guid? ContactId { get; set; }
    
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? DeliveryDate { get; set; }
    
    public decimal SubTotal { get; set; }
    
    public decimal DiscountAmount { get; set; }
    
    public decimal DiscountPercent { get; set; }
    
    public decimal TaxAmount { get; set; }
    
    public decimal ShippingAmount { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public decimal PaidAmount { get; set; }
    
    public decimal BalanceAmount => TotalAmount - PaidAmount;
    
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";
    
    public decimal? ExchangeRate { get; set; } = 1;
    
    [MaxLength(500)]
    public string? BillingAddress { get; set; }
    
    [MaxLength(500)]
    public string? ShippingAddress { get; set; }
    
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
    
    [MaxLength(100)]
    public string? PaymentReference { get; set; }
    
    public string? Terms { get; set; }
    
    public string? Notes { get; set; }
    
    public string? InternalNotes { get; set; }
    
    public Guid? AssignedToUserId { get; set; }
    
    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual Quotation? Quotation { get; set; }
    public virtual Contact? Contact { get; set; }
    public virtual User? AssignedToUser { get; set; }
    public virtual ICollection<OrderItem> Items { get; set; } = [];
    public virtual ICollection<Contract> Contracts { get; set; } = [];
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    
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
    public virtual Order? Order { get; set; }
    public virtual Product? Product { get; set; }
}

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Completed = 5,
    Cancelled = 6,
    Refunded = 7
}

public enum PaymentStatus
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Refunded = 3
}
