# Analytics API Documentation

## Tổng quan Module

### Analytics là gì?

**Analytics (Phân tích dữ liệu)** là module cung cấp insights, metrics, và KPIs (Key Performance Indicators) để đánh giá hiệu quả kinh doanh, dự báo xu hướng, và hỗ trợ ra quyết định chiến lược.

### Tại sao Analytics quan trọng?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│            KINH DOANH KHÔNG CÓ DATA = LÁI XE KHÔNG CÓ BẢN ĐỒ              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Without Analytics:                 With Analytics:                         │
│  ──────────────────                 ───────────────                         │
│  • "Sales có vẻ tốt..."            • Revenue: $1.2M (+15% vs last month)   │
│  • "Khách hàng hài lòng..."        • CSAT: 4.2/5.0 (target: 4.0)           │
│  • "Deals đang close..."           • Win rate: 35% (industry avg: 25%)     │
│  • "Team làm việc chăm chỉ..."     • Sales cycle: 45 days (down 10 days)   │
│                                                                             │
│  ❌ Vague, subjective               ✅ Precise, objective                    │
│  ❌ Không thể compare               ✅ Trackable over time                   │
│  ❌ Không biết vấn đề ở đâu         ✅ Identify bottlenecks                  │
│  ❌ Không dự báo được               ✅ Forecast & trend analysis             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Dashboard Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CRM ANALYTICS DASHBOARD                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      KPI CARDS (Top Row)                            │   │
│  ├─────────────┬─────────────┬─────────────┬─────────────────────────┤   │
│  │   Revenue   │    Leads    │  Customers  │       Support           │   │
│  │             │             │             │                         │   │
│  │   $1.2M     │     456     │     1,234   │   Open Tickets: 45      │   │
│  │  ↑ +15%     │  ↑ +25%     │  ↑ +10%     │   Avg Response: 2.3h    │   │
│  └─────────────┴─────────────┴─────────────┴─────────────────────────┘   │
│                                                                             │
│  ┌──────────────────────────────────────┐  ┌─────────────────────────┐    │
│  │      Revenue Trend (12 months)       │  │   Pipeline by Stage     │    │
│  │                                      │  │                         │    │
│  │  $200K ┤           ╱─╲               │  │  Prospecting:   $500K   │    │
│  │        │         ╱   ╲               │  │  Qualification: $800K   │    │
│  │  $150K ┤       ╱     ╲╲              │  │  Proposal:      $1.2M   │    │
│  │        │     ╱       ╲ ╲             │  │  Negotiation:   $900K   │    │
│  │  $100K ┤   ╱           ╲             │  │  Closed Won:    $600K   │    │
│  │        └───────────────────────      │  │                         │    │
│  │          Q1  Q2  Q3  Q4  Q1           │  │  Total: $4M pipeline    │    │
│  └──────────────────────────────────────┘  └─────────────────────────┘    │
│                                                                             │
│  ┌──────────────────────────────────────┐  ┌─────────────────────────┐    │
│  │       Win Rate by Stage              │  │   Top Sales Reps        │    │
│  │                                      │  │                         │    │
│  │  Prospecting:    85% ████████████    │  │  John Smith:   $850K    │    │
│  │  Qualification:  70% ██████████      │  │  Jane Doe:     $720K    │    │
│  │  Proposal:       50% ████████        │  │  Bob Wilson:   $680K    │    │
│  │  Negotiation:    40% ██████          │  │  Alice Brown:  $540K    │    │
│  │  Overall:        35% █████           │  │                         │    │
│  └──────────────────────────────────────┘  └─────────────────────────┘    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Key Metrics & KPIs

### Sales Metrics

| Metric | Formula | Target | What It Tells |
|--------|---------|--------|---------------|
| **Total Revenue** | Sum(Order.Amount) | Budget goal | Tổng doanh thu |
| **Revenue Growth** | (Current - Previous) / Previous × 100% | +10% MoM | Tốc độ tăng trưởng |
| **Pipeline Value** | Sum(Open Opportunity.Amount) | 3x quota | Tiềm năng doanh thu |
| **Win Rate** | Won / (Won + Lost) × 100% | 30-40% | Tỷ lệ chốt deal thành công |
| **Average Deal Size** | Total Revenue / Deals Won | Varies | Giá trị trung bình mỗi deal |
| **Sales Cycle** | Avg(CloseDate - CreatedDate) | <60 days | Thời gian chốt deal |

### Lead Metrics

| Metric | Formula | Target | What It Tells |
|--------|---------|--------|---------------|
| **Lead Conversion Rate** | Converted Leads / Total Leads × 100% | 15-20% | Chất lượng leads |
| **Lead Velocity** | (New Leads This Month - Last Month) / Last Month × 100% | +5% MoM | Tốc độ tăng trưởng leads |
| **Cost Per Lead (CPL)** | Marketing Spend / Leads Generated | Varies | Chi phí thu hút 1 lead |
| **Lead to Customer** | Customers / Leads × 100% | 10-15% | Tỷ lệ chuyển đổi leads → customers |

### Customer Metrics

| Metric | Formula | Target | What It Tells |
|--------|---------|--------|---------------|
| **Customer Lifetime Value (CLV)** | Avg Revenue Per Customer × Avg Lifetime | Varies | Giá trị 1 customer trong cả lifetime |
| **Churn Rate** | Lost Customers / Total Customers × 100% | <5% annually | Tỷ lệ mất khách hàng |
| **Customer Acquisition Cost (CAC)** | Sales & Marketing Spend / New Customers | CLV > 3×CAC | Chi phí thu hút 1 customer mới |
| **Net Promoter Score (NPS)** | % Promoters - % Detractors | >50 | Khách hàng có recommend không |

### Support Metrics

| Metric | Formula | Target | What It Tells |
|--------|---------|--------|---------------|
| **First Response Time** | Avg(FirstResponse - TicketCreated) | <1 hour | Tốc độ phản hồi |
| **Average Resolution Time** | Avg(Closed - Created) | <24 hours | Thời gian giải quyết |
| **SLA Compliance** | Met SLA / Total Tickets × 100% | >95% | Tuân thủ cam kết |
| **CSAT (Customer Satisfaction)** | Avg(Satisfaction Rating) | >4.0/5.0 | Mức độ hài lòng |

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Get Dashboard Metrics (Tổng quan Dashboard)

**Nghiệp vụ thực tế:**
- Executive dashboard với tất cả KPIs quan trọng
- One-page overview toàn bộ business health

**Ví dụ thực tế:**
> CEO login mỗi sáng, xem dashboard:
> - **Sales:** Revenue $1.2M (+15% vs last month), Pipeline $4M, Win rate 35%
> - **Leads:** 456 leads (+25%), Conversion rate 18%, 120 hot leads
> - **Customers:** 1,234 customers (+10%), Churn 3%, CLV $15K
> - **Support:** 45 open tickets, Avg resolution 2.3h, CSAT 4.2/5
> - **Revenue Chart:** Trend đi lên 6 tháng liên tiếp
> - **Pipeline Chart:** $1.2M ở stage Proposal (sắp close)
> → Business đang healthy, focus vào converting Proposal stage

---

### 2. Get Sales Cycle Analytics (Phân tích chu kỳ bán hàng)

**Nghiệp vụ thực tế:**
- Đo thời gian từ lead → close deal
- Identify bottlenecks trong sales process
- Compare performance giữa các sales reps

**Ví dụ thực tế:**
> Sales Manager analyze sales cycle:
> - **Average Cycle:** 45 days (target: 60 days) ✅
> - **Median Cycle:** 38 days (50% deals close trong 38 ngày)
> - **Fastest:** 15 days (deal nhỏ, decision maker duy nhất)
> - **Longest:** 180 days (enterprise deal, nhiều stakeholders)
> 
> **Stage Breakdown:**
> - Prospecting: 5 days avg
> - Qualification: 8 days avg
> - Proposal: 15 days avg (BOTTLENECK!)
> - Negotiation: 10 days avg
> - Closing: 7 days avg
> 
> **Action:** Focus on speeding up Proposal stage:
> - Create proposal templates
> - Automate approval workflows
> - Train reps on value selling

---

### 3. Get Win Rate Analytics (Phân tích tỷ lệ thắng)

**Nghiệp vụ thực tế:**
- Measure success rate của sales team
- Identify top performers và weak performers
- Understand why deals are won/lost

**Ví dụ thực tế:**
> Sales Director review win rate:
> - **Overall Win Rate:** 35% (industry avg: 25%) ✅
> - **Total Won:** 120 deals ($4.2M)
> - **Total Lost:** 220 deals
> - **Avg Won Value:** $35K
> - **Avg Lost Value:** $28K
> 
> **Win Rate by Stage:**
> - Prospecting → Qualification: 85%
> - Qualification → Proposal: 70%
> - Proposal → Negotiation: 50%
> - Negotiation → Close: 40%
> 
> **Win Rate by Rep:**
> - John Smith: 45% (top performer!)
> - Jane Doe: 38%
> - Bob Wilson: 32%
> - Alice Brown: 28% (needs coaching)
> 
> **Top Win Reasons:**
> - Product features: 45%
> - Price competitive: 30%
> - Customer service: 15%
> 
> **Top Loss Reasons:**
> - Price too high: 40%
> - Competitor chosen: 35%
> - Timing not right: 15%
> 
> **Action:**
> - Coach Alice Brown (win rate thấp)
> - Emphasize product features in sales pitch
> - Review pricing strategy (40% lost due to price)

---

### 4. Get Cohort Analytics (Phân tích nhóm khách hàng)

**Nghiệp vụ thực tế:**
- Track customer retention theo cohort (nhóm join cùng thời kỳ)
- Measure lifetime value của từng cohort
- Predict churn patterns

**Ví dụ thực tế:**
> Product Manager analyze Q1 2024 cohort:
> - **Cohort:** Q1 2024 (Jan-Mar 2024)
> - **New Customers:** 150
> - **Active Now (Month 10):** 120
> - **Churned:** 30
> - **Retention Rate:** 80% (excellent!)
> - **Avg LTV:** $18,500
> - **Total Revenue:** $2.7M
> 
> **Retention by Month:**
> | Month | Active | Retention | Revenue |
> |-------|--------|-----------|---------|
> | 0 (Jan 24) | 150 | 100% | $225K |
> | 1 (Feb 24) | 145 | 97% | $218K |
> | 3 (Apr 24) | 138 | 92% | $210K |
> | 6 (Jul 24) | 130 | 87% | $195K |
> | 10 (Nov 24) | 120 | 80% | $180K |
> 
> **Insights:**
> - Month 0-1: 3% churn (onboarding issues)
> - Month 1-3: 5% churn (product fit)
> - Month 3-6: 5% churn (value realization)
> - Month 6+: Stable (2-3% churn)
> 
> **Action:**
> - Improve onboarding (reduce Month 0-1 churn)
> - Quarterly business reviews (retain after Month 6)

---

### 5. Get Customer Lifetime Analytics (Phân tích giá trị khách hàng)

**Nghiệp vụ thực tế:**
- Calculate CLV (Customer Lifetime Value)
- Segment customers by value
- Optimize customer acquisition spend

**Ví dụ thực tế:**
> CFO review customer lifetime value:
> - **Total Customers:** 1,234
> - **Average Lifetime:** 3.2 years
> - **Average LTV:** $15,000
> - **Total LTV:** $18.5M
> 
> **Segment Breakdown:**
> | Segment | Count | Avg LTV | Total Revenue | % |
> |---------|-------|---------|---------------|---|
> | Enterprise | 50 | $85K | $4.25M | 23% |
> | Mid-Market | 284 | $25K | $7.1M | 38% |
> | SMB | 900 | $8K | $7.2M | 39% |
> 
> **Insights:**
> - Enterprise: 4% customers = 23% revenue (high value!)
> - Mid-Market: Best segment (good LTV, scalable)
> - SMB: High churn, lower LTV
> 
> **LTV vs CAC:**
> - Enterprise: LTV $85K, CAC $15K → Ratio 5.7:1 ✅
> - Mid-Market: LTV $25K, CAC $5K → Ratio 5:1 ✅
> - SMB: LTV $8K, CAC $3K → Ratio 2.7:1 ⚠️
> 
> **Action:**
> - Increase investment in Enterprise sales
> - Optimize SMB acquisition (reduce CAC or increase LTV)
> - Mid-Market is sweet spot, maintain current strategy

---

## Visualization Examples

### Revenue Trend Chart

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    REVENUE TREND - 12 MONTHS                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  $1.4M ┤                                                      ╱─╲           │
│        │                                                    ╱   ╲           │
│  $1.2M ┤                                      ╱──╲         ╱     ╲          │
│        │                                    ╱    ╲       ╱       ╲         │
│  $1.0M ┤                        ╱──╲      ╱      ╲     ╱         ╲──╲      │
│        │                      ╱    ╲    ╱        ╲   ╱               ╲     │
│  $800K ┤            ╱──╲    ╱      ╲  ╱          ╲ ╱                 ╲    │
│        │          ╱    ╲  ╱        ╲╱            ╲╱                   ╲   │
│  $600K ┤    ╱──╲╱      ╲╱                                               ╲  │
│        │  ╱                                                               ╲│
│  $400K └────────────────────────────────────────────────────────────────────│
│         J  F  M  A  M  J  J  A  S  O  N  D  J  F  M  A  M  J             │
│        2024                            2025                                 │
│                                                                             │
│  Insights:                                                                  │
│  • Seasonal peak: December (holiday sales)                                  │
│  • Growth trend: +5% avg per month                                          │
│  • Q1 2025 strong start ($1.2M → $1.3M → $1.4M)                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Pipeline Funnel

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SALES PIPELINE FUNNEL                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌────────────────────────────────────────────────────────────┐            │
│  │         Prospecting: 200 opps ($2M)                         │            │
│  │         ████████████████████████████████████████████        │            │
│  └────────────────────────────────┬───────────────────────────┘            │
│                                   │ 85% conversion                          │
│                                   ▼                                         │
│       ┌────────────────────────────────────────────────┐                   │
│       │    Qualification: 170 opps ($1.7M)             │                   │
│       │    ████████████████████████████████████        │                   │
│       └────────────────┬───────────────────────────────┘                   │
│                        │ 70% conversion                                     │
│                        ▼                                                    │
│          ┌──────────────────────────────────────┐                          │
│          │  Proposal: 119 opps ($1.4M)          │                          │
│          │  ████████████████████████            │                          │
│          └─────────┬────────────────────────────┘                          │
│                    │ 50% conversion                                         │
│                    ▼                                                        │
│             ┌────────────────────────────┐                                 │
│             │  Negotiation: 60 opps ($900K) │                              │
│             │  ████████████████          │                                 │
│             └──────┬─────────────────────┘                                 │
│                    │ 40% conversion                                         │
│                    ▼                                                        │
│                ┌─────────────────┐                                         │
│                │ Closed Won: 24  │                                         │
│                │   ($600K)       │                                         │
│                │ ████████        │                                         │
│                └─────────────────┘                                         │
│                                                                             │
│  Overall Conversion: 12% (24 won / 200 total)                              │
│  Weighted Pipeline: $1.8M (considering probability at each stage)          │
│  Expected Close This Quarter: $600K                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Best Practices

### 1. Set Realistic Targets

```javascript
// ❌ BAD: Arbitrary targets
const targets = {
    revenue: 10000000, // $10M (no justification)
    winRate: 80 // 80% (unrealistic)
};

// ✅ GOOD: Data-driven targets
const targets = {
    // Based on historical +10% growth + market expansion
    revenue: lastYearRevenue * 1.15,
    
    // Based on industry benchmark (30-40%) + team improvement
    winRate: 0.35,
    
    // Based on historical data + process improvements
    salesCycle: historicalAvg * 0.85 // Reduce 15%
};
```

### 2. Segment Your Data

```javascript
// ❌ BAD: One-size-fits-all metrics
const avgDealSize = totalRevenue / totalDeals;

// ✅ GOOD: Segment by customer type
const metrics = {
    enterprise: {
        avgDealSize: 85000,
        salesCycle: 120, // days
        winRate: 0.25
    },
    midMarket: {
        avgDealSize: 25000,
        salesCycle: 60,
        winRate: 0.35
    },
    smb: {
        avgDealSize: 8000,
        salesCycle: 30,
        winRate: 0.45
    }
};
```

### 3. Track Leading vs Lagging Indicators

```
Leading Indicators (Predict future):
────────────────────────────────────
• Pipeline value
• Lead velocity
• Activity metrics (calls, meetings)
• New opportunities created
→ Act on these NOW to influence future results

Lagging Indicators (Historical):
─────────────────────────────────
• Revenue
• Churn rate
• Customer satisfaction
• Deals closed
→ Learn from these, but can't change the past
```

### 4. Use Cohort Analysis

```javascript
// Track cohorts over time
const cohortAnalysis = {
    "2024-Q1": {
        month0: { customers: 150, retention: 100%, revenue: 225000 },
        month1: { customers: 145, retention: 97%, revenue: 218000 },
        month3: { customers: 138, retention: 92%, revenue: 210000 },
        month6: { customers: 130, retention: 87%, revenue: 195000 },
        month12: { customers: 120, retention: 80%, revenue: 180000 }
    }
};

// Identify churn patterns
// Month 0-1: Onboarding issues
// Month 3-6: Value realization problems
// Month 12+: Competitive pressure
```

### 5. Automate Reporting

```csharp
// Schedule automatic reports
[Hangfire Background Job - Daily 8 AM]
public async Task GenerateDailyMetrics()
{
    var metrics = await _analyticsService.GetDashboardMetricsAsync();
    
    // Send to stakeholders
    await SendReportAsync(
        recipients: new[] { "ceo@company.com", "sales@company.com" },
        subject: $"Daily Metrics - {DateTime.Now:yyyy-MM-dd}",
        content: FormatMetricsReport(metrics)
    );
    
    // Store for historical tracking
    await _metricsRepository.SaveSnapshotAsync(metrics);
}
```

---

## Technical Overview

**Base URL:** `/api/v1/analytics`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get Dashboard Metrics

Lấy comprehensive dashboard metrics.

```
GET /api/v1/analytics/dashboard?startDate={date}&endDate={date}
```

**Permission Required:** `analytics.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `startDate` | DateTime | No | 30 days ago | Start date |
| `endDate` | DateTime | No | Today | End date |

#### Response

```json
{
  "success": true,
  "data": {
    "sales": {
      "totalRevenue": 1200000,
      "revenueThisMonth": 150000,
      "revenueThisQuarter": 380000,
      "revenueGrowth": 15.5,
      "openOpportunities": 145,
      "pipelineValue": 4000000,
      "averageDealSize": 35000,
      "winRate": 35.2,
      "averageSalesCycle": 45.3,
      "dealsWonThisMonth": 12,
      "dealsLostThisMonth": 8
    },
    "leads": {
      "totalLeads": 456,
      "newLeadsThisMonth": 89,
      "qualifiedLeads": 234,
      "conversionRate": 18.5,
      "averageLeadScore": 72,
      "hotLeads": 45,
      "leadsConverted": 34,
      "leadsBySource": [
        { "source": "Website", "count": 180, "conversionRate": 22 },
        { "source": "Referral", "count": 120, "conversionRate": 28 },
        { "source": "Email Campaign", "count": 90, "conversionRate": 15 }
      ]
    },
    "customers": {
      "totalCustomers": 1234,
      "newCustomersThisMonth": 28,
      "activeCustomers": 1180,
      "churnedCustomers": 15,
      "churnRate": 3.2,
      "customerLifetimeValue": 15000,
      "customerAcquisitionCost": 2500,
      "customersAtRisk": 48
    },
    "support": {
      "openTickets": 45,
      "closedThisMonth": 234,
      "averageResolutionTime": 2.3,
      "averageFirstResponseTime": 0.8,
      "overdueSla": 3,
      "customerSatisfaction": 4.2,
      "ticketsByPriority": [
        { "priority": "Critical", "count": 5, "averageResolutionHours": 1.2 },
        { "priority": "High", "count": 18, "averageResolutionHours": 4.5 },
        { "priority": "Medium", "count": 22, "averageResolutionHours": 12.3 }
      ]
    },
    "activities": {
      "totalActivities": 567,
      "overdueTasks": 23,
      "dueToday": 45,
      "completedThisWeek": 123,
      "scheduledCalls": 34,
      "scheduledMeetings": 28
    },
    "revenueChart": [
      { "period": "2024-07", "revenue": 95000, "dealsWon": 8, "target": 100000 },
      { "period": "2024-08", "revenue": 110000, "dealsWon": 10, "target": 100000 },
      { "period": "2024-09", "revenue": 125000, "dealsWon": 11, "target": 120000 }
    ],
    "pipelineChart": [
      { "stageName": "Prospecting", "count": 50, "totalValue": 500000, "probability": 10, "weightedValue": 50000 },
      { "stageName": "Qualification", "count": 40, "totalValue": 800000, "probability": 25, "weightedValue": 200000 },
      { "stageName": "Proposal", "count": 30, "totalValue": 1200000, "probability": 50, "weightedValue": 600000 }
    ]
  }
}
```

---

### 2. Get Sales Cycle Analytics

Phân tích chu kỳ bán hàng.

```
GET /api/v1/analytics/sales-cycle?startDate={date}&endDate={date}
```

#### Response

```json
{
  "success": true,
  "data": {
    "averageCycleDays": 45.3,
    "medianCycleDays": 38.0,
    "shortestCycleDays": 15.0,
    "longestCycleDays": 180.0,
    "totalClosedOpportunities": 145,
    "stageBreakdown": [
      { "stageName": "Prospecting", "averageDays": 5.2, "medianDays": 4.0, "opportunitiesCount": 145 },
      { "stageName": "Qualification", "averageDays": 8.1, "medianDays": 7.0, "opportunitiesCount": 132 },
      { "stageName": "Proposal", "averageDays": 15.4, "medianDays": 12.0, "opportunitiesCount": 98 }
    ],
    "cycleByRep": [
      { "repId": "guid", "repName": "John Smith", "averageCycleDays": 38.2, "opportunitiesCount": 45, "winRate": 42.5 },
      { "repId": "guid", "repName": "Jane Doe", "averageCycleDays": 42.8, "opportunitiesCount": 38, "winRate": 38.2 }
    ],
    "cycleByStage": [
      { "stageName": "Proposal", "averageDays": 15.4, "opportunitiesCount": 98, "conversionRate": 52.3 }
    ]
  }
}
```

---

### 3. Get Win Rate Analytics

Phân tích tỷ lệ thắng.

```
GET /api/v1/analytics/win-rate?startDate={date}&endDate={date}
```

#### Response

```json
{
  "success": true,
  "data": {
    "overallWinRate": 35.2,
    "totalWon": 120,
    "totalLost": 220,
    "totalOpen": 145,
    "averageWonValue": 35000,
    "averageLostValue": 28000,
    "winRateByStage": [
      { "stageName": "Prospecting", "winRate": 85.0, "totalOpportunities": 200, "won": 170, "lost": 30 },
      { "stageName": "Qualification", "winRate": 70.0, "totalOpportunities": 170, "won": 119, "lost": 51 }
    ],
    "winRateByRep": [
      { "repId": "guid", "repName": "John Smith", "winRate": 45.0, "totalOpportunities": 60, "won": 27, "lost": 33, "totalWonValue": 945000 },
      { "repId": "guid", "repName": "Jane Doe", "winRate": 38.2, "totalOpportunities": 55, "won": 21, "lost": 34, "totalWonValue": 735000 }
    ],
    "winRateByProduct": [
      { "productName": "Enterprise Suite", "winRate": 42.0, "totalOpportunities": 50, "won": 21, "lost": 29 },
      { "productName": "Professional Plan", "winRate": 38.5, "totalOpportunities": 78, "won": 30, "lost": 48 }
    ],
    "topWinReasons": [
      { "reason": "Product features", "count": 54, "percentage": 45.0 },
      { "reason": "Competitive price", "count": 36, "percentage": 30.0 },
      { "reason": "Excellent support", "count": 18, "percentage": 15.0 }
    ],
    "topLossReasons": [
      { "reason": "Price too high", "count": 88, "percentage": 40.0 },
      { "reason": "Competitor chosen", "count": 77, "percentage": 35.0 },
      { "reason": "Timing not right", "count": 33, "percentage": 15.0 }
    ]
  }
}
```

---

### 4. Get Cohort Analytics

Phân tích retention theo cohort.

```
GET /api/v1/analytics/cohort/{cohortPeriod}
```

**Parameters:**
- `cohortPeriod`: Format "2024-01" (monthly) or "2024-Q1" (quarterly)

#### Response

```json
{
  "success": true,
  "data": {
    "cohortPeriod": "2024-Q1",
    "newCustomers": 150,
    "activeCustomers": 120,
    "churnedCustomers": 30,
    "churnRate": 20.0,
    "retentionRate": 80.0,
    "averageLifetimeValue": 18500,
    "totalRevenue": 2775000,
    "averageRevenuePerCustomer": 18500,
    "retentionByMonth": [
      { "monthNumber": 0, "monthLabel": "Jan 2024", "activeCustomers": 150, "retentionRate": 100.0, "revenue": 225000 },
      { "monthNumber": 1, "monthLabel": "Feb 2024", "activeCustomers": 145, "retentionRate": 96.7, "revenue": 218000 },
      { "monthNumber": 3, "monthLabel": "Apr 2024", "activeCustomers": 138, "retentionRate": 92.0, "revenue": 210000 },
      { "monthNumber": 6, "monthLabel": "Jul 2024", "activeCustomers": 130, "retentionRate": 86.7, "revenue": 195000 },
      { "monthNumber": 10, "monthLabel": "Nov 2024", "activeCustomers": 120, "retentionRate": 80.0, "revenue": 180000 }
    ]
  }
}
```

---

### 5. Get Customer Lifetime Analytics

Phân tích giá trị lifetime khách hàng.

```
GET /api/v1/analytics/customer-lifetime
```

#### Response

```json
{
  "success": true,
  "data": {
    "totalCustomers": 1234,
    "averageLifetimeDays": 1168,
    "averageLifetimeValue": 15000,
    "totalLifetimeValue": 18510000,
    "segmentBreakdown": [
      { "segmentName": "Enterprise", "customerCount": 50, "averageLTV": 85000, "totalRevenue": 4250000, "percentage": 23.0 },
      { "segmentName": "Mid-Market", "customerCount": 284, "averageLTV": 25000, "totalRevenue": 7100000, "percentage": 38.4 },
      { "segmentName": "SMB", "customerCount": 900, "averageLTV": 8000, "totalRevenue": 7200000, "percentage": 38.9 }
    ]
  }
}
```

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `analytics.view` | View analytics & dashboards |
| `analytics.export` | Export analytics data |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
