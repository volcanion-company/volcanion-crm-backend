using CrmSaas.Api.Data;
using CrmSaas.Api.DTOs.Webhook;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CrmSaas.Api.Services;

public interface IWebhookPublisher
{
    Task PublishEventAsync(string eventType, string entityType, Guid entityId, object data, CancellationToken cancellationToken = default);
    Task PublishCustomerCreatedAsync(Guid customerId, object customerData, CancellationToken cancellationToken = default);
    Task PublishLeadCreatedAsync(Guid leadId, object leadData, CancellationToken cancellationToken = default);
    Task PublishOpportunityWonAsync(Guid opportunityId, object opportunityData, CancellationToken cancellationToken = default);
    Task PublishTicketCreatedAsync(Guid ticketId, object ticketData, CancellationToken cancellationToken = default);
}

public class WebhookPublisher : IWebhookPublisher
{
    private readonly TenantDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<WebhookPublisher> _logger;

    public WebhookPublisher(
        TenantDbContext context,
        ICurrentUserService currentUser,
        ILogger<WebhookPublisher> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task PublishEventAsync(
        string eventType,
        string entityType,
        Guid entityId,
        object data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get active subscriptions for this event type
            var subscriptions = await _context.Set<WebhookSubscription>()
                .Where(s => s.IsActive && s.Events.Contains(eventType))
                .ToListAsync(cancellationToken);

            if (!subscriptions.Any())
            {
                _logger.LogDebug("No active webhook subscriptions for event {EventType}", eventType);
                return;
            }

            var tenantId = _currentUser.TenantId ?? Guid.Empty;

            var payload = new WebhookPayloadDto
            {
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                TenantId = tenantId,
                EntityType = entityType,
                EntityId = entityId,
                Data = data,
                Metadata = new Dictionary<string, string>
                {
                    { "userId", _currentUser.UserId?.ToString() ?? string.Empty },
                    { "tenantId", tenantId.ToString() }
                }
            };

            var payloadJson = JsonSerializer.Serialize(payload);

            foreach (var subscription in subscriptions)
            {
                var delivery = new WebhookDelivery
                {
                    WebhookSubscriptionId = subscription.Id,
                    EventType = eventType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Payload = payloadJson,
                    Status = WebhookDeliveryStatus.Pending
                };

                _context.Set<WebhookDelivery>().Add(delivery);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Published webhook event {EventType} for {EntityType} {EntityId} to {SubscriptionCount} subscriptions",
                eventType, entityType, entityId, subscriptions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error publishing webhook event {EventType} for {EntityType} {EntityId}",
                eventType, entityType, entityId);
        }
    }

    public Task PublishCustomerCreatedAsync(Guid customerId, object customerData, CancellationToken cancellationToken = default)
    {
        return PublishEventAsync(WebhookEvents.CustomerCreated, "Customer", customerId, customerData, cancellationToken);
    }

    public Task PublishLeadCreatedAsync(Guid leadId, object leadData, CancellationToken cancellationToken = default)
    {
        return PublishEventAsync(WebhookEvents.LeadCreated, "Lead", leadId, leadData, cancellationToken);
    }

    public Task PublishOpportunityWonAsync(Guid opportunityId, object opportunityData, CancellationToken cancellationToken = default)
    {
        return PublishEventAsync(WebhookEvents.OpportunityWon, "Opportunity", opportunityId, opportunityData, cancellationToken);
    }

    public Task PublishTicketCreatedAsync(Guid ticketId, object ticketData, CancellationToken cancellationToken = default)
    {
        return PublishEventAsync(WebhookEvents.TicketCreated, "Ticket", ticketId, ticketData, cancellationToken);
    }
}
