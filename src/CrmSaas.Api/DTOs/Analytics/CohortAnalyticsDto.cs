namespace CrmSaas.Api.DTOs.Analytics;

public class CohortAnalyticsDto
{
    public string CohortPeriod { get; set; } = string.Empty; // "2024-Q1", "2024-01", etc.
    public int NewCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int ChurnedCustomers { get; set; }
    public decimal ChurnRate { get; set; }
    public decimal RetentionRate { get; set; }
    public decimal AverageLifetimeValue { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageRevenuePerCustomer { get; set; }
    public List<CohortRetentionDto> RetentionByMonth { get; set; } = [];
}

public class CohortRetentionDto
{
    public int MonthNumber { get; set; } // 0 = acquisition month, 1 = month 1, etc.
    public string MonthLabel { get; set; } = string.Empty;
    public int ActiveCustomers { get; set; }
    public decimal RetentionRate { get; set; }
    public decimal Revenue { get; set; }
}

public class CustomerLifetimeAnalyticsDto
{
    public int TotalCustomers { get; set; }
    public decimal AverageLifetimeDays { get; set; }
    public decimal AverageLifetimeValue { get; set; }
    public decimal TotalLifetimeValue { get; set; }
    public List<CustomerSegmentDto> SegmentBreakdown { get; set; } = [];
}

public class CustomerSegmentDto
{
    public string SegmentName { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal AverageLTV { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal Percentage { get; set; }
}
