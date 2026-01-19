namespace CrmSaas.Api.DTOs.Analytics;

public class WinRateAnalyticsDto
{
    public decimal OverallWinRate { get; set; }
    public int TotalWon { get; set; }
    public int TotalLost { get; set; }
    public int TotalOpen { get; set; }
    public decimal AverageWonValue { get; set; }
    public decimal AverageLostValue { get; set; }
    public List<WinRateByStageDto> WinRateByStage { get; set; } = [];
    public List<WinRateByRepDto> WinRateByRep { get; set; } = [];
    public List<WinRateByProductDto> WinRateByProduct { get; set; } = [];
    public List<WinLossReasonDto> TopWinReasons { get; set; } = [];
    public List<WinLossReasonDto> TopLossReasons { get; set; } = [];
}

public class WinRateByStageDto
{
    public string StageName { get; set; } = string.Empty;
    public decimal WinRate { get; set; }
    public int TotalOpportunities { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
}

public class WinRateByRepDto
{
    public Guid RepId { get; set; }
    public string RepName { get; set; } = string.Empty;
    public decimal WinRate { get; set; }
    public int TotalOpportunities { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
    public decimal TotalWonValue { get; set; }
}

public class WinRateByProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal WinRate { get; set; }
    public int TotalOpportunities { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
}

public class WinLossReasonDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
