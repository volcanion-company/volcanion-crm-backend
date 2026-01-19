# Pipelines API Documentation

## Tổng quan Module

### Pipeline là gì?

**Pipeline** là một quy trình bán hàng có cấu trúc, gồm nhiều giai đoạn (stages) tuần tự mà một opportunity phải trải qua từ khi bắt đầu đến khi close deal (won hoặc lost). Pipeline giúp standardize sales process và track tiến độ deal một cách trực quan.

### Tại sao Pipeline quan trọng?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  PIPELINE = SALES PROCESS VISUALIZATION                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Without Pipeline:                With Pipeline:                            │
│  ─────────────────                ──────────────                            │
│  ❌ Deal ở đâu rồi?              ✅ Pipeline view: Deal at "Proposal" stage│
│  ❌ Bao giờ close?                ✅ 70% probability → Close in 2 weeks     │
│  ❌ Process không consistent      ✅ Standardized: 5-stage process for all  │
│  ❌ Forecast không chính xác      ✅ Weighted forecast: $500K * 70% = $350K │
│  ❌ Deals rơi rớt ở đâu?          ✅ Analytics: 40% drop at "Demo" stage    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Pipeline vs. Stage Relationship

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PIPELINE STRUCTURE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  PIPELINE: "Standard Sales Pipeline"                                       │
│  ═══════════════════════════════════════════════════════════════════════    │
│                                                                             │
│  Stage 1: Qualification                                                     │
│  ┌────────────────────────────────────┐                                    │
│  │ Probability: 10%                   │                                    │
│  │ Color: 🔵 Blue                     │                                    │
│  │ Opportunities: 15 deals            │                                    │
│  │ Total Value: $750K                 │                                    │
│  └────────────────────────────────────┘                                    │
│           │                                                                 │
│           ▼                                                                 │
│  Stage 2: Needs Analysis                                                    │
│  ┌────────────────────────────────────┐                                    │
│  │ Probability: 30%                   │                                    │
│  │ Color: 🟢 Green                    │                                    │
│  │ Opportunities: 10 deals            │                                    │
│  │ Total Value: $600K                 │                                    │
│  └────────────────────────────────────┘                                    │
│           │                                                                 │
│           ▼                                                                 │
│  Stage 3: Proposal                                                          │
│  ┌────────────────────────────────────┐                                    │
│  │ Probability: 60%                   │                                    │
│  │ Color: 🟡 Yellow                   │                                    │
│  │ Opportunities: 8 deals             │                                    │
│  │ Total Value: $500K                 │                                    │
│  └────────────────────────────────────┘                                    │
│           │                                                                 │
│           ▼                                                                 │
│  Stage 4: Negotiation                                                       │
│  ┌────────────────────────────────────┐                                    │
│  │ Probability: 80%                   │                                    │
│  │ Color: 🟠 Orange                   │                                    │
│  │ Opportunities: 5 deals             │                                    │
│  │ Total Value: $350K                 │                                    │
│  └────────────────────────────────────┘                                    │
│           │                                                                 │
│           ▼                                                                 │
│  Stage 5: Closed Won                                                        │
│  ┌────────────────────────────────────┐                                    │
│  │ Probability: 100%                  │                                    │
│  │ Color: 🟢 Green (Dark)             │                                    │
│  │ IsWon: true                        │                                    │
│  │ Opportunities: 20 deals (this qtr) │                                    │
│  │ Total Value: $1.2M                 │                                    │
│  └────────────────────────────────────┘                                    │
│                                                                             │
│  Stage 6: Closed Lost                                                       │
│  ┌────────────────────────────────────┐                                    │
│  │ Probability: 0%                    │                                    │
│  │ Color: 🔴 Red                      │                                    │
│  │ IsLost: true                       │                                    │
│  │ Opportunities: 12 deals (lost)     │                                    │
│  └────────────────────────────────────┘                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Pipeline Types & Use Cases

### Multiple Pipelines for Different Processes

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              WHY MULTIPLE PIPELINES? DIFFERENT SALES PROCESSES              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. STANDARD SALES PIPELINE (Default)                                      │
│     Use case: Regular B2B sales                                            │
│     Stages: Qualification → Needs Analysis → Proposal → Negotiation → Won  │
│     Avg cycle: 60 days                                                      │
│                                                                             │
│  2. ENTERPRISE SALES PIPELINE                                               │
│     Use case: Large corporate deals ($100K+)                                │
│     Stages: Initial Contact → Discovery → RFP → Evaluation → POC →         │
│             Executive Review → Legal Review → Closed Won                    │
│     Avg cycle: 180 days                                                     │
│                                                                             │
│  3. CHANNEL PARTNER PIPELINE                                                │
│     Use case: Reseller/Partner deals                                        │
│     Stages: Partner Referral → Qualification → Demo → Partner Quote →      │
│             Closed Won                                                      │
│     Avg cycle: 30 days                                                      │
│                                                                             │
│  4. RENEWAL PIPELINE                                                        │
│     Use case: Existing customer renewals                                    │
│     Stages: Renewal Notice → Customer Outreach → Negotiation → Renewed     │
│     Avg cycle: 14 days                                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Stage Probability & Forecasting

### How Probability Works

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      PROBABILITY-BASED FORECASTING                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Stage              Probability    Deals    Value       Weighted Forecast  │
│  ─────────────────  ───────────    ─────    ─────────   ─────────────────  │
│  Qualification      10%            15       $750,000    $75,000            │
│  Needs Analysis     30%            10       $600,000    $180,000           │
│  Proposal           60%            8        $500,000    $300,000           │
│  Negotiation        80%            5        $350,000    $280,000           │
│  Closed Won         100%           20       $1,200,000  $1,200,000         │
│                                                                             │
│  ═══════════════════════════════════════════════════════════════════════    │
│  TOTAL PIPELINE VALUE:                       $3,400,000                    │
│  WEIGHTED FORECAST:                          $2,035,000                    │
│  WIN RATE (based on historical):             34.5%                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Best Practices for Probability

| Stage | Typical Probability | Criteria |
|-------|---------------------|----------|
| **Qualification** | 10% | Lead qualified, budget confirmed |
| **Needs Analysis** | 30% | Requirements gathered, stakeholders identified |
| **Proposal** | 60% | Proposal sent, positive feedback |
| **Negotiation** | 80% | Terms discussed, verbal commitment |
| **Closed Won** | 100% | Contract signed |
| **Closed Lost** | 0% | Deal lost |

---

## Stage Colors & Visual Management

### Color Coding Strategy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          COLOR-CODED PIPELINE                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Kanban Board View:                                                         │
│                                                                             │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐               │
│  │🔵 Qualify │  │🟢 Analysis│  │🟡 Proposal│  │🟠 Negotia.│               │
│  │           │  │           │  │           │  │           │               │
│  │ ABC Corp  │  │ XYZ Inc   │  │ DEF Ltd   │  │ GHI Corp  │               │
│  │ $50K      │  │ $80K      │  │ $120K     │  │ $200K     │               │
│  │           │  │           │  │           │  │           │               │
│  │ MNO Ltd   │  │ PQR Corp  │  │ STU Inc   │  │ VWX Ltd   │               │
│  │ $30K      │  │ $45K      │  │ $90K      │  │ $150K     │               │
│  │           │  │           │  │           │  │           │               │
│  └───────────┘  └───────────┘  └───────────┘  └───────────┘               │
│                                                                             │
│  ┌───────────┐  ┌───────────┐                                              │
│  │✅ WON     │  │❌ LOST    │                                              │
│  │           │  │           │                                              │
│  │ YZA Corp  │  │ BCD Inc   │                                              │
│  │ $300K     │  │ $60K      │                                              │
│  │ Closed    │  │ Pricing   │                                              │
│  │ today     │  │           │                                              │
│  └───────────┘  └───────────┘                                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Recommended Colors

| Color | Hex Code | Use Case |
|-------|----------|----------|
| 🔵 Blue | #3B82F6 | Early stages (Qualification) |
| 🟢 Green | #10B981 | Mid stages (Needs Analysis) |
| 🟡 Yellow | #F59E0B | Advanced stages (Proposal) |
| 🟠 Orange | #F97316 | Final stages (Negotiation) |
| ✅ Dark Green | #059669 | Closed Won |
| 🔴 Red | #EF4444 | Closed Lost |

---

## IsWon & IsLost Flags

### Terminal Stages

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     TERMINAL STAGES BEHAVIOR                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ STAGE: Closed Won                                                  │    │
│  │ ─────────────────                                                  │    │
│  │ IsWon: true ✓                                                      │    │
│  │ Probability: 100%                                                  │    │
│  │                                                                    │    │
│  │ When opportunity moves to this stage:                             │    │
│  │   ✓ Status automatically set to "Won"                             │    │
│  │   ✓ CloseDate set to today                                        │    │
│  │   ✓ Cannot move to other stages (final)                           │    │
│  │   ✓ Triggers: Create contract, send thank you, onboard customer   │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ STAGE: Closed Lost                                                 │    │
│  │ ─────────────────                                                  │    │
│  │ IsLost: true ✓                                                     │    │
│  │ Probability: 0%                                                    │    │
│  │                                                                    │    │
│  │ When opportunity moves to this stage:                             │    │
│  │   ✓ Status automatically set to "Lost"                            │    │
│  │   ✓ Requires LostReason (competitor, pricing, timing, etc.)       │    │
│  │   ✓ CloseDate set to today                                        │    │
│  │   ✓ Cannot move to other stages (final)                           │    │
│  │   ✓ Triggers: Send survey, add to nurture campaign                │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Default Pipeline

### Why Default Pipeline?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DEFAULT PIPELINE CONCEPT                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Default Pipeline = Pipeline được chọn tự động khi:                         │
│  ──────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  1. Create new opportunity without specifying pipeline                     │
│     → System assigns default pipeline                                      │
│                                                                             │
│  2. Convert Lead to Opportunity                                            │
│     → Opportunity created with default pipeline                            │
│                                                                             │
│  3. Import opportunities from external source                              │
│     → Default pipeline used if none specified                              │
│                                                                             │
│  ═══════════════════════════════════════════════════════════════════════    │
│                                                                             │
│  Example:                                                                   │
│  ─────────                                                                  │
│  Your CRM has 3 pipelines:                                                  │
│    • Standard Sales Pipeline (Default) ✓                                   │
│    • Enterprise Sales Pipeline                                             │
│    • Partner Channel Pipeline                                              │
│                                                                             │
│  Sales Rep creates opportunity:                                            │
│    → No pipeline selected                                                  │
│    → System uses "Standard Sales Pipeline"                                 │
│    → Opportunity starts at first stage "Qualification"                     │
│                                                                             │
│  Only ONE pipeline can be default at a time                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Pipeline (Create Pipeline)

**Nghiệp vụ thực tế:**
- Sales Manager define sales process
- Customize cho từng loại deal
- Standardize workflow for team

**Ví dụ thực tế:**
> Company bán SaaS, muốn tạo pipeline cho Enterprise deals:
> - Tạo pipeline: "Enterprise Sales"
> - Add 7 stages:
>   1. Initial Contact (5%)
>   2. Discovery Call (15%)
>   3. RFP Submission (30%)
>   4. Product Evaluation (50%)
>   5. Proof of Concept (70%)
>   6. Executive Sign-off (90%)
>   7. Closed Won (100%)
> - Set IsDefault: false (special pipeline)
> → Enterprise deals now follow standardized 7-stage process

---

### 2. Lấy All Pipelines (Get All Pipelines)

**Nghiệp vụ thực tế:**
- View tất cả pipelines
- Select pipeline khi tạo opportunity
- Pipeline management overview

**Ví dụ thực tế:**
> Sales Manager xem pipelines:
> - Pipeline list:
>   * Standard Sales (Default) - 38 active opportunities
>   * Enterprise Sales - 12 active opportunities
>   * Partner Channel - 7 active opportunities
>   * Renewal Pipeline - 25 active renewals
> → Total: 82 opportunities across 4 pipelines

---

### 3. Xem chi tiết Pipeline (Get Pipeline by ID)

**Nghiệp vụ thực tế:**
- Xem stages trong pipeline
- Check probability settings
- Analyze pipeline structure

**Ví dụ thực tế:**
> Click "Standard Sales Pipeline":
> - Name: "Standard Sales Pipeline"
> - IsDefault: true
> - Stages (5):
>   1. Qualification (10%, Blue) - 15 deals
>   2. Needs Analysis (30%, Green) - 10 deals
>   3. Proposal (60%, Yellow) - 8 deals
>   4. Negotiation (80%, Orange) - 5 deals
>   5. Closed Won (100%, Dark Green) - 20 deals this quarter
> → Visualize full pipeline with deals at each stage

---

### 4. Cập nhật Pipeline (Update Pipeline)

**Nghiệp vụ thực tế:**
- Đổi tên pipeline
- Set/unset default
- Update description

**Ví dụ thực tế:**
> Rename pipeline:
> - Old name: "New Business"
> - New name: "Standard Sales Pipeline"
> - Set IsDefault: true
> - Remove default from other pipelines automatically
> → Pipeline renamed and set as default

---

### 5. Thêm Stage vào Pipeline (Add Stage)

**Nghiệp vụ thực tế:**
- Expand sales process
- Add new step to workflow
- Improve tracking granularity

**Ví dụ thực tế:**
> Pipeline có 4 stages, muốn add "Demo" stage:
> - Current stages:
>   1. Qualification
>   2. Proposal
>   3. Negotiation
>   4. Closed Won
> - Add stage: "Demo"
> - SortOrder: 2 (between Qualification and Proposal)
> - Probability: 40%
> - Color: #10B981 (Green)
> - Result:
>   1. Qualification
>   2. Demo (NEW)
>   3. Proposal
>   4. Negotiation
>   5. Closed Won
> → Now track demos separately

---

### 6. Cập nhật Stage (Update Stage)

**Nghiệp vụ thực tế:**
- Adjust probability based on data
- Reorder stages
- Change colors for visibility

**Ví dụ thực tế:**
> Stage "Proposal" win rate low:
> - Current probability: 60%
> - Historical data: Only 45% of proposals close
> - Update: Probability 60% → 50%
> - Also update color: Yellow → Light Orange
> → More accurate forecasting

---

### 7. Xóa Stage (Delete Stage)

**Nghiệp vụ thực tế:**
- Remove unnecessary stage
- Simplify pipeline
- Cannot delete if opportunities exist

**Ví dụ thực tế:**
> Stage "Pre-qualification" không dùng:
> - Check: 0 opportunities in this stage
> - Delete stage
> - Result: Pipeline now has 4 stages instead of 5
> - ⚠️ If stage had opportunities: Error "Cannot delete stage with existing opportunities"

---

### 8. Xóa Pipeline (Delete Pipeline)

**Nghiệp vụ thực tế:**
- Retire old pipeline
- Soft delete (not hard delete)
- Cannot delete if opportunities exist

**Ví dụ thực tế:**
> Old pipeline "Legacy Sales Process":
> - Check stages: All have 0 opportunities
> - IsDefault: false
> - Delete pipeline
> - Result: IsActive = false, IsDeleted = true
> - Pipeline hidden from UI but data preserved
> - ⚠️ Cannot delete default pipeline
> - ⚠️ Cannot delete if opportunities exist in any stage

---

## Pipeline Design Best Practices

### 1. Keep it Simple (5-7 stages ideal)

```
❌ Too Many Stages (12 stages):
   Initial Contact → Cold Call → Follow-up → Qualification → Budget Check → 
   Needs Analysis → Demo → Proposal → Review → Negotiation → Legal → Closed Won

✅ Optimal (5 stages):
   Qualification → Needs Analysis → Proposal → Negotiation → Closed Won
```

### 2. Clear Stage Criteria

Each stage should have clear entry/exit criteria:

| Stage | Entry Criteria | Exit Criteria |
|-------|---------------|---------------|
| **Qualification** | Lead contacted, interest confirmed | Budget & timeline confirmed |
| **Needs Analysis** | Discovery call scheduled | Requirements documented |
| **Proposal** | Proposal requested | Proposal sent & reviewed |
| **Negotiation** | Feedback received | Terms agreed |
| **Closed Won** | Contract ready | Contract signed |

### 3. Probability Calibration

Review and adjust probabilities quarterly based on actual win rates:

```
Stage: Proposal
─────────────────
Historical data (last quarter):
  - 50 deals entered this stage
  - 28 deals won
  - Win rate: 56%

Current setting: Probability 60%
Adjustment: 60% → 56% (more accurate)
```

### 4. Terminal Stages

Always have two terminal stages:
- **Closed Won** (IsWon = true, Probability = 100%)
- **Closed Lost** (IsLost = true, Probability = 0%)

---

## Tích hợp với Opportunities

```
                        ┌──────────┐
                        │ PIPELINE │
                        └────┬─────┘
                             │
                             │ has many
                             │
                             ▼
                        ┌──────────┐
                        │  STAGE   │
                        └────┬─────┘
                             │
                             │ has many
                             │
                             ▼
                     ┌───────────────┐
                     │ OPPORTUNITY   │
                     └───────────────┘
```

### Opportunity Movement through Pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    OPPORTUNITY PROGRESSION                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Opportunity: "ABC Corp - CRM Deal"                                         │
│  Value: $150,000                                                            │
│  Pipeline: Standard Sales                                                   │
│                                                                             │
│  Timeline:                                                                  │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Jan 1:  Created → Stage: Qualification (10%)                               │
│           Forecast: $150K * 10% = $15K                                      │
│                                                                             │
│  Jan 5:  Moved → Stage: Needs Analysis (30%)                                │
│           Forecast: $150K * 30% = $45K                                      │
│           Activity: Discovery call completed                                │
│                                                                             │
│  Jan 12: Moved → Stage: Proposal (60%)                                      │
│           Forecast: $150K * 60% = $90K                                      │
│           Activity: Proposal sent                                           │
│                                                                             │
│  Jan 20: Moved → Stage: Negotiation (80%)                                   │
│           Forecast: $150K * 80% = $120K                                     │
│           Activity: Terms discussed                                         │
│                                                                             │
│  Jan 28: Moved → Stage: Closed Won (100%)                                   │
│           Forecast: $150K * 100% = $150K                                    │
│           Status: Won, CloseDate: Jan 28                                    │
│           Trigger: Create contract, send welcome email                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Overview

**Base URL:** `/api/v1/pipelines`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Pipelines

Lấy danh sách tất cả active pipelines với stages.

```
GET /api/v1/pipelines
```

**Permission Required:** `pipeline.view`

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Standard Sales Pipeline",
      "description": "Default B2B sales process",
      "isDefault": true,
      "isActive": true,
      "stages": [
        {
          "id": "stage-guid-1",
          "name": "Qualification",
          "sortOrder": 0,
          "probability": 10,
          "color": "#3B82F6",
          "isWon": false,
          "isLost": false
        },
        {
          "id": "stage-guid-2",
          "name": "Needs Analysis",
          "sortOrder": 1,
          "probability": 30,
          "color": "#10B981",
          "isWon": false,
          "isLost": false
        },
        {
          "id": "stage-guid-3",
          "name": "Proposal",
          "sortOrder": 2,
          "probability": 60,
          "color": "#F59E0B",
          "isWon": false,
          "isLost": false
        },
        {
          "id": "stage-guid-4",
          "name": "Negotiation",
          "sortOrder": 3,
          "probability": 80,
          "color": "#F97316",
          "isWon": false,
          "isLost": false
        },
        {
          "id": "stage-guid-5",
          "name": "Closed Won",
          "sortOrder": 4,
          "probability": 100,
          "color": "#059669",
          "isWon": true,
          "isLost": false
        },
        {
          "id": "stage-guid-6",
          "name": "Closed Lost",
          "sortOrder": 5,
          "probability": 0,
          "color": "#EF4444",
          "isWon": false,
          "isLost": true
        }
      ]
    }
  ]
}
```

---

### 2. Get Pipeline by ID

Lấy chi tiết một pipeline.

```
GET /api/v1/pipelines/{id}
```

**Permission Required:** `pipeline.view`

---

### 3. Create Pipeline

Tạo pipeline mới với stages.

```
POST /api/v1/pipelines
```

**Permission Required:** `pipeline.create`

#### Request Body

```json
{
  "name": "Enterprise Sales Pipeline",
  "description": "For large corporate deals over $100K",
  "isDefault": false,
  "stages": [
    {
      "name": "Initial Contact",
      "probability": 5,
      "color": "#3B82F6",
      "isWon": false,
      "isLost": false
    },
    {
      "name": "Discovery",
      "probability": 15,
      "color": "#10B981",
      "isWon": false,
      "isLost": false
    },
    {
      "name": "RFP Submission",
      "probability": 30,
      "color": "#8B5CF6",
      "isWon": false,
      "isLost": false
    },
    {
      "name": "Evaluation",
      "probability": 50,
      "color": "#F59E0B",
      "isWon": false,
      "isLost": false
    },
    {
      "name": "POC",
      "probability": 70,
      "color": "#F97316",
      "isWon": false,
      "isLost": false
    },
    {
      "name": "Executive Review",
      "probability": 85,
      "color": "#EC4899",
      "isWon": false,
      "isLost": false
    },
    {
      "name": "Closed Won",
      "probability": 100,
      "color": "#059669",
      "isWon": true,
      "isLost": false
    },
    {
      "name": "Closed Lost",
      "probability": 0,
      "color": "#EF4444",
      "isWon": false,
      "isLost": true
    }
  ]
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Tên pipeline (max 100 chars) |
| `description` | string | No | Mô tả pipeline (max 500 chars) |
| `isDefault` | bool | No | Set làm default pipeline |
| `stages` | array | **Yes** | Danh sách stages |

#### Stage Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Tên stage |
| `probability` | int | **Yes** | 0-100% |
| `color` | string | No | Hex color code |
| `isWon` | bool | No | Terminal stage (won) |
| `isLost` | bool | No | Terminal stage (lost) |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-pipeline-guid",
    "name": "Enterprise Sales Pipeline",
    "description": "For large corporate deals over $100K",
    "isDefault": false,
    "isActive": true,
    "stages": [...]
  }
}
```

---

### 4. Update Pipeline

Cập nhật thông tin pipeline.

```
PUT /api/v1/pipelines/{id}
```

**Permission Required:** `pipeline.update`

#### Request Body (All fields optional)

```json
{
  "name": "Updated Pipeline Name",
  "description": "Updated description",
  "isDefault": true
}
```

**Note:** Setting `isDefault: true` automatically removes default from other pipelines.

---

### 5. Add Stage to Pipeline

Thêm stage mới vào pipeline.

```
POST /api/v1/pipelines/{pipelineId}/stages
```

**Permission Required:** `pipeline.update`

#### Request Body

```json
{
  "name": "Demo Stage",
  "sortOrder": 2,
  "probability": 40,
  "color": "#10B981",
  "isWon": false,
  "isLost": false
}
```

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-stage-guid",
    "name": "Demo Stage",
    "sortOrder": 2,
    "probability": 40,
    "color": "#10B981",
    "isWon": false,
    "isLost": false
  }
}
```

---

### 6. Update Stage

Cập nhật thông tin stage.

```
PUT /api/v1/pipelines/stages/{stageId}
```

**Permission Required:** `pipeline.update`

#### Request Body (All fields optional)

```json
{
  "name": "Updated Stage Name",
  "sortOrder": 3,
  "probability": 55,
  "color": "#F59E0B"
}
```

---

### 7. Delete Stage

Xóa stage khỏi pipeline.

```
DELETE /api/v1/pipelines/stages/{stageId}
```

**Permission Required:** `pipeline.delete`

**Validation:**
- Cannot delete if stage has opportunities
- Returns 400 Bad Request if validation fails

---

### 8. Delete Pipeline

Xóa pipeline (soft delete).

```
DELETE /api/v1/pipelines/{id}
```

**Permission Required:** `pipeline.delete`

**Validation:**
- Cannot delete default pipeline
- Cannot delete if any stage has opportunities
- Soft delete: Sets `IsActive = false`, `IsDeleted = true`

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `pipeline.view` | Xem pipelines |
| `pipeline.create` | Tạo pipeline mới |
| `pipeline.update` | Cập nhật pipeline & stages |
| `pipeline.delete` | Xóa pipeline & stages |

---

## Error Codes

| Code | Message | Cause |
|------|---------|-------|
| 404 | Pipeline not found | Invalid pipeline ID |
| 404 | Stage not found | Invalid stage ID |
| 400 | Cannot delete default pipeline | Attempting to delete IsDefault=true |
| 400 | Cannot delete stage with existing opportunities | Stage has opportunities |
| 400 | Cannot delete pipeline with existing opportunities | Any stage has opportunities |

---

## Example: Create Standard Sales Pipeline

```bash
curl -X POST "http://localhost:5000/api/v1/pipelines" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Standard Sales Pipeline",
    "description": "Default B2B sales process",
    "isDefault": true,
    "stages": [
      {
        "name": "Qualification",
        "probability": 10,
        "color": "#3B82F6"
      },
      {
        "name": "Needs Analysis",
        "probability": 30,
        "color": "#10B981"
      },
      {
        "name": "Proposal",
        "probability": 60,
        "color": "#F59E0B"
      },
      {
        "name": "Negotiation",
        "probability": 80,
        "color": "#F97316"
      },
      {
        "name": "Closed Won",
        "probability": 100,
        "color": "#059669",
        "isWon": true
      },
      {
        "name": "Closed Lost",
        "probability": 0,
        "color": "#EF4444",
        "isLost": true
      }
    ]
  }'
```

---

## Integration Examples

### Create Opportunity with Pipeline

```bash
curl -X POST "http://localhost:5000/api/v1/opportunities" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "ABC Corp - CRM Deal",
    "customerId": "customer-guid",
    "pipelineId": "pipeline-guid",
    "stageId": "stage-qualification-guid",
    "amount": 150000,
    "expectedCloseDate": "2026-03-31"
  }'
```

### Move Opportunity to Next Stage

```bash
curl -X PUT "http://localhost:5000/api/v1/opportunities/{opportunityId}/stage" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "stageId": "stage-needs-analysis-guid",
    "reason": "Discovery call completed successfully"
  }'
```

---

## Analytics Use Cases

### 1. Pipeline Coverage Report

```
Q1 Target: $1,000,000
Current pipeline: $3,400,000 (weighted: $2,035,000)
Coverage ratio: 3.4x
Weighted coverage: 2.0x
Status: ✅ Healthy pipeline
```

### 2. Stage Conversion Rates

```
Stage Conversion Analysis (Last Quarter):
─────────────────────────────────────────
Qualification → Needs Analysis: 67%
Needs Analysis → Proposal: 80%
Proposal → Negotiation: 63% ⚠️ (Drop-off)
Negotiation → Closed Won: 85%

Overall Win Rate: 28%
Action: Improve proposal stage process
```

### 3. Stage Duration Analysis

```
Average Days in Each Stage:
───────────────────────────
Qualification: 7 days
Needs Analysis: 14 days
Proposal: 21 days ⚠️ (Too long)
Negotiation: 10 days
Total Avg Cycle: 52 days

Action: Streamline proposal creation
```

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
