namespace CrmSaas.Api.DTOs.Analytics;

public class SalesCycleAnalyticsDto
{
    public decimal AverageCycleDays { get; set; }
    public decimal MedianCycleDays { get; set; }
    public decimal ShortestCycleDays { get; set; }
    public decimal LongestCycleDays { get; set; }
    public int TotalClosedOpportunities { get; set; }
    public List<StageTimeAnalysisDto> StageBreakdown { get; set; } = [];
    public List<CycleByRepDto> CycleByRep { get; set; } = [];
    public List<CycleByStageDto> CycleByStage { get; set; } = [];
}

public class StageTimeAnalysisDto
{
    public string StageName { get; set; } = string.Empty;
    public decimal AverageDays { get; set; }
    public decimal MedianDays { get; set; }
    public int OpportunitiesCount { get; set; }
}

public class CycleByRepDto
{
    public Guid RepId { get; set; }
    public string RepName { get; set; } = string.Empty;
    public decimal AverageCycleDays { get; set; }
    public int OpportunitiesCount { get; set; }
    public decimal WinRate { get; set; }
}

public class CycleByStageDto
{
    public string StageName { get; set; } = string.Empty;
    public decimal AverageDays { get; set; }
    public int OpportunitiesCount { get; set; }
    public decimal ConversionRate { get; set; }
}
