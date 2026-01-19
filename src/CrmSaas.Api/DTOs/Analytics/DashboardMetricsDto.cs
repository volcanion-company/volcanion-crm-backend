namespace CrmSaas.Api.DTOs.Analytics;

public class DashboardMetricsDto
{
    public SalesMetricsDto Sales { get; set; } = new();
    public LeadMetricsDto Leads { get; set; } = new();
    public CustomerMetricsDto Customers { get; set; } = new();
    public TicketMetricsDto Support { get; set; } = new();
    public ActivityMetricsDto Activities { get; set; } = new();
    public List<RevenueByPeriodDto> RevenueChart { get; set; } = [];
    public List<OpportunityByStageDto> PipelineChart { get; set; } = [];
}

public class SalesMetricsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueThisQuarter { get; set; }
    public decimal RevenueGrowth { get; set; } // % vs previous period
    public int OpenOpportunities { get; set; }
    public decimal PipelineValue { get; set; }
    public decimal AverageDealSize { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageSalesCycle { get; set; } // in days
    public int DealsWonThisMonth { get; set; }
    public int DealsLostThisMonth { get; set; }
}

public class LeadMetricsDto
{
    public int TotalLeads { get; set; }
    public int NewLeadsThisMonth { get; set; }
    public int QualifiedLeads { get; set; }
    public decimal ConversionRate { get; set; } // Lead -> Customer %
    public decimal AverageLeadScore { get; set; }
    public int HotLeads { get; set; } // Priority = Hot
    public int LeadsConverted { get; set; }
    public List<LeadSourceBreakdownDto> LeadsBySource { get; set; } = [];
}

public class CustomerMetricsDto
{
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int ActiveCustomers { get; set; }
    public int ChurnedCustomers { get; set; }
    public decimal ChurnRate { get; set; }
    public decimal CustomerLifetimeValue { get; set; }
    public decimal CustomerAcquisitionCost { get; set; }
    public int CustomersAtRisk { get; set; } // Health score < 40
}

public class TicketMetricsDto
{
    public int OpenTickets { get; set; }
    public int ClosedThisMonth { get; set; }
    public decimal AverageResolutionTime { get; set; } // in hours
    public decimal AverageFirstResponseTime { get; set; } // in hours
    public int OverdueSla { get; set; }
    public decimal CustomerSatisfaction { get; set; } // CSAT score (placeholder)
    public List<TicketByPriorityDto> TicketsByPriority { get; set; } = [];
}

public class ActivityMetricsDto
{
    public int TotalActivities { get; set; }
    public int OverdueTasks { get; set; }
    public int DueToday { get; set; }
    public int CompletedThisWeek { get; set; }
    public int ScheduledCalls { get; set; }
    public int ScheduledMeetings { get; set; }
}

public class RevenueByPeriodDto
{
    public string Period { get; set; } = string.Empty; // "2024-01", "2024-Q1", etc.
    public decimal Revenue { get; set; }
    public int DealsWon { get; set; }
    public decimal Target { get; set; }
}

public class OpportunityByStageDto
{
    public string StageName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
    public decimal Probability { get; set; }
    public decimal WeightedValue { get; set; }
}

public class LeadSourceBreakdownDto
{
    public string Source { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal ConversionRate { get; set; }
}

public class TicketByPriorityDto
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal AverageResolutionHours { get; set; }
}
