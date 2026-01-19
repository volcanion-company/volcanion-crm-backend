using CrmSaas.Api.Data;
using CrmSaas.Api.DTOs.Webhook;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CrmSaas.Api.Services;

public interface IWebhookService
{
    Task<WebhookSubscriptionDto> CreateSubscriptionAsync(CreateWebhookSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task<WebhookSubscriptionDto> UpdateSubscriptionAsync(Guid id, UpdateWebhookSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WebhookSubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<WebhookSubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<List<WebhookSubscriptionDto>> GetActiveSubscriptionsForEventAsync(string eventType, CancellationToken cancellationToken = default);
    Task<WebhookTestResultDto> TestSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WebhookStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}

public class WebhookService : IWebhookService
{
    private readonly TenantDbContext _context;
    private readonly IWebhookDeliveryService _deliveryService;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        TenantDbContext context,
        IWebhookDeliveryService deliveryService,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task<WebhookSubscriptionDto> CreateSubscriptionAsync(
        CreateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        var subscription = new WebhookSubscription
        {
            Name = dto.Name,
            TargetUrl = dto.TargetUrl,
            Secret = dto.Secret,
            Events = string.Join(",", dto.Events),
            ContentType = dto.ContentType,
            MaxRetries = dto.MaxRetries,
            TimeoutSeconds = dto.TimeoutSeconds,
            Description = dto.Description,
            CustomHeaders = dto.CustomHeaders != null ? JsonSerializer.Serialize(dto.CustomHeaders) : null,
            IsActive = true
        };

        _context.Set<WebhookSubscription>().Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created webhook subscription {Name} with ID {Id}", dto.Name, subscription.Id);

        return MapToDto(subscription);
    }

    public async Task<WebhookSubscriptionDto> UpdateSubscriptionAsync(
        Guid id,
        UpdateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (subscription == null)
            throw new InvalidOperationException($"Webhook subscription {id} not found");

        if (dto.Name != null) subscription.Name = dto.Name;
        if (dto.TargetUrl != null) subscription.TargetUrl = dto.TargetUrl;
        if (dto.Secret != null) subscription.Secret = dto.Secret;
        if (dto.Events != null) subscription.Events = string.Join(",", dto.Events);
        if (dto.IsActive.HasValue) subscription.IsActive = dto.IsActive.Value;
        if (dto.MaxRetries.HasValue) subscription.MaxRetries = dto.MaxRetries.Value;
        if (dto.TimeoutSeconds.HasValue) subscription.TimeoutSeconds = dto.TimeoutSeconds.Value;
        if (dto.Description != null) subscription.Description = dto.Description;
        if (dto.CustomHeaders != null)
            subscription.CustomHeaders = JsonSerializer.Serialize(dto.CustomHeaders);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated webhook subscription {Id}", id);

        return MapToDto(subscription);
    }

    public async Task DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (subscription == null)
            throw new InvalidOperationException($"Webhook subscription {id} not found");

        _context.Set<WebhookSubscription>().Remove(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted webhook subscription {Id}", id);
    }

    public async Task<WebhookSubscriptionDto?> GetSubscriptionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        return subscription != null ? MapToDto(subscription) : null;
    }

    public async Task<List<WebhookSubscriptionDto>> GetSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _context.Set<WebhookSubscription>()
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return subscriptions.Select(MapToDto).ToList();
    }

    public async Task<List<WebhookSubscriptionDto>> GetActiveSubscriptionsForEventAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _context.Set<WebhookSubscription>()
            .Where(w => w.IsActive && w.Events.Contains(eventType))
            .ToListAsync(cancellationToken);

        return subscriptions.Select(MapToDto).ToList();
    }

    public async Task<WebhookTestResultDto> TestSubscriptionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (subscription == null)
            throw new InvalidOperationException($"Webhook subscription {id} not found");

        var testPayload = new WebhookPayloadDto
        {
            EventType = "test.event",
            Timestamp = DateTime.UtcNow,
            TenantId = subscription.TenantId,
            EntityType = "Test",
            EntityId = Guid.NewGuid(),
            Data = new { message = "This is a test webhook" }
        };

        var delivery = new WebhookDelivery
        {
            WebhookSubscriptionId = id,
            EventType = "test.event",
            EntityType = "Test",
            Payload = JsonSerializer.Serialize(testPayload),
            Status = WebhookDeliveryStatus.Pending
        };

        _context.Set<WebhookDelivery>().Add(delivery);
        await _context.SaveChangesAsync(cancellationToken);

        // Trigger delivery
        await _deliveryService.DeliverWebhookAsync(delivery.Id, cancellationToken);

        // Reload to get result
        await _context.Entry(delivery).ReloadAsync(cancellationToken);

        return new WebhookTestResultDto
        {
            Success = delivery.Status == WebhookDeliveryStatus.Success,
            StatusCode = delivery.ResponseStatusCode,
            ResponseBody = delivery.ResponseBody,
            ErrorMessage = delivery.ErrorMessage,
            DurationMs = delivery.DurationMs ?? 0
        };
    }

    public async Task<WebhookStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _context.Set<WebhookSubscription>().ToListAsync(cancellationToken);
        var deliveries = await _context.Set<WebhookDelivery>()
            .Where(d => d.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync(cancellationToken);

        var stats = new WebhookStatsDto
        {
            TotalSubscriptions = subscriptions.Count,
            ActiveSubscriptions = subscriptions.Count(s => s.IsActive),
            TotalDeliveries = deliveries.Count,
            SuccessfulDeliveries = deliveries.Count(d => d.Status == WebhookDeliveryStatus.Success),
            FailedDeliveries = deliveries.Count(d => d.Status == WebhookDeliveryStatus.Failed),
            PendingDeliveries = deliveries.Count(d => d.Status == WebhookDeliveryStatus.Pending || d.Status == WebhookDeliveryStatus.Retrying),
            SuccessRate = deliveries.Any() ? 
                (decimal)deliveries.Count(d => d.Status == WebhookDeliveryStatus.Success) / deliveries.Count * 100 : 0,
            EventStats = deliveries
                .GroupBy(d => d.EventType)
                .Select(g => new EventStatsDto
                {
                    EventType = g.Key,
                    Count = g.Count(),
                    SuccessCount = g.Count(d => d.Status == WebhookDeliveryStatus.Success),
                    FailureCount = g.Count(d => d.Status == WebhookDeliveryStatus.Failed)
                })
                .OrderByDescending(e => e.Count)
                .ToList()
        };

        return stats;
    }

    private static WebhookSubscriptionDto MapToDto(WebhookSubscription subscription)
    {
        return new WebhookSubscriptionDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            TargetUrl = subscription.TargetUrl,
            Events = subscription.Events.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            IsActive = subscription.IsActive,
            ContentType = subscription.ContentType,
            MaxRetries = subscription.MaxRetries,
            TimeoutSeconds = subscription.TimeoutSeconds,
            LastSuccessAt = subscription.LastSuccessAt,
            LastFailureAt = subscription.LastFailureAt,
            SuccessCount = subscription.SuccessCount,
            FailureCount = subscription.FailureCount,
            Description = subscription.Description,
            CreatedAt = subscription.CreatedAt
        };
    }
}
