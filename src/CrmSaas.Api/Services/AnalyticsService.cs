using CrmSaas.Api.Data;
using CrmSaas.Api.DTOs.Analytics;
using CrmSaas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Services;

public interface IAnalyticsService
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<SalesCycleAnalyticsDto> GetSalesCycleAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<WinRateAnalyticsDto> GetWinRateAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<CohortAnalyticsDto> GetCohortAnalyticsAsync(string cohortPeriod, CancellationToken cancellationToken = default);
    Task<CustomerLifetimeAnalyticsDto> GetCustomerLifetimeAnalyticsAsync(CancellationToken cancellationToken = default);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(TenantDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddMonths(-1);
        endDate ??= DateTime.UtcNow;

        var metrics = new DashboardMetricsDto();

        // Sales Metrics
        var opportunities = await _context.Opportunities
            .Include(o => o.AssignedToUser)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var wonOpps = opportunities.Where(o => o.Status == OpportunityStatus.Won).ToList();
        var lostOpps = opportunities.Where(o => o.Status == OpportunityStatus.Lost).ToList();
        var openOpps = opportunities.Where(o => o.Status == OpportunityStatus.Open).ToList();

        metrics.Sales = new SalesMetricsDto
        {
            TotalRevenue = wonOpps.Sum(o => o.Amount),
            RevenueThisMonth = wonOpps.Where(o => o.ActualCloseDate?.Month == DateTime.UtcNow.Month).Sum(o => o.Amount),
            OpenOpportunities = openOpps.Count,
            PipelineValue = openOpps.Sum(o => o.Amount),
            AverageDealSize = wonOpps.Any() ? wonOpps.Average(o => o.Amount) : 0m,
            WinRate = opportunities.Any() ? (decimal)wonOpps.Count / opportunities.Count * 100 : 0m,
            DealsWonThisMonth = wonOpps.Count(o => o.ActualCloseDate?.Month == DateTime.UtcNow.Month),
            DealsLostThisMonth = lostOpps.Count(o => o.ActualCloseDate?.Month == DateTime.UtcNow.Month)
        };

        // Calculate sales cycle
        var closedWithDates = wonOpps.Where(o => o.CreatedAt != default && o.ActualCloseDate != null).ToList();
        if (closedWithDates.Any())
        {
            var cycles = closedWithDates.Select(o => (o.ActualCloseDate!.Value - o.CreatedAt).TotalDays).ToList();
            metrics.Sales.AverageSalesCycle = (decimal)cycles.Average();
        }

        // Lead Metrics
        var leads = await _context.Leads
            .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var convertedLeads = leads.Count(l => l.Status == LeadStatus.Converted || l.Status == LeadStatus.Qualified);

        metrics.Leads = new LeadMetricsDto
        {
            TotalLeads = leads.Count,
            NewLeadsThisMonth = leads.Count(l => l.CreatedAt.Month == DateTime.UtcNow.Month),
            QualifiedLeads = leads.Count(l => l.Status == LeadStatus.Qualified),
            ConversionRate = leads.Any() ? (decimal)convertedLeads / leads.Count * 100 : 0,
            HotLeads = 0, // Lead.Priority field doesn't exist
            LeadsConverted = convertedLeads,
            LeadsBySource = leads.GroupBy(l => l.Source)
                .Select(g => new LeadSourceBreakdownDto
                {
                    Source = g.Key.ToString(),
                    Count = g.Count(),
                    ConversionRate = (decimal)g.Count(l => l.Status == LeadStatus.Converted) / g.Count() * 100
                })
                .OrderByDescending(x => x.Count)
                .ToList()
        };

        // Customer Metrics
        var customers = await _context.Customers
            .Where(c => c.CreatedAt >= startDate.Value.AddYears(-1)) // Last year for churn calculation
            .ToListAsync(cancellationToken);

        var newCustomersThisMonth = customers.Count(c => c.CreatedAt.Month == DateTime.UtcNow.Month && c.CreatedAt.Year == DateTime.UtcNow.Year);

        metrics.Customers = new CustomerMetricsDto
        {
            TotalCustomers = customers.Count,
            NewCustomersThisMonth = newCustomersThisMonth,
            ActiveCustomers = customers.Count(c => !c.IsDeleted),
            ChurnedCustomers = customers.Count(c => c.IsDeleted && c.DeletedAt >= startDate),
            ChurnRate = customers.Any() ? (decimal)customers.Count(c => c.IsDeleted) / customers.Count * 100 : 0
        };

        // Ticket Metrics
        var tickets = await _context.Tickets
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var closedTickets = tickets.Where(t => t.Status == TicketStatus.Closed || t.Status == TicketStatus.Resolved).ToList();
        var resolutionTimes = closedTickets
            .Where(t => t.ResolvedDate != null)
            .Select(t => (t.ResolvedDate!.Value - t.CreatedAt).TotalHours)
            .ToList();

        var firstResponseTimes = tickets
            .Where(t => t.FirstResponseDate != null)
            .Select(t => (t.FirstResponseDate!.Value - t.CreatedAt).TotalHours)
            .ToList();

        metrics.Support = new TicketMetricsDto
        {
            OpenTickets = tickets.Count(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress),
            ClosedThisMonth = closedTickets.Count(t => t.ResolvedDate?.Month == DateTime.UtcNow.Month),
            AverageResolutionTime = resolutionTimes.Any() ? (decimal)resolutionTimes.Average() : 0,
            AverageFirstResponseTime = firstResponseTimes.Any() ? (decimal)firstResponseTimes.Average() : 0,
            OverdueSla = tickets.Count(t => t.SlaBreached),
            TicketsByPriority = tickets.GroupBy(t => t.Priority)
                .Select(g => new TicketByPriorityDto
                {
                    Priority = g.Key.ToString(),
                    Count = g.Count(),
                    AverageResolutionHours = g.Where(t => t.ResolvedDate != null)
                        .Select(t => (decimal)(t.ResolvedDate!.Value - t.CreatedAt).TotalHours)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .ToList()
        };

        // Activity Metrics
        var activities = await _context.Activities
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        metrics.Activities = new ActivityMetricsDto
        {
            TotalActivities = activities.Count,
            OverdueTasks = activities.Count(a => a.DueDate < DateTime.UtcNow && a.Status != ActivityStatus.Completed),
            DueToday = activities.Count(a => a.DueDate.HasValue && a.DueDate.Value.Date == DateTime.UtcNow.Date),
            CompletedThisWeek = activities.Count(a => a.Status == ActivityStatus.Completed && a.CompletedDate >= DateTime.UtcNow.AddDays(-7)),
            ScheduledCalls = activities.Count(a => a.Type == ActivityType.Call && a.DueDate > DateTime.UtcNow),
            ScheduledMeetings = activities.Count(a => a.Type == ActivityType.Meeting && a.DueDate > DateTime.UtcNow)
        };

        // Revenue Chart (last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var revenueData = await _context.Opportunities
            .Where(o => o.Status == OpportunityStatus.Won && o.ActualCloseDate >= sixMonthsAgo)
            .ToListAsync(cancellationToken);

        metrics.RevenueChart = revenueData
            .GroupBy(o => new { Year = o.ActualCloseDate!.Value.Year, Month = o.ActualCloseDate.Value.Month })
            .Select(g => new RevenueByPeriodDto
            {
                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(o => o.Amount),
                DealsWon = g.Count()
            })
            .OrderBy(x => x.Period)
            .ToList();

        // Pipeline Chart
        var oppWithStages = await _context.Opportunities
            .Include(o => o.Stage)
            .Where(o => o.Status == OpportunityStatus.Open)
            .ToListAsync(cancellationToken);
        
        metrics.PipelineChart = oppWithStages
            .GroupBy(o => o.Stage?.Name ?? "Unknown")
            .Select(g => new OpportunityByStageDto
            {
                StageName = g.Key,
                Count = g.Count(),
                TotalValue = g.Sum(o => o.Amount),
                Probability = (decimal)g.Average(o => o.Probability),
                WeightedValue = g.Sum(o => o.Amount * o.Probability / 100)
            })
            .OrderByDescending(x => x.TotalValue)
            .ToList();

        return metrics;
    }

    public async Task<SalesCycleAnalyticsDto> GetSalesCycleAnalyticsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddMonths(-6);
        endDate ??= DateTime.UtcNow;

        var closedOpportunities = await _context.Opportunities
            .Include(o => o.AssignedToUser)
            .Where(o => o.Status == OpportunityStatus.Won &&
                       o.ActualCloseDate >= startDate && 
                       o.ActualCloseDate <= endDate)
            .ToListAsync(cancellationToken);

        if (!closedOpportunities.Any())
        {
            return new SalesCycleAnalyticsDto();
        }

        var cycles = closedOpportunities
            .Where(o => o.ActualCloseDate != null)
            .Select(o => (o.ActualCloseDate!.Value - o.CreatedAt).TotalDays)
            .OrderBy(d => d)
            .ToList();

        var analytics = new SalesCycleAnalyticsDto
        {
            TotalClosedOpportunities = closedOpportunities.Count,
            AverageCycleDays = cycles.Any() ? (decimal)cycles.Average() : 0m,
            MedianCycleDays = cycles.Any() ? (decimal)cycles[cycles.Count / 2] : 0m,
            ShortestCycleDays = cycles.Any() ? (decimal)cycles.Min() : 0m,
            LongestCycleDays = cycles.Any() ? (decimal)cycles.Max() : 0m
        };

        // Cycle by rep
        analytics.CycleByRep = closedOpportunities
            .Where(o => o.AssignedToUserId != null && o.ActualCloseDate != null)
            .GroupBy(o => new { o.AssignedToUserId, OwnerName = o.AssignedToUser?.FullName ?? "Unknown" })
            .Select(g => new CycleByRepDto
            {
                RepId = g.Key.AssignedToUserId!.Value,
                RepName = g.Key.OwnerName,
                AverageCycleDays = (decimal)g.Average(o => (o.ActualCloseDate!.Value - o.CreatedAt).TotalDays),
                OpportunitiesCount = g.Count(),
                WinRate = 100 // All are won in this dataset
            })
            .OrderBy(x => x.AverageCycleDays)
            .ToList();

        return analytics;
    }

    public async Task<WinRateAnalyticsDto> GetWinRateAnalyticsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddMonths(-6);
        endDate ??= DateTime.UtcNow;

        var opportunities = await _context.Opportunities
            .Include(o => o.AssignedToUser)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var won = opportunities.Where(o => o.Status == OpportunityStatus.Won).ToList();
        var lost = opportunities.Where(o => o.Status == OpportunityStatus.Lost).ToList();
        var open = opportunities.Where(o => o.Status == OpportunityStatus.Open).ToList();

        var analytics = new WinRateAnalyticsDto
        {
            TotalWon = won.Count,
            TotalLost = lost.Count,
            TotalOpen = open.Count,
            OverallWinRate = (won.Count + lost.Count) > 0 ? (decimal)won.Count / (won.Count + lost.Count) * 100 : 0m,
            AverageWonValue = won.Any() ? won.Average(o => o.Amount) : 0m,
            AverageLostValue = lost.Any() ? lost.Average(o => o.Amount) : 0m
        };

        // Win rate by rep
        var closedOpps = won.Concat(lost).ToList();
        analytics.WinRateByRep = closedOpps
            .Where(o => o.AssignedToUserId != null)
            .GroupBy(o => new { o.AssignedToUserId, OwnerName = o.AssignedToUser?.FullName ?? "Unknown" })
            .Select(g => new WinRateByRepDto
            {
                RepId = g.Key.AssignedToUserId!.Value,
                RepName = g.Key.OwnerName,
                TotalOpportunities = g.Count(),
                Won = g.Count(o => o.Status == OpportunityStatus.Won),
                Lost = g.Count(o => o.Status == OpportunityStatus.Lost),
                WinRate = g.Count() > 0 ? (decimal)g.Count(o => o.Status == OpportunityStatus.Won) / g.Count() * 100 : 0,
                TotalWonValue = g.Where(o => o.Status == OpportunityStatus.Won).Sum(o => o.Amount)
            })
            .OrderByDescending(x => x.WinRate)
            .ToList();

        // Win rate by stage
        var oppWithStages = await _context.Opportunities
            .Include(o => o.Stage)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && 
                       (o.Status == OpportunityStatus.Won || o.Status == OpportunityStatus.Lost))
            .ToListAsync(cancellationToken);
            
        analytics.WinRateByStage = oppWithStages
            .GroupBy(o => o.Stage?.Name ?? "Unknown")
            .Select(g => new WinRateByStageDto
            {
                StageName = g.Key,
                TotalOpportunities = g.Count(),
                Won = g.Count(o => o.Status == OpportunityStatus.Won),
                Lost = g.Count(o => o.Status == OpportunityStatus.Lost),
                WinRate = g.Count() > 0 ? (decimal)g.Count(o => o.Status == OpportunityStatus.Won) / g.Count() * 100 : 0
            })
            .ToList();

        return analytics;
    }

    public async Task<CohortAnalyticsDto> GetCohortAnalyticsAsync(
        string cohortPeriod, 
        CancellationToken cancellationToken = default)
    {
        // Parse cohort period (e.g., "2024-01" or "2024-Q1")
        var isQuarterly = cohortPeriod.Contains("Q");
        DateTime cohortStart;
        DateTime cohortEnd;

        if (isQuarterly)
        {
            var parts = cohortPeriod.Split('-');
            var year = int.Parse(parts[0]);
            var quarter = int.Parse(parts[1].Replace("Q", ""));
            cohortStart = new DateTime(year, (quarter - 1) * 3 + 1, 1);
            cohortEnd = cohortStart.AddMonths(3).AddDays(-1);
        }
        else
        {
            var parts = cohortPeriod.Split('-');
            var year = int.Parse(parts[0]);
            var month = int.Parse(parts[1]);
            cohortStart = new DateTime(year, month, 1);
            cohortEnd = cohortStart.AddMonths(1).AddDays(-1);
        }

        var cohortCustomers = await _context.Customers
            .Where(c => c.CreatedAt >= cohortStart && c.CreatedAt <= cohortEnd)
            .ToListAsync(cancellationToken);

        var analytics = new CohortAnalyticsDto
        {
            CohortPeriod = cohortPeriod,
            NewCustomers = cohortCustomers.Count,
            ActiveCustomers = cohortCustomers.Count(c => !c.IsDeleted),
            ChurnedCustomers = cohortCustomers.Count(c => c.IsDeleted),
            ChurnRate = cohortCustomers.Any() ? (decimal)cohortCustomers.Count(c => c.IsDeleted) / cohortCustomers.Count * 100 : 0,
            RetentionRate = cohortCustomers.Any() ? (decimal)cohortCustomers.Count(c => !c.IsDeleted) / cohortCustomers.Count * 100 : 0
        };

        return analytics;
    }

    public async Task<CustomerLifetimeAnalyticsDto> GetCustomerLifetimeAnalyticsAsync(
        CancellationToken cancellationToken = default)
    {
        var customers = await _context.Customers
            .Include(c => c.Opportunities)
            .ToListAsync(cancellationToken);

        var analytics = new CustomerLifetimeAnalyticsDto
        {
            TotalCustomers = customers.Count
        };

        if (customers.Any())
        {
            var lifetimes = customers
                .Select(c => (DateTime.UtcNow - c.CreatedAt).TotalDays)
                .ToList();

            analytics.AverageLifetimeDays = (decimal)lifetimes.Average();

            // Calculate LTV from won opportunities
            var customerLtvs = customers.Select(c => new
            {
                Customer = c,
                LTV = c.Opportunities?
                    .Where(o => o.Status == OpportunityStatus.Won)
                    .Sum(o => o.Amount) ?? 0
            }).ToList();

            analytics.TotalLifetimeValue = customerLtvs.Sum(x => x.LTV);
            analytics.AverageLifetimeValue = customerLtvs.Any() ? customerLtvs.Average(x => x.LTV) : 0;

            // Segment by LTV
            analytics.SegmentBreakdown = new List<CustomerSegmentDto>
            {
                new() {
                    SegmentName = "High Value (>$10k)",
                    CustomerCount = customerLtvs.Count(x => x.LTV > 10000),
                    TotalRevenue = customerLtvs.Where(x => x.LTV > 10000).Sum(x => x.LTV),
                    AverageLTV = customerLtvs.Where(x => x.LTV > 10000).Any() ? 
                        customerLtvs.Where(x => x.LTV > 10000).Average(x => x.LTV) : 0
                },
                new() {
                    SegmentName = "Medium Value ($1k-$10k)",
                    CustomerCount = customerLtvs.Count(x => x.LTV >= 1000 && x.LTV <= 10000),
                    TotalRevenue = customerLtvs.Where(x => x.LTV >= 1000 && x.LTV <= 10000).Sum(x => x.LTV),
                    AverageLTV = customerLtvs.Where(x => x.LTV >= 1000 && x.LTV <= 10000).Any() ?
                        customerLtvs.Where(x => x.LTV >= 1000 && x.LTV <= 10000).Average(x => x.LTV) : 0
                },
                new() {
                    SegmentName = "Low Value (<$1k)",
                    CustomerCount = customerLtvs.Count(x => x.LTV < 1000),
                    TotalRevenue = customerLtvs.Where(x => x.LTV < 1000).Sum(x => x.LTV),
                    AverageLTV = customerLtvs.Where(x => x.LTV < 1000).Any() ?
                        customerLtvs.Where(x => x.LTV < 1000).Average(x => x.LTV) : 0
                }
            };

            // Calculate percentages
            foreach (var segment in analytics.SegmentBreakdown)
            {
                segment.Percentage = customers.Any() ? (decimal)segment.CustomerCount / customers.Count * 100 : 0;
            }
        }

        return analytics;
    }
}
