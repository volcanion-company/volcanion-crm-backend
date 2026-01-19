using CrmSaas.Api.Common;
using CrmSaas.Api.DTOs.Analytics;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSaas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive dashboard metrics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _analyticsService.GetDashboardMetricsAsync(startDate, endDate, cancellationToken);
            return Ok(ApiResponse<DashboardMetricsDto>.Ok(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard metrics");
            return StatusCode(500, ApiResponse<DashboardMetricsDto>.Fail("Failed to retrieve dashboard metrics"));
        }
    }

    /// <summary>
    /// Get sales cycle analytics
    /// </summary>
    [HttpGet("sales-cycle")]
    public async Task<IActionResult> GetSalesCycleAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _analyticsService.GetSalesCycleAnalyticsAsync(startDate, endDate, cancellationToken);
            return Ok(ApiResponse<SalesCycleAnalyticsDto>.Ok(analytics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales cycle analytics");
            return StatusCode(500, ApiResponse<SalesCycleAnalyticsDto>.Fail("Failed to retrieve sales cycle analytics"));
        }
    }

    /// <summary>
    /// Get win rate analytics
    /// </summary>
    [HttpGet("win-rate")]
    public async Task<IActionResult> GetWinRateAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _analyticsService.GetWinRateAnalyticsAsync(startDate, endDate, cancellationToken);
            return Ok(ApiResponse<WinRateAnalyticsDto>.Ok(analytics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting win rate analytics");
            return StatusCode(500, ApiResponse<WinRateAnalyticsDto>.Fail("Failed to retrieve win rate analytics"));
        }
    }

    /// <summary>
    /// Get customer cohort analytics for a specific period
    /// </summary>
    /// <param name="cohortPeriod">Format: "2024-01" for monthly or "2024-Q1" for quarterly</param>
    [HttpGet("cohort/{cohortPeriod}")]
    public async Task<IActionResult> GetCohortAnalytics(
        string cohortPeriod,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _analyticsService.GetCohortAnalyticsAsync(cohortPeriod, cancellationToken);
            return Ok(ApiResponse<CohortAnalyticsDto>.Ok(analytics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cohort analytics for period {Period}", cohortPeriod);
            return StatusCode(500, ApiResponse<CohortAnalyticsDto>.Fail("Failed to retrieve cohort analytics"));
        }
    }

    /// <summary>
    /// Get customer lifetime value analytics
    /// </summary>
    [HttpGet("customer-lifetime")]
    public async Task<IActionResult> GetCustomerLifetimeAnalytics(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _analyticsService.GetCustomerLifetimeAnalyticsAsync(cancellationToken);
            return Ok(ApiResponse<CustomerLifetimeAnalyticsDto>.Ok(analytics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer lifetime analytics");
            return StatusCode(500, ApiResponse<CustomerLifetimeAnalyticsDto>.Fail("Failed to retrieve customer lifetime analytics"));
        }
    }
}
