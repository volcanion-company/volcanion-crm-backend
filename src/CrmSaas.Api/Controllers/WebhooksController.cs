using CrmSaas.Api.Common;
using CrmSaas.Api.DTOs.Webhook;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSaas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Get all webhook subscriptions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _webhookService.GetSubscriptionsAsync(cancellationToken);
            return Ok(ApiResponse<List<WebhookSubscriptionDto>>.Ok(subscriptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook subscriptions");
            return StatusCode(500, ApiResponse<List<WebhookSubscriptionDto>>.Fail("Failed to retrieve webhook subscriptions"));
        }
    }

    /// <summary>
    /// Get a specific webhook subscription
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSubscription(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _webhookService.GetSubscriptionAsync(id, cancellationToken);
            
            if (subscription == null)
                return NotFound(ApiResponse<WebhookSubscriptionDto>.Fail("Webhook subscription not found"));

            return Ok(ApiResponse<WebhookSubscriptionDto>.Ok(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook subscription {Id}", id);
            return StatusCode(500, ApiResponse<WebhookSubscriptionDto>.Fail("Failed to retrieve webhook subscription"));
        }
    }

    /// <summary>
    /// Create a new webhook subscription
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _webhookService.CreateSubscriptionAsync(dto, cancellationToken);
            return CreatedAtAction(
                nameof(GetSubscription),
                new { id = subscription.Id },
                ApiResponse<WebhookSubscriptionDto>.Ok(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook subscription");
            return StatusCode(500, ApiResponse<WebhookSubscriptionDto>.Fail("Failed to create webhook subscription"));
        }
    }

    /// <summary>
    /// Update a webhook subscription
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubscription(
        Guid id,
        [FromBody] UpdateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _webhookService.UpdateSubscriptionAsync(id, dto, cancellationToken);
            return Ok(ApiResponse<WebhookSubscriptionDto>.Ok(subscription));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<WebhookSubscriptionDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook subscription {Id}", id);
            return StatusCode(500, ApiResponse<WebhookSubscriptionDto>.Fail("Failed to update webhook subscription"));
        }
    }

    /// <summary>
    /// Delete a webhook subscription
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _webhookService.DeleteSubscriptionAsync(id, cancellationToken);
            return Ok(ApiResponse.Ok("Webhook subscription deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook subscription {Id}", id);
            return StatusCode(500, ApiResponse.Fail("Failed to delete webhook subscription"));
        }
    }

    /// <summary>
    /// Test a webhook subscription by sending a test payload
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestSubscription(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _webhookService.TestSubscriptionAsync(id, cancellationToken);
            return Ok(ApiResponse<WebhookTestResultDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<WebhookTestResultDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook subscription {Id}", id);
            return StatusCode(500, ApiResponse<WebhookTestResultDto>.Fail("Failed to test webhook subscription"));
        }
    }

    /// <summary>
    /// Get webhook delivery statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _webhookService.GetStatsAsync(cancellationToken);
            return Ok(ApiResponse<WebhookStatsDto>.Ok(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook stats");
            return StatusCode(500, ApiResponse<WebhookStatsDto>.Fail("Failed to retrieve webhook statistics"));
        }
    }
}
