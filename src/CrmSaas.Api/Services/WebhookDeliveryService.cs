using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrmSaas.Api.Services;

public interface IWebhookDeliveryService
{
    Task DeliverWebhookAsync(Guid deliveryId, CancellationToken cancellationToken = default);
    Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken = default);
    Task RetryFailedDeliveriesAsync(CancellationToken cancellationToken = default);
}

public class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly TenantDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    public WebhookDeliveryService(
        TenantDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DeliverWebhookAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await _context.Set<WebhookDelivery>()
            .Include(d => d.WebhookSubscription)
            .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);

        if (delivery == null)
        {
            _logger.LogWarning("Webhook delivery {DeliveryId} not found", deliveryId);
            return;
        }

        if (delivery.WebhookSubscription == null || !delivery.WebhookSubscription.IsActive)
        {
            _logger.LogInformation("Webhook subscription inactive for delivery {DeliveryId}", deliveryId);
            delivery.Status = WebhookDeliveryStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        delivery.Status = WebhookDeliveryStatus.Sending;
        await _context.SaveChangesAsync(cancellationToken);

        var sw = Stopwatch.StartNew();

        try
        {
            var httpClient = _httpClientFactory.CreateClient("WebhookClient");
            httpClient.Timeout = TimeSpan.FromSeconds(delivery.WebhookSubscription.TimeoutSeconds);

            var request = new HttpRequestMessage(HttpMethod.Post, delivery.WebhookSubscription.TargetUrl)
            {
                Content = new StringContent(delivery.Payload, Encoding.UTF8, delivery.WebhookSubscription.ContentType)
            };

            // Add custom headers
            if (!string.IsNullOrEmpty(delivery.WebhookSubscription.CustomHeaders))
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    delivery.WebhookSubscription.CustomHeaders);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            // Add signature header if secret is configured
            if (!string.IsNullOrEmpty(delivery.WebhookSubscription.Secret))
            {
                var signature = GenerateSignature(delivery.Payload, delivery.WebhookSubscription.Secret);
                request.Headers.Add("X-Webhook-Signature", signature);
            }

            // Add metadata headers
            request.Headers.Add("X-Webhook-Event", delivery.EventType);
            request.Headers.Add("X-Webhook-Delivery-Id", delivery.Id.ToString());
            request.Headers.Add("X-Webhook-Timestamp", DateTime.UtcNow.ToString("O"));

            // Store request headers
            delivery.RequestHeaders = JsonSerializer.Serialize(
                request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

            var response = await httpClient.SendAsync(request, cancellationToken);

            sw.Stop();

            delivery.ResponseStatusCode = (int)response.StatusCode;
            delivery.DurationMs = (int)sw.ElapsedMilliseconds;
            delivery.ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            delivery.ResponseHeaders = JsonSerializer.Serialize(
                response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

            if (response.IsSuccessStatusCode)
            {
                delivery.Status = WebhookDeliveryStatus.Success;
                delivery.CompletedAt = DateTime.UtcNow;
                delivery.ErrorMessage = null;

                // Update subscription stats
                delivery.WebhookSubscription.SuccessCount++;
                delivery.WebhookSubscription.LastSuccessAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Webhook delivery {DeliveryId} succeeded in {Duration}ms",
                    deliveryId, sw.ElapsedMilliseconds);
            }
            else
            {
                await HandleDeliveryFailureAsync(delivery, 
                    $"HTTP {delivery.ResponseStatusCode}: {delivery.ResponseBody}", 
                    cancellationToken);
            }
        }
        catch (TaskCanceledException ex)
        {
            sw.Stop();
            delivery.DurationMs = (int)sw.ElapsedMilliseconds;
            await HandleDeliveryFailureAsync(delivery, $"Timeout after {sw.ElapsedMilliseconds}ms: {ex.Message}", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            delivery.DurationMs = (int)sw.ElapsedMilliseconds;
            await HandleDeliveryFailureAsync(delivery, $"HTTP error: {ex.Message}", cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            delivery.DurationMs = (int)sw.ElapsedMilliseconds;
            await HandleDeliveryFailureAsync(delivery, $"Unexpected error: {ex.Message}", cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        var pendingDeliveries = await _context.Set<WebhookDelivery>()
            .Where(d => d.Status == WebhookDeliveryStatus.Pending && 
                       (d.NextRetryAt == null || d.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(d => d.CreatedAt)
            .Take(50) // Process in batches
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processing {Count} pending webhook deliveries", pendingDeliveries.Count);

        foreach (var delivery in pendingDeliveries)
        {
            try
            {
                await DeliverWebhookAsync(delivery.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook delivery {DeliveryId}", delivery.Id);
            }
        }
    }

    public async Task RetryFailedDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        var retriableDeliveries = await _context.Set<WebhookDelivery>()
            .Include(d => d.WebhookSubscription)
            .Where(d => d.Status == WebhookDeliveryStatus.Retrying &&
                       d.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(d => d.NextRetryAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrying {Count} failed webhook deliveries", retriableDeliveries.Count);

        foreach (var delivery in retriableDeliveries)
        {
            try
            {
                await DeliverWebhookAsync(delivery.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying webhook delivery {DeliveryId}", delivery.Id);
            }
        }
    }

    private async Task HandleDeliveryFailureAsync(
        WebhookDelivery delivery,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        delivery.ErrorMessage = errorMessage;
        delivery.RetryCount++;

        if (delivery.WebhookSubscription == null)
        {
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.CompletedAt = DateTime.UtcNow;
            return;
        }

        if (delivery.RetryCount < delivery.WebhookSubscription.MaxRetries)
        {
            delivery.Status = WebhookDeliveryStatus.Retrying;
            
            // Exponential backoff: 1min, 5min, 15min, 30min, 1hr
            var delayMinutes = Math.Min(Math.Pow(2, delivery.RetryCount) * 5, 60);
            delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);

            _logger.LogWarning(
                "Webhook delivery {DeliveryId} failed (attempt {Attempt}/{Max}). Next retry at {NextRetry}. Error: {Error}",
                delivery.Id, delivery.RetryCount, delivery.WebhookSubscription.MaxRetries,
                delivery.NextRetryAt, errorMessage);
        }
        else
        {
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.CompletedAt = DateTime.UtcNow;

            // Update subscription stats
            delivery.WebhookSubscription.FailureCount++;
            delivery.WebhookSubscription.LastFailureAt = DateTime.UtcNow;

            _logger.LogError(
                "Webhook delivery {DeliveryId} permanently failed after {Attempts} attempts. Error: {Error}",
                delivery.Id, delivery.RetryCount, errorMessage);
        }
    }

    private static string GenerateSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(hash);
    }
}
