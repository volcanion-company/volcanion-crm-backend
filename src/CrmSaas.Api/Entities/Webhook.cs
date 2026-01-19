using System.ComponentModel.DataAnnotations;

namespace CrmSaas.Api.Entities;

/// <summary>
/// Webhook subscription for outbound events
/// </summary>
public class WebhookSubscription : TenantAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2000)]
    public string TargetUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Secret key for HMAC signature validation
    /// </summary>
    [MaxLength(500)]
    public string? Secret { get; set; }
    
    /// <summary>
    /// Events this webhook subscribes to (comma-separated or JSON array)
    /// </summary>
    public string Events { get; set; } = string.Empty; // "customer.created,opportunity.won"
    
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// HTTP headers to include with webhook requests (JSON)
    /// </summary>
    public string? CustomHeaders { get; set; }
    
    /// <summary>
    /// Content type for webhook payload
    /// </summary>
    [MaxLength(100)]
    public string ContentType { get; set; } = "application/json";
    
    /// <summary>
    /// Retry policy configuration (JSON)
    /// </summary>
    public string? RetryPolicy { get; set; }
    
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Timeout in seconds for webhook delivery
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Last successful delivery timestamp
    /// </summary>
    public DateTime? LastSuccessAt { get; set; }
    
    /// <summary>
    /// Last failed delivery timestamp
    /// </summary>
    public DateTime? LastFailureAt { get; set; }
    
    /// <summary>
    /// Total successful deliveries
    /// </summary>
    public int SuccessCount { get; set; } = 0;
    
    /// <summary>
    /// Total failed deliveries
    /// </summary>
    public int FailureCount { get; set; } = 0;
    
    public string? Description { get; set; }
    
    // Navigation
    public virtual ICollection<WebhookDelivery> Deliveries { get; set; } = [];
}

/// <summary>
/// Individual webhook delivery attempt
/// </summary>
public class WebhookDelivery : TenantAuditableEntity
{
    public Guid WebhookSubscriptionId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Entity type that triggered the event (Customer, Lead, Opportunity, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? EntityType { get; set; }
    
    /// <summary>
    /// Entity ID that triggered the event
    /// </summary>
    public Guid? EntityId { get; set; }
    
    /// <summary>
    /// Webhook payload (JSON)
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// Delivery status
    /// </summary>
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    
    /// <summary>
    /// HTTP status code from the webhook target
    /// </summary>
    public int? ResponseStatusCode { get; set; }
    
    /// <summary>
    /// Response body from the webhook target
    /// </summary>
    public string? ResponseBody { get; set; }
    
    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Next retry scheduled time
    /// </summary>
    public DateTime? NextRetryAt { get; set; }
    
    /// <summary>
    /// Time when delivery was completed (success or final failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Request headers sent (JSON)
    /// </summary>
    public string? RequestHeaders { get; set; }
    
    /// <summary>
    /// Response headers received (JSON)
    /// </summary>
    public string? ResponseHeaders { get; set; }
    
    /// <summary>
    /// Time taken for delivery in milliseconds
    /// </summary>
    public int? DurationMs { get; set; }
    
    // Navigation
    public virtual WebhookSubscription? WebhookSubscription { get; set; }
}

public enum WebhookDeliveryStatus
{
    Pending = 0,
    Sending = 1,
    Success = 2,
    Failed = 3,
    Retrying = 4,
    Cancelled = 5
}

/// <summary>
/// Webhook event types
/// </summary>
public static class WebhookEvents
{
    // Customer events
    public const string CustomerCreated = "customer.created";
    public const string CustomerUpdated = "customer.updated";
    public const string CustomerDeleted = "customer.deleted";
    
    // Lead events
    public const string LeadCreated = "lead.created";
    public const string LeadUpdated = "lead.updated";
    public const string LeadConverted = "lead.converted";
    public const string LeadDeleted = "lead.deleted";
    
    // Opportunity events
    public const string OpportunityCreated = "opportunity.created";
    public const string OpportunityUpdated = "opportunity.updated";
    public const string OpportunityWon = "opportunity.won";
    public const string OpportunityLost = "opportunity.lost";
    public const string OpportunityDeleted = "opportunity.deleted";
    
    // Ticket events
    public const string TicketCreated = "ticket.created";
    public const string TicketUpdated = "ticket.updated";
    public const string TicketAssigned = "ticket.assigned";
    public const string TicketResolved = "ticket.resolved";
    public const string TicketClosed = "ticket.closed";
    public const string TicketEscalated = "ticket.escalated";
    
    // Order events
    public const string OrderCreated = "order.created";
    public const string OrderUpdated = "order.updated";
    public const string OrderCancelled = "order.cancelled";
    
    // Contract events
    public const string ContractCreated = "contract.created";
    public const string ContractRenewed = "contract.renewed";
    public const string ContractExpired = "contract.expired";
    
    // Campaign events
    public const string CampaignStarted = "campaign.started";
    public const string CampaignCompleted = "campaign.completed";
    
    // Activity events
    public const string ActivityCreated = "activity.created";
    public const string ActivityCompleted = "activity.completed";
}
