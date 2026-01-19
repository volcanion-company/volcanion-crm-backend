# Reports API Documentation

## Tổng quan Module

### Reports là gì?

**Reports (Báo cáo)** là module cung cấp các báo cáo business intelligence với visualization, export functionality, và scheduled reports. Reports khác với Analytics ở chỗ tập trung vào historical data analysis và business reporting.

### Tại sao cần Reports?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  ANALYTICS vs REPORTS                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Analytics:                        Reports:                                 │
│  ──────────                        ────────                                 │
│  • Real-time metrics               • Historical analysis                    │
│  • Live dashboards                 • Scheduled generation                   │
│  • Interactive exploration         • Fixed format output                    │
│  • Quick insights                  • Formal documentation                   │
│                                                                             │
│  Use Cases:                        Use Cases:                               │
│  • Daily monitoring                • Monthly sales report                   │
│  • Performance tracking            • Quarterly board presentation           │
│  • Ad-hoc analysis                 • Year-end summary                       │
│                                    • Compliance reporting                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Available Reports

### 1. Dashboard Summary

**Overview snapshot** của tất cả key metrics.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     DASHBOARD SUMMARY REPORT                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  CUSTOMERS                                                                  │
│  ─────────────────────────────────────────────────────────────────────     │
│  Total: 1,234                New This Month: 28        Active: 1,180       │
│                                                                             │
│  LEADS                                                                      │
│  ─────────────────────────────────────────────────────────────────────     │
│  Total: 456 (open)          New This Month: 89                             │
│                                                                             │
│  OPPORTUNITIES                                                              │
│  ─────────────────────────────────────────────────────────────────────     │
│  Open: 145                  Pipeline Value: $4M        Won This Month: 12  │
│  Weighted Pipeline: $1.8M   Revenue This Month: $450K                      │
│                                                                             │
│  SUPPORT                                                                    │
│  ─────────────────────────────────────────────────────────────────────     │
│  Open Tickets: 45           Overdue: 3                 Due Today: 12       │
│                                                                             │
│  ACTIVITIES                                                                 │
│  ─────────────────────────────────────────────────────────────────────     │
│  Overdue: 23                Due Today: 45                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 2. Sales Performance Report

**Detailed sales analysis** với revenue trends, win/loss tracking.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  SALES PERFORMANCE REPORT - 2025 Q4                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  SUMMARY METRICS                                                            │
│  ─────────────────────────────────────────────────────────────────────     │
│  Total Won: 45 deals               Total Lost: 32 deals                    │
│  Total Revenue: $1,575,000         Average Deal: $35,000                   │
│  Win Rate: 58.4%                                                            │
│                                                                             │
│  MONTHLY BREAKDOWN                                                          │
│  ─────────────────────────────────────────────────────────────────────     │
│  │ Month │ Revenue  │ Deals Won │ Avg Deal │ Win Rate │                    │
│  ├───────┼──────────┼───────────┼──────────┼──────────┤                    │
│  │ Oct   │ $480K    │ 14        │ $34.3K   │ 56%      │                    │
│  │ Nov   │ $520K    │ 15        │ $34.7K   │ 60%      │                    │
│  │ Dec   │ $575K    │ 16        │ $35.9K   │ 59%      │                    │
│  └───────┴──────────┴───────────┴──────────┴──────────┘                    │
│                                                                             │
│  TREND ANALYSIS                                                             │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Revenue: ↑ 19.8% vs Q3                                                   │
│  • Deal count: ↑ 12.5% vs Q3                                                │
│  • Win rate: ↑ 2.3% vs Q3                                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 3. Pipeline Analysis Report

**Deep dive into sales pipeline** by stage.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      PIPELINE ANALYSIS REPORT                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  OVERALL METRICS                                                            │
│  ─────────────────────────────────────────────────────────────────────     │
│  Total Opportunities: 145          Total Value: $4,050,000                 │
│  Weighted Value: $1,823,000        Average Value: $27,931                  │
│  Average Probability: 45%                                                   │
│                                                                             │
│  BY STAGE                                                                   │
│  ─────────────────────────────────────────────────────────────────────     │
│  │ Stage         │ Count │ Value    │ Weighted  │ Avg Prob │              │
│  ├───────────────┼───────┼──────────┼───────────┼──────────┤              │
│  │ Prospecting   │ 50    │ $500K    │ $50K      │ 10%      │              │
│  │ Qualification │ 40    │ $800K    │ $200K     │ 25%      │              │
│  │ Proposal      │ 30    │ $1.2M    │ $600K     │ 50%      │              │
│  │ Negotiation   │ 15    │ $900K    │ $720K     │ 80%      │              │
│  │ Closing       │ 10    │ $650K    │ $585K     │ 90%      │              │
│  └───────────────┴───────┴──────────┴───────────┴──────────┘              │
│                                                                             │
│  INSIGHTS                                                                   │
│  • 65% of pipeline value in late stages (Proposal+)                         │
│  • Prospecting stage needs more leads                                       │
│  • Strong conversion from Qualification → Proposal                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 4. Lead Conversion Report

**Lead funnel analysis** với conversion rates by source, rating.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     LEAD CONVERSION REPORT - 2025                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  OVERALL METRICS                                                            │
│  ─────────────────────────────────────────────────────────────────────     │
│  Total Leads: 1,234                Converted: 222 (18.0%)                  │
│  Qualified: 445 (36.1%)            Unqualified: 345 (28.0%)                │
│  Lost: 222 (18.0%)                                                          │
│                                                                             │
│  CONVERSION BY SOURCE                                                       │
│  ─────────────────────────────────────────────────────────────────────     │
│  │ Source          │ Total │ Converted │ Rate   │                          │
│  ├─────────────────┼───────┼───────────┼────────┤                          │
│  │ Website         │ 450   │ 99        │ 22.0%  │ ████████████            │
│  │ Referral        │ 320   │ 90        │ 28.1%  │ ██████████████          │
│  │ Email Campaign  │ 280   │ 42        │ 15.0%  │ ████████                │
│  │ Social Media    │ 130   │ 18        │ 13.8%  │ ███████                 │
│  │ Direct          │ 54    │ 16        │ 29.6%  │ ███████████████         │
│  └─────────────────┴───────┴───────────┴────────┘                          │
│                                                                             │
│  CONVERSION BY RATING                                                       │
│  ─────────────────────────────────────────────────────────────────────     │
│  │ Rating │ Total │ Converted │ Rate   │                                   │
│  ├────────┼───────┼───────────┼────────┤                                   │
│  │ Hot    │ 185   │ 92        │ 49.7%  │ █████████████████████████        │
│  │ Warm   │ 420   │ 92        │ 21.9%  │ ███████████                      │
│  │ Cold   │ 629   │ 38        │ 6.0%   │ ███                              │
│  └────────┴───────┴───────────┴────────┘                                   │
│                                                                             │
│  KEY INSIGHTS                                                               │
│  • Referrals = highest conversion (28.1%)                                   │
│  • Hot leads convert at 50% (excellent!)                                    │
│  • Improve cold lead qualification                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 5. Ticket Analytics Report

**Support team performance** với resolution times, SLA compliance.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  TICKET ANALYTICS REPORT - 2025 Q4                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  SUMMARY METRICS                                                            │
│  ─────────────────────────────────────────────────────────────────────     │
│  Total Tickets: 1,456              Open: 45          Resolved: 1,411       │
│  Avg Resolution Time: 18.5 hours   SLA Met: 95.2%                          │
│  Avg First Response: 1.2 hours     Customer Sat: 4.3/5.0                   │
│                                                                             │
│  BY PRIORITY                                                                │
│  ─────────────────────────────────────────────────────────────────────     │
│  │ Priority │ Count │ Avg Resolution │ SLA Met │                           │
│  ├──────────┼───────┼────────────────┼─────────┤                           │
│  │ Critical │ 89    │ 2.1 hours      │ 98%     │                           │
│  │ High     │ 342   │ 8.5 hours      │ 96%     │                           │
│  │ Medium   │ 685   │ 24.3 hours     │ 94%     │                           │
│  │ Low      │ 340   │ 48.7 hours     │ 95%     │                           │
│  └──────────┴───────┴────────────────┴─────────┘                           │
│                                                                             │
│  BY TYPE                                                                    │
│  │ Type            │ Count │ % Total │                                     │
│  ├─────────────────┼───────┼─────────┤                                     │
│  │ Problem         │ 654   │ 44.9%   │ ██████████████████                 │
│  │ Question        │ 432   │ 29.7%   │ ████████████                       │
│  │ Incident        │ 234   │ 16.1%   │ ████████                           │
│  │ Feature Request │ 136   │ 9.3%    │ █████                              │
│  └─────────────────┴───────┴─────────┘                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Dashboard Summary (Tổng quan Dashboard)

**Nghiệp vụ thực tế:**
- Executive summary cho management
- Quick health check của toàn bộ business

**Ví dụ thực tế:**
> CEO login mỗi sáng, check dashboard summary:
> - Customers: 1,234 total, 28 new this month ✓
> - Pipeline: $4M, weighted $1.8M
> - Support: 45 open tickets, 3 overdue ⚠️
> - Activities: 23 overdue ⚠️ → Need attention
> → Focus on overdue items today

---

### 2. Sales Performance Report (Báo cáo hiệu quả bán hàng)

**Nghiệp vụ thực tế:**
- Track sales team performance
- Identify trends và seasonal patterns
- Measure against targets

**Ví dụ thực tế:**
> Sales Director generate monthly report:
> - Total Revenue: $520K (target: $500K) ✅
> - Deals Won: 15 (avg: 13)
> - Win Rate: 60% (target: 55%) ✅
> - Trend: Revenue increasing 3 months straight
> → Team performing well, increase Q1 target by 10%

---

### 3. Pipeline Analysis Report (Phân tích pipeline)

**Nghiệp vụ thực tế:**
- Understand pipeline health
> - Identify bottlenecks
- Forecast future revenue

**Ví dụ thực tế:**
> Sales VP review pipeline:
> - Total pipeline: $4M
> - Weighted (realistic): $1.8M
> - Prospecting stage: Only $500K (needs more leads!)
> - Late stages (Proposal+): $2.75M (65% of pipeline) ✓
> → Action: Run lead generation campaign
> → Forecast: $900K likely to close this quarter

---

### 4. Lead Conversion Report (Báo cáo chuyển đổi leads)

**Nghiệp vụ thực tế:**
- Evaluate lead quality by source
- Optimize marketing spend
- Improve lead qualification process

**Ví dụ thực tế:**
> Marketing Manager analyze leads:
> - Website leads: 450 total, 22% conversion
> - Referrals: 320 total, 28.1% conversion ✅ (best!)
> - Email campaigns: 280 total, 15% conversion (low)
> - Hot leads: 49.7% conversion ✅
> - Cold leads: 6% conversion (waste of time)
> 
> **Action:**
> - Invest more in referral program
> - Improve email campaign targeting
> - Stop pursuing cold leads, focus on hot/warm

---

### 5. Ticket Analytics Report (Phân tích support tickets)

**Nghiệp vụ thực tế:**
- Measure support team efficiency
- Ensure SLA compliance
- Identify common issues

**Ví dụ thực tế:**
> Support Manager monthly review:
> - Total tickets: 1,456
> - Avg resolution: 18.5 hours (target: 24h) ✅
> - SLA compliance: 95.2% (target: 95%) ✅
> - Critical tickets: 2.1h avg resolution (excellent!)
> - Customer satisfaction: 4.3/5.0 ✅
> 
> **Top issues:**
> - Login problems: 234 tickets (16%)
> - Feature questions: 432 tickets (30%)
> 
> **Action:**
> - Create self-service guide for login issues
> - Update feature documentation

---

## Best Practices

### 1. Schedule Regular Reports

```csharp
// Auto-generate and email reports
[Hangfire Background Job - Every Monday 8 AM]
public async Task GenerateWeeklyReport()
{
    var report = await GenerateSalesPerformanceReport();
    
    await EmailService.SendAsync(
        to: "sales-team@company.com",
        subject: "Weekly Sales Performance Report",
        body: FormatReport(report),
        attachments: new[] { ExportToPdf(report) }
    );
}
```

### 2. Export to Multiple Formats

```csharp
// Support PDF, Excel, CSV exports
public async Task<FileResult> ExportReport(
    ReportType type, 
    ExportFormat format)
{
    var data = await GenerateReportData(type);
    
    return format switch
    {
        ExportFormat.PDF => GeneratePdf(data),
        ExportFormat.Excel => GenerateExcel(data),
        ExportFormat.CSV => GenerateCsv(data),
        _ => throw new ArgumentException("Invalid format")
    };
}
```

### 3. Cache Heavy Reports

```csharp
// Cache reports to improve performance
[ResponseCache(Duration = 3600)] // 1 hour
public async Task<IActionResult> GetSalesPerformance()
{
    var cacheKey = $"report:sales:{DateTime.Today:yyyyMMdd}";
    
    var cached = await cache.GetAsync(cacheKey);
    if (cached != null) return Ok(cached);
    
    var report = await GenerateReport();
    await cache.SetAsync(cacheKey, report, TimeSpan.FromHours(1));
    
    return Ok(report);
}
```

### 4. Add Date Range Filters

```csharp
// Always allow custom date ranges
public async Task<Report> GenerateReport(
    DateTime? startDate = null,
    DateTime? endDate = null)
{
    // Default to last 30 days if not specified
    var start = startDate ?? DateTime.UtcNow.AddDays(-30);
    var end = endDate ?? DateTime.UtcNow;
    
    // Generate report for date range
    return await QueryData(start, end);
}
```

---

## Technical Overview

**Base URL:** `/api/v1/reports`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Dashboard Summary

Tổng quan metrics của toàn bộ CRM.

```
GET /api/v1/reports/dashboard
```

**Permission Required:** `report.view`

#### Response

```json
{
  "success": true,
  "data": {
    "totalCustomers": 1234,
    "newCustomersThisMonth": 28,
    "totalLeads": 456,
    "newLeadsThisMonth": 89,
    "openOpportunities": 145,
    "totalPipelineValue": 4050000,
    "weightedPipelineValue": 1823000,
    "wonOpportunitiesThisMonth": 12,
    "revenueWonThisMonth": 450000,
    "openTickets": 45,
    "overdueTickets": 3,
    "overdueActivities": 23,
    "activitiesDueToday": 45
  }
}
```

---

### 2. Sales Performance Report

Báo cáo hiệu quả bán hàng theo thời gian.

```
GET /api/v1/reports/sales-performance?startDate={date}&endDate={date}
```

**Permission Required:** `report.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `startDate` | DateTime | No | 12 months ago | Start date |
| `endDate` | DateTime | No | Today | End date |

#### Response

```json
{
  "success": true,
  "data": {
    "totalWon": 45,
    "totalLost": 32,
    "totalRevenue": 1575000,
    "averageDealSize": 35000,
    "winRate": 58.4,
    "monthlyData": [
      {
        "year": 2025,
        "month": 10,
        "revenue": 480000,
        "dealsWon": 14
      },
      {
        "year": 2025,
        "month": 11,
        "revenue": 520000,
        "dealsWon": 15
      },
      {
        "year": 2025,
        "month": 12,
        "revenue": 575000,
        "dealsWon": 16
      }
    ]
  }
}
```

---

### 3. Pipeline Analysis Report

Phân tích chi tiết sales pipeline.

```
GET /api/v1/reports/pipeline-analysis?pipelineId={guid}
```

**Permission Required:** `report.view`

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `pipelineId` | Guid | No | Filter by specific pipeline |

#### Response

```json
{
  "success": true,
  "data": {
    "totalOpportunities": 145,
    "totalValue": 4050000,
    "weightedValue": 1823000,
    "averageValue": 27931,
    "averageProbability": 45,
    "byStage": [
      {
        "stageId": "stage-guid",
        "stageName": "Prospecting",
        "sortOrder": 1,
        "count": 50,
        "value": 500000,
        "weightedValue": 50000
      },
      {
        "stageId": "stage-guid",
        "stageName": "Qualification",
        "sortOrder": 2,
        "count": 40,
        "value": 800000,
        "weightedValue": 200000
      }
    ]
  }
}
```

---

### 4. Lead Conversion Report

Báo cáo chuyển đổi leads.

```
GET /api/v1/reports/lead-conversion?startDate={date}&endDate={date}
```

**Permission Required:** `report.view`

#### Response

```json
{
  "success": true,
  "data": {
    "totalLeads": 1234,
    "convertedLeads": 222,
    "qualifiedLeads": 445,
    "unqualifiedLeads": 345,
    "lostLeads": 222,
    "conversionRate": 18.0,
    "bySource": [
      {
        "source": "Website",
        "total": 450,
        "converted": 99,
        "conversionRate": 22.0
      },
      {
        "source": "Referral",
        "total": 320,
        "converted": 90,
        "conversionRate": 28.1
      }
    ],
    "byRating": [
      {
        "rating": "Hot",
        "total": 185,
        "converted": 92,
        "conversionRate": 49.7
      },
      {
        "rating": "Warm",
        "total": 420,
        "converted": 92,
        "conversionRate": 21.9
      },
      {
        "rating": "Cold",
        "total": 629,
        "converted": 38,
        "conversionRate": 6.0
      }
    ]
  }
}
```

---

### 5. Ticket Analytics Report

Phân tích performance support team.

```
GET /api/v1/reports/ticket-analytics?startDate={date}&endDate={date}
```

**Permission Required:** `report.view`

#### Response

```json
{
  "success": true,
  "data": {
    "totalTickets": 1456,
    "openTickets": 45,
    "resolvedTickets": 1411,
    "averageResolutionTimeHours": 18.5,
    "averageFirstResponseTimeHours": 1.2,
    "slaComplianceRate": 95.2,
    "byPriority": [
      {
        "priority": "Critical",
        "count": 89,
        "averageResolutionHours": 2.1
      },
      {
        "priority": "High",
        "count": 342,
        "averageResolutionHours": 8.5
      }
    ],
    "byType": [
      {
        "type": "Problem",
        "count": 654,
        "percentage": 44.9
      },
      {
        "type": "Question",
        "count": 432,
        "percentage": 29.7
      }
    ]
  }
}
```

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `report.view` | View all reports |
| `report.export` | Export reports to PDF/Excel |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
