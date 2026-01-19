using CrmSaas.Api.Authorization;
using CrmSaas.Api.Common;
using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Controllers;

[Authorize]
public class ReportsController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(TenantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("dashboard")]
    [RequirePermission(Permissions.ReportView)]
    public async Task<ActionResult<ApiResponse<DashboardSummary>>> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var summary = new DashboardSummary
        {
            // Customer metrics
            TotalCustomers = await _db.Customers.CountAsync(),
            NewCustomersThisMonth = await _db.Customers
                .Where(c => c.CreatedAt >= startOfMonth)
                .CountAsync(),

            // Lead metrics
            TotalLeads = await _db.Leads
                .Where(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost)
                .CountAsync(),
            NewLeadsThisMonth = await _db.Leads
                .Where(l => l.CreatedAt >= startOfMonth)
                .CountAsync(),

            // Opportunity metrics
            OpenOpportunities = await _db.Opportunities
                .Where(o => o.Status == OpportunityStatus.Open)
                .CountAsync(),
            TotalPipelineValue = await _db.Opportunities
                .Where(o => o.Status == OpportunityStatus.Open)
                .SumAsync(o => o.Amount),
            WeightedPipelineValue = (await _db.Opportunities
                .Where(o => o.Status == OpportunityStatus.Open)
                .Select(o => new { o.Amount, o.Probability })
                .ToListAsync())
                .Sum(o => o.Amount * o.Probability / 100m),
            WonOpportunitiesThisMonth = await _db.Opportunities
                .Where(o => o.Status == OpportunityStatus.Won && o.ActualCloseDate >= startOfMonth)
                .CountAsync(),
            RevenueWonThisMonth = await _db.Opportunities
                .Where(o => o.Status == OpportunityStatus.Won && o.ActualCloseDate >= startOfMonth)
                .SumAsync(o => o.Amount),

            // Ticket metrics
            OpenTickets = await _db.Tickets
                .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved)
                .CountAsync(),
            OverdueTickets = await _db.Tickets
                .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved)
                .Where(t => t.DueDate < now)
                .CountAsync(),

            // Activity metrics
            OverdueActivities = await _db.Activities
                .Where(a => a.Status != ActivityStatus.Completed && a.DueDate < now)
                .CountAsync(),
            ActivitiesDueToday = await _db.Activities
                .Where(a => a.Status != ActivityStatus.Completed)
                .Where(a => a.DueDate.HasValue && a.DueDate.Value.Date == now.Date)
                .CountAsync()
        };

        return OkResponse(summary);
    }

    [HttpGet("sales-performance")]
    [RequirePermission(Permissions.ReportView)]
    public async Task<ActionResult<ApiResponse<SalesPerformanceReport>>> GetSalesPerformance(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-12);
        var end = endDate ?? DateTime.UtcNow;

        var wonOpportunities = await _db.Opportunities
            .AsNoTracking()
            .Where(o => o.Status == OpportunityStatus.Won)
            .Where(o => o.ActualCloseDate >= start && o.ActualCloseDate <= end)
            .ToListAsync();

        var lostOpportunities = await _db.Opportunities
            .AsNoTracking()
            .Where(o => o.Status == OpportunityStatus.Lost)
            .Where(o => o.ActualCloseDate >= start && o.ActualCloseDate <= end)
            .ToListAsync();

        var report = new SalesPerformanceReport
        {
            TotalWon = wonOpportunities.Count,
            TotalLost = lostOpportunities.Count,
            TotalRevenue = wonOpportunities.Sum(o => o.Amount),
            AverageDealSize = wonOpportunities.Any() ? wonOpportunities.Average(o => o.Amount) : 0,
            WinRate = (wonOpportunities.Count + lostOpportunities.Count) > 0
                ? Math.Round((decimal)wonOpportunities.Count / (wonOpportunities.Count + lostOpportunities.Count) * 100, 2)
                : 0,
            MonthlyData = wonOpportunities
                .Where(o => o.ActualCloseDate.HasValue)
                .GroupBy(o => new { o.ActualCloseDate!.Value.Year, o.ActualCloseDate!.Value.Month })
                .Select(g => new MonthlySalesData
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.Amount),
                    DealsWon = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList()
        };

        return OkResponse(report);
    }

    [HttpGet("pipeline-analysis")]
    [RequirePermission(Permissions.ReportView)]
    public async Task<ActionResult<ApiResponse<PipelineAnalysisReport>>> GetPipelineAnalysis(
        [FromQuery] Guid? pipelineId = null)
    {
        var query = _db.Opportunities
            .AsNoTracking()
            .Include(o => o.Stage)
            .Where(o => o.Status == OpportunityStatus.Open);

        if (pipelineId.HasValue)
        {
            query = query.Where(o => o.PipelineId == pipelineId);
        }

        var opportunities = await query.ToListAsync();

        var report = new PipelineAnalysisReport
        {
            TotalOpportunities = opportunities.Count,
            TotalValue = opportunities.Sum(o => o.Amount),
            WeightedValue = opportunities.Sum(o => o.WeightedAmount),
            AverageValue = opportunities.Any() ? opportunities.Average(o => o.Amount) : 0,
            AverageProbability = opportunities.Any() ? (decimal)opportunities.Average(o => o.Probability) : 0,
            ByStage = opportunities
                .Where(o => o.Stage != null)
                .GroupBy(o => new { o.StageId, o.Stage!.Name, o.Stage.SortOrder })
                .Select(g => new StageAnalysis
                {
                    StageId = g.Key.StageId,
                    StageName = g.Key.Name,
                    SortOrder = g.Key.SortOrder,
                    Count = g.Count(),
                    Value = g.Sum(o => o.Amount),
                    WeightedValue = g.Sum(o => o.WeightedAmount)
                })
                .OrderBy(s => s.SortOrder)
                .ToList()
        };

        return OkResponse(report);
    }

    [HttpGet("lead-conversion")]
    [RequirePermission(Permissions.ReportView)]
    public async Task<ActionResult<ApiResponse<LeadConversionReport>>> GetLeadConversion(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-12);
        var end = endDate ?? DateTime.UtcNow;

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.CreatedAt >= start && l.CreatedAt <= end)
            .ToListAsync();

        var report = new LeadConversionReport
        {
            TotalLeads = leads.Count,
            ConvertedLeads = leads.Count(l => l.Status == LeadStatus.Converted),
            QualifiedLeads = leads.Count(l => l.Status == LeadStatus.Qualified || l.Status == LeadStatus.Converted),
            UnqualifiedLeads = leads.Count(l => l.Status == LeadStatus.Unqualified),
            LostLeads = leads.Count(l => l.Status == LeadStatus.Lost),
            ConversionRate = leads.Any()
                ? Math.Round((decimal)leads.Count(l => l.Status == LeadStatus.Converted) / leads.Count * 100, 2)
                : 0,
            BySource = leads
                .GroupBy(l => l.Source)
                .Select(g => new SourceConversion
                {
                    Source = g.Key.ToString(),
                    Total = g.Count(),
                    Converted = g.Count(l => l.Status == LeadStatus.Converted),
                    ConversionRate = g.Any()
                        ? Math.Round((decimal)g.Count(l => l.Status == LeadStatus.Converted) / g.Count() * 100, 2)
                        : 0
                })
                .OrderByDescending(s => s.Total)
                .ToList(),
            ByRating = leads
                .GroupBy(l => l.Rating)
                .Select(g => new RatingConversion
                {
                    Rating = g.Key.ToString(),
                    Total = g.Count(),
                    Converted = g.Count(l => l.Status == LeadStatus.Converted),
                    ConversionRate = g.Any()
                        ? Math.Round((decimal)g.Count(l => l.Status == LeadStatus.Converted) / g.Count() * 100, 2)
                        : 0
                })
                .ToList()
        };

        return OkResponse(report);
    }

    [HttpGet("ticket-analytics")]
    [RequirePermission(Permissions.ReportView)]
    public async Task<ActionResult<ApiResponse<TicketAnalyticsReport>>> GetTicketAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
        var end = endDate ?? DateTime.UtcNow;

        var tickets = await _db.Tickets
            .AsNoTracking()
            .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
            .ToListAsync();

        var report = new TicketAnalyticsReport
        {
            TotalTickets = tickets.Count,
            OpenTickets = tickets.Count(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
            ResolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed),
            SlaBreachedTickets = tickets.Count(t => t.SlaBreached),
            ByPriority = tickets
                .GroupBy(t => t.Priority)
                .Select(g => new PriorityBreakdown
                {
                    Priority = g.Key.ToString(),
                    Count = g.Count(),
                    Resolved = g.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                })
                .ToList(),
            ByType = tickets
                .GroupBy(t => t.Type)
                .Select(g => new TypeBreakdown
                {
                    Type = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderByDescending(t => t.Count)
                .ToList(),
            ByChannel = tickets
                .GroupBy(t => t.Channel ?? "Unknown")
                .Select(g => new ChannelBreakdown
                {
                    Channel = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Count)
                .ToList()
        };

        return OkResponse(report);
    }

    [HttpGet("activity-summary")]
    [RequirePermission(Permissions.ReportView)]
    public async Task<ActionResult<ApiResponse<ActivitySummaryReport>>> GetActivitySummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? userId = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var query = _db.Activities
            .AsNoTracking()
            .Where(a => a.CreatedAt >= start && a.CreatedAt <= end);

        if (userId.HasValue)
        {
            query = query.Where(a => a.AssignedToUserId == userId);
        }

        var activities = await query.ToListAsync();

        var report = new ActivitySummaryReport
        {
            TotalActivities = activities.Count,
            CompletedActivities = activities.Count(a => a.Status == ActivityStatus.Completed),
            OverdueActivities = activities.Count(a => a.Status != ActivityStatus.Completed && a.DueDate < DateTime.UtcNow),
            CompletionRate = activities.Any()
                ? Math.Round((decimal)activities.Count(a => a.Status == ActivityStatus.Completed) / activities.Count * 100, 2)
                : 0,
            ByType = activities
                .GroupBy(a => a.Type)
                .Select(g => new ActivityTypeBreakdown
                {
                    Type = g.Key.ToString(),
                    Count = g.Count(),
                    Completed = g.Count(a => a.Status == ActivityStatus.Completed)
                })
                .OrderByDescending(t => t.Count)
                .ToList()
        };

        return OkResponse(report);
    }
}

// Response DTOs
public class DashboardSummary
{
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int TotalLeads { get; set; }
    public int NewLeadsThisMonth { get; set; }
    public int OpenOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int WonOpportunitiesThisMonth { get; set; }
    public decimal RevenueWonThisMonth { get; set; }
    public int OpenTickets { get; set; }
    public int OverdueTickets { get; set; }
    public int OverdueActivities { get; set; }
    public int ActivitiesDueToday { get; set; }
}

public class SalesPerformanceReport
{
    public int TotalWon { get; set; }
    public int TotalLost { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageDealSize { get; set; }
    public decimal WinRate { get; set; }
    public List<MonthlySalesData> MonthlyData { get; set; } = [];
}

public class MonthlySalesData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
    public int DealsWon { get; set; }
}

public class PipelineAnalysisReport
{
    public int TotalOpportunities { get; set; }
    public decimal TotalValue { get; set; }
    public decimal WeightedValue { get; set; }
    public decimal AverageValue { get; set; }
    public decimal AverageProbability { get; set; }
    public List<StageAnalysis> ByStage { get; set; } = [];
}

public class StageAnalysis
{
    public Guid? StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Count { get; set; }
    public decimal Value { get; set; }
    public decimal WeightedValue { get; set; }
}

public class LeadConversionReport
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int QualifiedLeads { get; set; }
    public int UnqualifiedLeads { get; set; }
    public int LostLeads { get; set; }
    public decimal ConversionRate { get; set; }
    public List<SourceConversion> BySource { get; set; } = [];
    public List<RatingConversion> ByRating { get; set; } = [];
}

public class SourceConversion
{
    public string Source { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Converted { get; set; }
    public decimal ConversionRate { get; set; }
}

public class RatingConversion
{
    public string Rating { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Converted { get; set; }
    public decimal ConversionRate { get; set; }
}

public class TicketAnalyticsReport
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int SlaBreachedTickets { get; set; }
    public List<PriorityBreakdown> ByPriority { get; set; } = [];
    public List<TypeBreakdown> ByType { get; set; } = [];
    public List<ChannelBreakdown> ByChannel { get; set; } = [];
}

public class PriorityBreakdown
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Resolved { get; set; }
}

public class TypeBreakdown
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ChannelBreakdown
{
    public string Channel { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ActivitySummaryReport
{
    public int TotalActivities { get; set; }
    public int CompletedActivities { get; set; }
    public int OverdueActivities { get; set; }
    public decimal CompletionRate { get; set; }
    public List<ActivityTypeBreakdown> ByType { get; set; } = [];
}

public class ActivityTypeBreakdown
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Completed { get; set; }
}
