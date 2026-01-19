# Campaigns API Documentation

## Tổng quan Module

### Campaign là gì?

**Campaign (Chiến dịch Marketing)** là một hoạt động marketing có tổ chức, có mục tiêu rõ ràng, được thực hiện trong một khoảng thời gian nhất định để tiếp cận khách hàng tiềm năng, tạo leads, và tăng doanh thu. Campaign có thể là email marketing, SMS, social media ads, events, webinars, v.v.

### Tại sao Campaign quan trọng?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   CAMPAIGNS = SYSTEMATIC CUSTOMER OUTREACH                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Without Campaigns:               With Campaigns:                           │
│  ──────────────────               ────────────────                          │
│  ❌ Random outreach               ✅ Organized, targeted marketing          │
│  ❌ No tracking                   ✅ ROI tracking: Spend $5K, earn $50K     │
│  ❌ Không biết gì hiệu quả        ✅ Analytics: 25% open, 5% conversion     │
│  ❌ Lặp lại manual                ✅ Automated email sequences              │
│  ❌ No lead source tracking       ✅ Know which campaign generated which lead│
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Campaign Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CAMPAIGN LIFECYCLE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────┐                                                               │
│  │  DRAFT   │  Planning phase: Create campaign, define target audience     │
│  └────┬─────┘  Set budget, expected results                                │
│       │                                                                     │
│       ▼                                                                     │
│  ┌──────────┐                                                               │
│  │SCHEDULED │  Scheduled for future: StartDate not reached yet             │
│  └────┬─────┘  Waiting to start                                            │
│       │                                                                     │
│       ▼                                                                     │
│  ┌──────────┐                                                               │
│  │IN PROGRESS│ Active: Sending emails, running ads, tracking results       │
│  └────┬─────┘  Can pause/resume                                            │
│       │                                                                     │
│       ├───────────┐                                                         │
│       │           ▼                                                         │
│       │      ┌──────────┐                                                   │
│       │      │  PAUSED  │  Temporarily stopped                             │
│       │      └────┬─────┘  Can resume later                                │
│       │           │                                                         │
│       │           ▼                                                         │
│       │      Resume → Back to IN PROGRESS                                  │
│       │                                                                     │
│       ▼                                                                     │
│  ┌──────────┐                                                               │
│  │COMPLETED │  Ended: EndDate reached or manually completed                │
│  └──────────┘  Analyze ROI, results                                        │
│       │                                                                     │
│       ▼                                                                     │
│  Report: Budget vs Actual Cost, Expected vs Actual ROI                     │
│                                                                             │
│  ┌──────────┐                                                               │
│  │CANCELLED │  Terminated: Campaign không chạy nữa                         │
│  └──────────┘  No results tracking                                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Campaign Types

### Phân loại theo channel

| Type | Description | Use Case | Avg Budget |
|------|-------------|----------|------------|
| **Email** | Email marketing campaigns | Newsletter, nurture sequences, product launches | $500-5K |
| **SMS** | Text message campaigns | Flash sales, appointment reminders, alerts | $200-2K |
| **Social** | Social media advertising | Facebook/LinkedIn ads, brand awareness | $1K-50K |
| **Event** | Physical/virtual events | Trade shows, conferences, meetups | $5K-100K |
| **Webinar** | Online seminars | Product demos, educational content | $500-5K |
| **Advertisement** | Paid advertising | Google Ads, display ads, retargeting | $2K-100K |
| **Other** | Custom campaigns | - | Variable |

---

## Campaign Metrics & ROI

### Core Metrics Tracked

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          CAMPAIGN METRICS                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  FINANCIAL METRICS:                                                         │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Budget:           $10,000                                                  │
│  Actual Cost:      $8,500                                                   │
│  Expected Revenue: $50,000                                                  │
│  Actual Revenue:   $65,000                                                  │
│  ROI:              665% = (($65K - $8.5K) / $8.5K) * 100                   │
│  Cost per Lead:    $85 = $8,500 / 100 leads                                │
│  Cost per Conversion: $850 = $8,500 / 10 conversions                       │
│                                                                             │
│  ═══════════════════════════════════════════════════════════════════════    │
│                                                                             │
│  ENGAGEMENT METRICS (Email Campaign):                                       │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Total Sent:       1,000 emails                                             │
│  Total Delivered:  980 emails (98% delivery rate)                           │
│  Total Opened:     245 emails (25% open rate)                               │
│  Total Clicked:    49 clicks (20% click rate)                               │
│  Total Bounced:    20 emails (2% bounce rate)                               │
│  Total Unsubscribed: 5 (0.5% unsubscribe rate)                             │
│                                                                             │
│  ═══════════════════════════════════════════════════════════════════════    │
│                                                                             │
│  CONVERSION METRICS:                                                        │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Expected Leads:       120 leads                                            │
│  Total Leads Generated: 100 leads (83% of target)                           │
│  Expected Conversions:  12 deals                                            │
│  Total Conversions:     10 deals (83% of target)                            │
│  Conversion Rate:       10% = (10 / 100) * 100                             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### ROI Calculation

```
ROI Formula:
────────────
ROI = ((Actual Revenue - Actual Cost) / Actual Cost) * 100

Example:
────────
Campaign: "Q1 Product Launch"
Budget: $10,000
Actual Cost: $8,500
Actual Revenue: $65,000

ROI = (($65,000 - $8,500) / $8,500) * 100
    = ($56,500 / $8,500) * 100
    = 6.65 * 100
    = 665%

Interpretation: Every $1 spent generated $7.65 in revenue
```

---

## Email Campaign Funnel

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        EMAIL CAMPAIGN FUNNEL                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. SENT                                                                    │
│     ┌───────────────────────────────────────────────┐                      │
│     │ 1,000 emails sent                             │                      │
│     └───────────────────────┬───────────────────────┘                      │
│                             │                                               │
│                             ├─────► BOUNCED (20) ─► Remove from list       │
│                             │                                               │
│                             ▼                                               │
│  2. DELIVERED                                                               │
│     ┌───────────────────────────────────────────────┐                      │
│     │ 980 emails (98% delivery rate)                │                      │
│     └───────────────────────┬───────────────────────┘                      │
│                             │                                               │
│                             ▼                                               │
│  3. OPENED                                                                  │
│     ┌───────────────────────────────────────────────┐                      │
│     │ 245 emails (25% open rate)                    │                      │
│     │ ✅ Good subject line                          │                      │
│     └───────────────────────┬───────────────────────┘                      │
│                             │                                               │
│                             ▼                                               │
│  4. CLICKED                                                                 │
│     ┌───────────────────────────────────────────────┐                      │
│     │ 49 clicks (20% click rate from opened)        │                      │
│     │ ✅ Compelling CTA                             │                      │
│     └───────────────────────┬───────────────────────┘                      │
│                             │                                               │
│                             ▼                                               │
│  5. RESPONDED / LEAD                                                        │
│     ┌───────────────────────────────────────────────┐                      │
│     │ 100 leads generated (10% of delivered)        │                      │
│     └───────────────────────┬───────────────────────┘                      │
│                             │                                               │
│                             ▼                                               │
│  6. CONVERTED                                                               │
│     ┌───────────────────────────────────────────────┐                      │
│     │ 10 conversions (10% conversion rate)          │                      │
│     │ $65K total revenue                            │                      │
│     └───────────────────────────────────────────────┘                      │
│                                                                             │
│  UNSUBSCRIBED (5) ─► Remove from future campaigns                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Campaign Members

### Campaign Member Tracking

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       CAMPAIGN MEMBER JOURNEY                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Lead: "John Doe" (john@example.com)                                        │
│  Campaign: "Q1 Product Launch"                                             │
│                                                                             │
│  Timeline:                                                                  │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Jan 10, 10:00 AM - Status: Pending                                        │
│                     Added to campaign                                       │
│                                                                             │
│  Jan 10, 10:30 AM - Status: Sent                                           │
│                     Email sent ✓                                            │
│                     SentAt: 2026-01-10T10:30:00Z                           │
│                                                                             │
│  Jan 10, 2:15 PM  - Status: Opened                                         │
│                     Email opened                                            │
│                     OpenedAt: 2026-01-10T14:15:00Z                         │
│                                                                             │
│  Jan 10, 2:17 PM  - Status: Clicked                                        │
│                     Clicked CTA link                                        │
│                     ClickedAt: 2026-01-10T14:17:00Z                        │
│                                                                             │
│  Jan 12, 9:00 AM  - Status: Responded                                      │
│                     Filled contact form                                     │
│                     RespondedAt: 2026-01-12T09:00:00Z                      │
│                     Response: "Interested in demo"                          │
│                                                                             │
│  Jan 20, 3:00 PM  - Status: Converted                                      │
│                     Purchased product ($5,000)                              │
│                     ConvertedAt: 2026-01-20T15:00:00Z                      │
│                     Opportunity created: "John Doe - Product Purchase"     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Member Status Flow

```
Pending → Sent → Opened → Clicked → Responded → Converted
            │        │        │
            ▼        ▼        ▼
         Bounced  Unsubscribed (anytime)
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Campaign (Create Campaign)

**Nghiệp vụ thực tế:**
- Marketing team plan new campaign
- Set budget, target audience, expected results
- Launch email/SMS/social campaigns

**Ví dụ thực tế:**
> Marketing Manager tạo email campaign:
> - Name: "Q1 Product Launch - SaaS CRM"
> - Type: Email
> - StartDate: Jan 15, 2026
> - EndDate: Feb 15, 2026
> - Budget: $10,000
> - Target Audience: "B2B Software Buyers, 100-500 employees"
> - Expected Leads: 120
> - Expected Conversions: 12
> - Expected Revenue: $50,000
> → Campaign created in Draft status

---

### 2. Get All Campaigns (Get All Campaigns)

**Nghiệp vụ thực tế:**
- Xem tất cả campaigns
- Filter theo status (Draft/InProgress/Completed)
- Filter theo type (Email/SMS/Event)

**Ví dụ thực tế:**
> Marketing Manager xem campaigns:
> - Q1 Product Launch (Email, In Progress) - 100 leads, $65K revenue
> - Winter Webinar Series (Webinar, Completed) - 50 leads, $30K revenue
> - Valentine Sale (SMS, Scheduled) - Start Feb 10
> - Trade Show Q2 (Event, Draft) - Planning phase
> → Total: 4 campaigns

---

### 3. Get Active Campaigns (Get Active Campaigns)

**Nghiệp vụ thực tế:**
- Xem campaigns đang chạy
- Monitor current campaigns
- Quick overview of ongoing marketing

**Ví dụ thực tế:**
> Check active campaigns hôm nay:
> - Q1 Product Launch (Day 5/30) - 100 leads so far
> - LinkedIn Ads Campaign (Day 12/60) - 35 leads
> → 2 active campaigns running

---

### 4. Xem chi tiết Campaign (Get Campaign by ID)

**Nghiệp vụ thực tế:**
- Deep dive vào một campaign
- Xem full metrics, spend, ROI
- Analyze performance

**Ví dụ thực tế:**
> Click "Q1 Product Launch" campaign:
> - Name: "Q1 Product Launch - SaaS CRM"
> - Type: Email, Status: In Progress
> - Started: Jan 15, Ends: Feb 15
> - Budget: $10,000, Spent: $8,500
> - Sent: 1,000 | Delivered: 980 | Opened: 245 (25%)
> - Clicked: 49 | Leads: 100 | Conversions: 10
> - Expected Revenue: $50K | Actual: $65K
> - ROI: 665%
> → Campaign exceeding expectations!

---

### 5. Cập nhật Campaign (Update Campaign)

**Nghiệp vụ thực tế:**
- Adjust budget
- Extend end date
- Update target audience

**Ví dụ thực tế:**
> Campaign performing well, extend:
> - Current EndDate: Feb 15
> - Update EndDate: Mar 15 (extend 1 month)
> - Increase Budget: $10K → $15K
> - Update Target: Add "Enterprise companies"
> → Campaign extended to capture more leads

---

### 6. Activate Campaign (Activate Campaign)

**Nghiệp vụ thực tế:**
- Start campaign
- Draft → InProgress
- Begin sending emails/running ads

**Ví dụ thực tế:**
> Campaign ready to launch:
> - Status: Draft
> - All setup done: email templates, audience list
> - Click "Activate"
> - Status: Draft → InProgress
> - System starts sending emails
> → Campaign now live!

---

### 7. Pause Campaign (Pause Campaign)

**Nghiệp vụ thực tế:**
- Temporarily stop campaign
- Review results, adjust strategy
- Resume later

**Ví dụ thực tế:**
> Campaign not performing well:
> - Open rate: 12% (expected 20%)
> - Click rate: 2% (expected 5%)
> - Click "Pause Campaign"
> - Status: InProgress → Paused
> - Review email subject lines, CTAs
> - Improve template
> - Resume next week
> → Campaign paused for optimization

---

### 8. Complete Campaign (Complete Campaign)

**Nghiệp vụ thực tế:**
- Manually end campaign
- Generate final report
- Analyze ROI

**Ví dụ thực tế:**
> Campaign reached goals early:
> - Target: 120 leads, Actual: 150 leads
> - Target: $50K revenue, Actual: $65K
> - Click "Complete Campaign"
> - Status: InProgress → Completed
> - Generate report:
>   * ROI: 665%
>   * Cost per lead: $85
>   * Conversion rate: 10%
> → Highly successful campaign!

---

### 9. Update Metrics (Update Campaign Metrics)

**Nghiệp vụ thực tế:**
- Update actual spend
- Track real-time performance
- Sync from email service provider

**Ví dụ thực tế:**
> Email service provider stats:
> - Sent: 1,000
> - Delivered: 980
> - Opened: 245
> - Clicked: 49
> - Bounced: 20
> - Call API to update metrics
> → CRM reflects latest campaign data

---

### 10. Get Performance (Get Campaign Performance)

**Nghiệp vụ thực tế:**
- Generate performance report
- Calculate ROI, rates
- Present to management

**Ví dụ thực tế:**
> Monthly marketing review:
> - Request: GET /campaigns/{id}/performance
> - Response:
>   * Budget: $10K, Spent: $8.5K (15% under)
>   * Revenue: $65K (30% over target)
>   * ROI: 665%
>   * Open Rate: 25% (good)
>   * Click Rate: 20% (excellent)
>   * Conversion Rate: 10% (industry avg: 5%)
> → Present to CEO: Campaign highly successful!

---

## Campaign Best Practices

### 1. Set Realistic Budgets

```
SMART Budget Planning:
──────────────────────
Specific:   Email campaign to 1,000 prospects
Measurable: Track open, click, conversion rates
Achievable: Industry avg: 20% open, 5% click, 2% conversion
Realistic:  Budget: $5K for design, copy, email service
Time-bound: 30-day campaign

Expected:
  - 20 conversions at $5K each = $100K revenue
  - Cost per conversion: $250
  - ROI: 1900%
```

### 2. Track Everything

Essential metrics to track:
- Financial: Budget, actual cost, revenue, ROI
- Engagement: Open, click, response rates
- Conversion: Leads generated, deals closed
- Timing: Best send times, day-of-week analysis

### 3. A/B Testing

```
Example: Email Subject Line Test
─────────────────────────────────
Group A (500 recipients):
  Subject: "New Product Launch - 20% Off"
  Open Rate: 18%

Group B (500 recipients):
  Subject: "You're Invited: Exclusive Product Preview"
  Open Rate: 28%

Winner: Group B → Use for remaining audience
```

### 4. Segmentation

```
Target Audience Segmentation:
─────────────────────────────
Campaign: "Enterprise CRM Launch"

Segment 1: Small Business (1-50 employees)
  - Budget: $2K-5K
  - Message: "Affordable CRM for growing teams"
  - Offer: 30% discount

Segment 2: Mid-Market (50-500 employees)
  - Budget: $10K-30K
  - Message: "Scale your sales operations"
  - Offer: Free consultation

Segment 3: Enterprise (500+ employees)
  - Budget: $100K+
  - Message: "Enterprise-grade CRM solution"
  - Offer: Custom pricing, dedicated support
```

---

## Integration with Other Modules

```
                        ┌──────────┐
                        │ CAMPAIGN │
                        └────┬─────┘
                             │
      ┌──────────────────────┼──────────────────────┐
      │          │           │           │          │
      ▼          ▼           ▼           ▼          ▼
  ┌──────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌──────────────┐
  │ LEAD │  │CONTACT │  │  USER  │  │TEMPLATE│  │OPPORTUNITY   │
  │      │  │        │  │        │  │        │  │              │
  │Track │  │Campaign│  │ Owner  │  │ Email  │  │Track campaign│
  │source│  │members │  │        │  │content │  │attribution   │
  └──────┘  └────────┘  └────────┘  └────────┘  └──────────────┘
```

| Module | Relationship | Use Case |
|--------|--------------|----------|
| **Leads** | Campaign generates Leads | Track lead source from campaign |
| **Contacts** | Campaign targets Contacts | Send emails to contact list |
| **Opportunities** | Campaign creates Opportunities | Attribute deals to campaigns |
| **Templates** | Campaign uses Templates | Email/SMS content |
| **Users** | Campaign has Owner | Marketing manager responsible |

---

## Technical Overview

**Base URL:** `/api/v1/campaigns`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Campaigns

Lấy danh sách campaigns với filter và pagination.

```
GET /api/v1/campaigns
```

**Permission Required:** `campaign.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Items mỗi trang |
| `sortBy` | string | No | "CreatedAt" | Field sắp xếp |
| `sortDescending` | bool | No | false | Giảm dần |
| `search` | string | No | - | Tìm theo name |
| `status` | CampaignStatus | No | - | Filter theo status |
| `type` | CampaignType | No | - | Filter theo type |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Q1 Product Launch",
        "type": "Email",
        "status": "InProgress",
        "startDate": "2026-01-15T00:00:00Z",
        "endDate": "2026-02-15T00:00:00Z",
        "budget": 10000,
        "actualCost": 8500,
        "expectedRevenue": 50000,
        "actualRevenue": 65000,
        "totalLeadsGenerated": 100,
        "totalConversions": 10,
        "totalSent": 1000,
        "createdAt": "2026-01-10T10:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3,
    "totalCount": 25
  }
}
```

---

### 2. Get Active Campaigns

Lấy campaigns đang active (InProgress, trong khoảng StartDate-EndDate).

```
GET /api/v1/campaigns/active
```

**Permission Required:** `campaign.view`

---

### 3. Get Campaign by ID

Lấy chi tiết một campaign.

```
GET /api/v1/campaigns/{id}
```

**Permission Required:** `campaign.view`

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Q1 Product Launch",
    "description": "Email campaign for new product launch targeting B2B buyers",
    "type": "Email",
    "status": "InProgress",
    "startDate": "2026-01-15T00:00:00Z",
    "endDate": "2026-02-15T00:00:00Z",
    "budget": 10000,
    "actualCost": 8500,
    "currency": "USD",
    "expectedRevenue": 50000,
    "actualRevenue": 65000,
    "expectedLeads": 120,
    "expectedConversions": 12,
    "totalSent": 1000,
    "totalDelivered": 980,
    "totalOpened": 245,
    "totalClicked": 49,
    "totalBounced": 20,
    "totalUnsubscribed": 5,
    "totalLeadsGenerated": 100,
    "totalConversions": 10,
    "ownerName": "Marketing Manager",
    "targetAudience": "B2B Software Buyers, 100-500 employees",
    "tags": "[\"product-launch\", \"b2b\", \"saas\"]",
    "createdAt": "2026-01-10T10:00:00Z"
  }
}
```

---

### 4. Create Campaign

Tạo campaign mới.

```
POST /api/v1/campaigns
```

**Permission Required:** `campaign.create`

#### Request Body

```json
{
  "name": "Q2 Webinar Series",
  "description": "Educational webinars for prospects",
  "type": 4,
  "startDate": "2026-04-01T00:00:00Z",
  "endDate": "2026-06-30T00:00:00Z",
  "budget": 5000,
  "currency": "USD",
  "expectedRevenue": 30000,
  "expectedLeads": 80,
  "expectedConversions": 8,
  "targetAudience": "Tech professionals, decision makers",
  "tags": "[\"webinar\", \"education\", \"b2b\"]",
  "ownerId": "user-guid"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Campaign name (max 200) |
| `description` | string | No | Description (max 500) |
| `type` | CampaignType | **Yes** | 0=Email, 1=SMS, 2=Social, 3=Event, 4=Webinar, 5=Advertisement |
| `startDate` | DateTime | No | Start date |
| `endDate` | DateTime | No | End date |
| `budget` | decimal | No | Budget amount |
| `currency` | string | No | Currency (default: USD) |
| `expectedRevenue` | decimal | No | Expected revenue |
| `expectedLeads` | int | No | Target leads |
| `expectedConversions` | int | No | Target conversions |
| `targetAudience` | string | No | Audience description |
| `tags` | string | No | Tags (JSON array) |
| `ownerId` | Guid | No | Owner (default: current user) |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-campaign-guid",
    "name": "Q2 Webinar Series",
    "type": "Webinar",
    "status": "Draft",
    "startDate": "2026-04-01T00:00:00Z",
    "endDate": "2026-06-30T00:00:00Z",
    "budget": 5000,
    "createdAt": "2026-01-18T10:00:00Z"
  }
}
```

---

### 5. Update Campaign

Cập nhật thông tin campaign.

```
PUT /api/v1/campaigns/{id}
```

**Permission Required:** `campaign.update`

#### Request Body (All fields optional)

```json
{
  "name": "Updated Campaign Name",
  "description": "Updated description",
  "endDate": "2026-03-15T00:00:00Z",
  "budget": 15000,
  "expectedRevenue": 75000
}
```

---

### 6. Activate Campaign

Kích hoạt campaign (Draft → InProgress).

```
POST /api/v1/campaigns/{id}/activate
```

**Permission Required:** `campaign.update`

---

### 7. Pause Campaign

Tạm dừng campaign (InProgress → Paused).

```
POST /api/v1/campaigns/{id}/pause
```

**Permission Required:** `campaign.update`

---

### 8. Complete Campaign

Kết thúc campaign (InProgress → Completed).

```
POST /api/v1/campaigns/{id}/complete
```

**Permission Required:** `campaign.update`

---

### 9. Update Metrics

Cập nhật campaign metrics (từ email service, analytics).

```
POST /api/v1/campaigns/{id}/update-metrics
```

**Permission Required:** `campaign.update`

#### Request Body (All fields optional)

```json
{
  "actualCost": 8500,
  "actualRevenue": 65000,
  "totalSent": 1000,
  "totalDelivered": 980,
  "totalOpened": 245,
  "totalClicked": 49,
  "totalBounced": 20,
  "totalUnsubscribed": 5,
  "totalLeadsGenerated": 100,
  "totalConversions": 10
}
```

---

### 10. Get Performance

Lấy campaign performance report với calculated metrics.

```
GET /api/v1/campaigns/{id}/performance
```

**Permission Required:** `campaign.view`

#### Response

```json
{
  "success": true,
  "data": {
    "campaignId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "campaignName": "Q1 Product Launch",
    "status": "InProgress",
    "budget": 10000,
    "actualCost": 8500,
    "expectedRevenue": 50000,
    "actualRevenue": 65000,
    "roi": 665.0,
    "totalSent": 1000,
    "totalDelivered": 980,
    "totalOpened": 245,
    "totalClicked": 49,
    "totalLeadsGenerated": 100,
    "totalConversions": 10,
    "openRate": 25.0,
    "clickRate": 20.0,
    "conversionRate": 10.0
  }
}
```

**Calculated Metrics:**
- `roi`: ((ActualRevenue - ActualCost) / ActualCost) * 100
- `openRate`: (TotalOpened / TotalDelivered) * 100
- `clickRate`: (TotalClicked / TotalOpened) * 100
- `conversionRate`: (TotalConversions / TotalLeadsGenerated) * 100

---

### 11. Delete Campaign

Xóa campaign (soft delete).

```
DELETE /api/v1/campaigns/{id}
```

**Permission Required:** `campaign.delete`

---

## Enums

### CampaignType

| Value | Name | Description |
|-------|------|-------------|
| 0 | Email | Email marketing |
| 1 | SMS | Text messages |
| 2 | Social | Social media ads |
| 3 | Event | Physical/virtual events |
| 4 | Webinar | Online seminars |
| 5 | Advertisement | Paid advertising |
| 6 | Other | Custom |

### CampaignStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Draft | Planning |
| 1 | Scheduled | Scheduled for future |
| 2 | InProgress | Active |
| 3 | Paused | Temporarily stopped |
| 4 | Completed | Finished |
| 5 | Cancelled | Terminated |

### CampaignMemberStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Pending | Not sent yet |
| 1 | Sent | Email/SMS sent |
| 2 | Opened | Email opened |
| 3 | Clicked | Link clicked |
| 4 | Responded | Replied/form filled |
| 5 | Converted | Became customer |
| 6 | Unsubscribed | Opted out |
| 7 | Bounced | Failed to deliver |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `campaign.view` | Xem campaigns |
| `campaign.create` | Tạo campaign |
| `campaign.update` | Cập nhật, activate, pause, complete |
| `campaign.delete` | Xóa campaign |

---

## Example: Email Campaign Workflow

### Step 1: Create Email Campaign

```bash
curl -X POST "http://localhost:5000/api/v1/campaigns" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Q1 Product Launch",
    "description": "Email campaign for new CRM features",
    "type": 0,
    "startDate": "2026-01-15T00:00:00Z",
    "endDate": "2026-02-15T00:00:00Z",
    "budget": 10000,
    "expectedRevenue": 50000,
    "expectedLeads": 120,
    "expectedConversions": 12,
    "targetAudience": "B2B Software Buyers"
  }'
```

### Step 2: Activate Campaign

```bash
curl -X POST "http://localhost:5000/api/v1/campaigns/{id}/activate" \
  -H "Authorization: Bearer {token}"
```

### Step 3: Update Metrics (from Email Service)

```bash
curl -X POST "http://localhost:5000/api/v1/campaigns/{id}/update-metrics" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "totalSent": 1000,
    "totalDelivered": 980,
    "totalOpened": 245,
    "totalClicked": 49,
    "totalLeadsGenerated": 100
  }'
```

### Step 4: Get Performance Report

```bash
curl -X GET "http://localhost:5000/api/v1/campaigns/{id}/performance" \
  -H "Authorization: Bearer {token}"
```

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
