namespace CrmSaas.Api.DTOs.Webhook;

public class WebhookSubscriptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public List<string> Events { get; set; } = [];
    public bool IsActive { get; set; }
    public string ContentType { get; set; } = "application/json";
    public int MaxRetries { get; set; }
    public int TimeoutSeconds { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateWebhookSubscriptionDto
{
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public List<string> Events { get; set; } = [];
    public Dictionary<string, string>? CustomHeaders { get; set; }
    public string ContentType { get; set; } = "application/json";
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public string? Description { get; set; }
}

public class UpdateWebhookSubscriptionDto
{
    public string? Name { get; set; }
    public string? TargetUrl { get; set; }
    public string? Secret { get; set; }
    public List<string>? Events { get; set; }
    public Dictionary<string, string>? CustomHeaders { get; set; }
    public bool? IsActive { get; set; }
    public int? MaxRetries { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? Description { get; set; }
}

public class WebhookDeliveryDto
{
    public Guid Id { get; set; }
    public Guid WebhookSubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ResponseStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WebhookPayloadDto
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid TenantId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public object Data { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
}

public class WebhookTestResultDto
{
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
}

public class WebhookStatsDto
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TotalDeliveries { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public int PendingDeliveries { get; set; }
    public decimal SuccessRate { get; set; }
    public List<EventStatsDto> EventStats { get; set; } = [];
}

public class EventStatsDto
{
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}
