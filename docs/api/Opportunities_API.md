# Opportunities API Documentation

## Tổng quan Module

### Opportunity là gì?

**Opportunity (Cơ hội bán hàng)** là một deal tiềm năng với giá trị cụ thể đang được theo dõi trong quy trình bán hàng. Opportunity đại diện cho một giao dịch có khả năng chuyển đổi thành doanh thu thực tế.

### Sự khác biệt giữa Lead và Opportunity

| Khái niệm | Lead | Opportunity |
|-----------|------|-------------|
| **Định nghĩa** | Người/tổ chức quan tâm | Deal cụ thể với giá trị |
| **Giá trị** | Ước tính, không chắc chắn | Cụ thể, đã thảo luận |
| **Xác suất** | Chưa xác định | % cụ thể (0-100%) |
| **Trong Pipeline** | Không | Có, theo dõi qua stages |
| **Thời gian** | Không deadline | Expected Close Date |

### Tại sao cần quản lý Opportunity?

| Vấn đề thực tế | Giải pháp của module Opportunity |
|----------------|----------------------------------|
| Không biết có bao nhiêu deal đang chạy | Pipeline view hiển thị tất cả opportunities |
| Không biết deal nào sẽ close sớm | Expected Close Date + Filter/Sort |
| Không dự báo được doanh thu | Weighted Pipeline = Amount × Probability |
| Không biết deal tắc ở đâu | Stage tracking trong pipeline |
| Không biết tại sao thua deal | Loss Reason analysis |

---

## Sales Pipeline Concept

### Pipeline là gì?

**Sales Pipeline** là quy trình bán hàng được chia thành các giai đoạn (stages). Mỗi opportunity di chuyển qua các stages từ đầu đến cuối.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         SALES PIPELINE                                       │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────┐  ┌─────────────┐  ┌────────────┐  ┌────────────┐  ┌────────┐ │
│  │PROSPECTING │─►│QUALIFICATION│─►│  PROPOSAL  │─►│NEGOTIATION │─►│ CLOSED │ │
│  │            │  │             │  │            │  │            │  │        │ │
│  │ 5 deals    │  │ 8 deals     │  │ 4 deals    │  │ 2 deals    │  │        │ │
│  │ $50,000    │  │ $120,000    │  │ $80,000    │  │ $60,000    │  │        │ │
│  │ 10%        │  │ 30%         │  │ 60%        │  │ 80%        │  │ 100%   │ │
│  └────────────┘  └─────────────┘  └────────────┘  └────────────┘  └────────┘ │
│                                                                     │        │
│                                                    ┌───────────┐    │        │
│                                                    │  WON      │◄───┘        │
│                                                    │ $50,000   │             │
│                                                    └───────────┘             │
│                                                    ┌───────────┐             │
│                                                    │  LOST     │             │
│                                                    │ $30,000   │             │
│                                                    └───────────┘             │
│                                                                              │
│  Pipeline Total: $310,000                                                    │
│  Weighted Value: $75,000 (tính theo probability)                             │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Các giai đoạn phổ biến

| Stage | Probability | Mô tả | Tiêu chí để vào stage |
|-------|-------------|-------|----------------------|
| **Prospecting** | 10% | Đang tìm hiểu, chưa có liên hệ sâu | Lead vừa convert thành opportunity |
| **Qualification** | 30% | Đã xác nhận nhu cầu và budget | Có cuộc gọi discovery, biết BANT |
| **Proposal** | 60% | Đã gửi proposal/báo giá | Proposal đã gửi, đang chờ feedback |
| **Negotiation** | 80% | Đang đàm phán điều khoản | Đang thương lượng giá, terms |
| **Closed Won** | 100% | Đã thắng deal | Hợp đồng ký, đơn hàng tạo |
| **Closed Lost** | 0% | Đã thua deal | Khách chọn đối thủ hoặc không mua |

---

## Weighted Pipeline & Forecasting

### Weighted Amount là gì?

**Weighted Amount (Giá trị có trọng số)** = Amount × Probability / 100

Đây là cách tính doanh thu dự kiến có tính đến xác suất thắng deal.

### Ví dụ tính toán

| Opportunity | Amount | Probability | Weighted Amount |
|-------------|--------|-------------|-----------------|
| Deal A | $100,000 | 80% | $80,000 |
| Deal B | $50,000 | 30% | $15,000 |
| Deal C | $200,000 | 10% | $20,000 |
| **Total** | **$350,000** | - | **$115,000** |

→ Pipeline total là $350K nhưng dự báo thực tế chỉ $115K

### Sales Forecasting

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SALES FORECAST Q1 2026                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Tháng         │ Pipeline │ Weighted │ Deals │ Avg Deal Size                │
│  ─────────────────────────────────────────────────────────────────────────  │
│  January 2026  │ $150,000 │  $85,000 │   8   │ $18,750                      │
│  February 2026 │ $200,000 │ $110,000 │  12   │ $16,667                      │
│  March 2026    │ $180,000 │  $95,000 │  10   │ $18,000                      │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Q1 Total      │ $530,000 │ $290,000 │  30   │ $17,667                      │
│                                                                             │
│  Target: $300,000                                                           │
│  Forecast: $290,000 (97% of target)                                         │
│  Gap: -$10,000                                                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Opportunity (Create Opportunity)

**Nghiệp vụ thực tế:**
- Khi lead đã qualified và sẵn sàng thảo luận về deal cụ thể
- Khi có cơ hội upsell/cross-sell từ khách hàng hiện tại
- Khi có renewal/gia hạn hợp đồng sắp đến hạn

**Ví dụ thực tế:**
> Lead ABC Corp đã confirm budget $100K và muốn nhận proposal. Sales tạo Opportunity:
> - Name: "ABC Corp - Enterprise License"
> - Amount: $100,000
> - Pipeline: Sales Pipeline
> - Stage: Proposal
> - Expected Close Date: 15/02/2026
> - Type: New Business

---

### 2. Xem danh sách Opportunities (Get All Opportunities)

**Nghiệp vụ thực tế:**
- Xem tổng quan pipeline, bao nhiêu deals đang chạy
- Filter theo stage để focus vào deals cần attention
- Filter theo customer để xem tất cả deals của một khách

**Ví dụ thực tế:**
> Sales Manager mỗi sáng review pipeline:
> - Filter Stage = Negotiation → 3 deals sắp close, cần hỗ trợ gì?
> - Filter Expected Close = This Month → 8 deals, đủ target không?

---

### 3. Xem chi tiết Opportunity (Get Opportunity by ID)

**Nghiệp vụ thực tế:**
- Xem full context của deal trước khi meeting
- Biết deal từ lead/campaign nào, ai là contact chính
- Xem lịch sử: đã gửi quotation nào, có activity gì

**Ví dụ thực tế:**
> Trước cuộc gọi với CFO của ABC Corp về deal $100K:
> - Xem Description: "Cần enterprise features, integration với SAP"
> - Xem Primary Contact: CFO Trần Thị B
> - Xem Priority: High
> - Xem Competition: Đang so sánh với Salesforce

---

### 4. Cập nhật Opportunity (Update Opportunity)

**Nghiệp vụ thực tế:**
- Điều chỉnh Amount khi thay đổi scope
- Update Expected Close Date khi deal kéo dài
- Cập nhật Probability khi có thêm thông tin

**Ví dụ thực tế:**
> Sau meeting, ABC Corp muốn thêm module Training (+$20K):
> - Update Amount: $100K → $120K
> - Update Description: "Thêm Training package"
> - Update Probability: 60% → 75% (deal tích cực hơn)

---

### 5. Move Stage (Chuyển giai đoạn)

**Nghiệp vụ thực tế:**
- Di chuyển opportunity qua các stages trong pipeline
- Probability tự động update theo stage
- Khi move đến Won/Lost, đóng opportunity

**Ví dụ thực tế:**
> ABC Corp đồng ý ký hợp đồng:
> - Move từ Negotiation → Closed Won
> - Win Reason: "Best features, competitive price"
> - Probability tự động = 100%
> - Actual Close Date = Today

> XYZ Corp chọn đối thủ:
> - Move từ Proposal → Closed Lost
> - Loss Reason: "Competitor lower price"
> - Opportunity status = Lost

---

### 6. Xóa Opportunity (Delete Opportunity)

**Nghiệp vụ thực tế:**
- Xóa opportunity duplicate hoặc tạo nhầm
- Xóa deal không còn relevant (khách hàng close down)
- Soft delete để giữ lịch sử cho báo cáo

---

### 7. Get Forecast (Dự báo doanh thu)

**Nghiệp vụ thực tế:**
- Dự báo doanh thu theo tháng dựa trên pipeline hiện tại
- Tính weighted value để có con số thực tế hơn
- So sánh với target để biết cần bao nhiêu deals nữa

**Ví dụ thực tế:**
> Cuối tháng 1, Sales Manager chạy forecast Q1:
> - January: $85K weighted (đã close $60K, còn $25K open)
> - February: $110K weighted
> - March: $95K weighted
> - Q1 Total: $290K vs Target $300K → cần thêm $10K

---

## Opportunity Types

### Phân loại theo nguồn gốc

| Type | Mô tả | Ví dụ |
|------|-------|-------|
| **New Business** | Khách hàng mới lần đầu mua | ABC Corp chưa từng mua |
| **Existing Business** | Khách hàng cũ mua thêm | ABC Corp mua thêm module |
| **Renewal** | Gia hạn hợp đồng | ABC Corp renew license năm 2 |
| **Upsell** | Nâng cấp gói/dịch vụ | ABC Corp upgrade từ Pro lên Enterprise |

### Ý nghĩa với báo cáo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    REVENUE BY OPPORTUNITY TYPE                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Type              │ Deals │  Amount   │  % Revenue │  Avg Size             │
│  ─────────────────────────────────────────────────────────────────────────  │
│  New Business      │  15   │ $250,000  │    50%     │ $16,667               │
│  Existing Business │   8   │ $100,000  │    20%     │ $12,500               │
│  Renewal           │  12   │ $120,000  │    24%     │ $10,000               │
│  Upsell            │   5   │  $30,000  │     6%     │  $6,000               │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Total             │  40   │ $500,000  │   100%     │ $12,500               │
│                                                                             │
│  Insight: 50% revenue từ New Business → cần balance với Retention           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Priority System

| Priority | Khi nào sử dụng | Actions |
|----------|-----------------|---------|
| **High** | Deal lớn, close sớm, strategic customer | Daily follow-up, C-level engagement |
| **Medium** | Deal thông thường | Weekly follow-up |
| **Low** | Deal nhỏ, timing dài | Monthly check-in |

---

## Win/Loss Analysis

### Win Reasons thường gặp

| Reason | Mô tả | Action |
|--------|-------|--------|
| Best Features | Sản phẩm phù hợp nhất | Document winning features |
| Competitive Price | Giá cạnh tranh | Maintain pricing strategy |
| Strong Relationship | Quan hệ tốt | Invest in relationship |
| Good Support | Hỗ trợ tốt | Highlight in sales process |

### Loss Reasons thường gặp

| Reason | Mô tả | Action |
|--------|-------|--------|
| Competitor Lower Price | Đối thủ giá rẻ hơn | Review pricing, value proposition |
| Missing Features | Thiếu tính năng | Product feedback loop |
| Bad Timing | Không đúng thời điểm | Re-engage later |
| No Budget | Không có ngân sách | Qualify budget earlier |
| Chose Status Quo | Không đổi | Better urgency creation |

---

## Tích hợp với các module khác

```
                    ┌───────────────────┐
                    │   OPPORTUNITY     │
                    │   (Sales Deal)    │
                    └─────────┬─────────┘
                              │
    ┌────────────┬────────────┼────────────┬────────────┐
    │            │            │            │            │
    ▼            ▼            ▼            ▼            ▼
┌────────┐ ┌──────────┐ ┌──────────┐ ┌─────────┐ ┌───────────┐
│  LEAD  │ │ CUSTOMER │ │ PIPELINE │ │QUOTATION│ │ ACTIVITY  │
│        │ │          │ │  STAGE   │ │         │ │           │
│ Source │ │ Who      │ │ Where    │ │ Pricing │ │ Actions   │
└────────┘ └──────────┘ └──────────┘ └─────────┘ └───────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │          OUTCOME              │
              ├───────────────┬───────────────┤
              │     WON       │     LOST      │
              │      ↓        │      ↓        │
              │   ORDER       │  ANALYSIS     │
              │  CONTRACT     │  RE-ENGAGE    │
              └───────────────┴───────────────┘
```

| Module | Quan hệ | Mô tả |
|--------|---------|-------|
| **Lead** | Lead → Opportunity | Opportunity có thể từ lead convert |
| **Customer** | Opportunity của Customer | Deal cho khách hàng cụ thể |
| **Pipeline/Stage** | Opportunity trong Stage | Tracking vị trí trong pipeline |
| **Quotation** | Opportunity có Quotations | Các báo giá đã gửi |
| **Activity** | Opportunity có Activities | Meeting, call, task liên quan |
| **Order** | Won → Order | Khi thắng tạo đơn hàng |
| **Contract** | Won → Contract | Khi thắng tạo hợp đồng |
| **Campaign** | Opportunity từ Campaign | Tracking marketing attribution |

---

## Technical Overview

**Base URL:** `/api/v1/opportunities`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Opportunities

Lấy danh sách opportunities với phân trang và filter.

```
GET /api/v1/opportunities
```

**Permission Required:** `opportunity.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Số items mỗi trang (max: 100) |
| `sortBy` | string | No | "CreatedAt" | Field để sắp xếp |
| `sortDescending` | bool | No | false | Sắp xếp giảm dần |
| `search` | string | No | - | Tìm kiếm theo name |
| `status` | OpportunityStatus | No | - | Filter theo status (Open/Won/Lost) |
| `pipelineId` | Guid | No | - | Filter theo pipeline |
| `stageId` | Guid | No | - | Filter theo stage |
| `customerId` | Guid | No | - | Filter theo customer |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "ABC Corp - Enterprise License",
        "customerName": "ABC Corporation",
        "stageName": "Negotiation",
        "status": "Open",
        "amount": 100000.00,
        "currency": "USD",
        "probability": 80,
        "weightedAmount": 80000.00,
        "expectedCloseDate": "2026-02-15T00:00:00Z",
        "assignedToUserName": "Sales Rep 1",
        "createdAt": "2026-01-10T10:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3,
    "totalCount": 25,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

---

### 2. Get Opportunity by ID

Lấy chi tiết một opportunity.

```
GET /api/v1/opportunities/{id}
```

**Permission Required:** `opportunity.view`

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "ABC Corp - Enterprise License",
    "customerId": "customer-guid",
    "customerName": "ABC Corporation",
    "pipelineId": "pipeline-guid",
    "pipelineName": "Sales Pipeline",
    "stageId": "stage-guid",
    "stageName": "Negotiation",
    "status": "Open",
    "amount": 100000.00,
    "currency": "USD",
    "probability": 80,
    "weightedAmount": 80000.00,
    "expectedCloseDate": "2026-02-15T00:00:00Z",
    "actualCloseDate": null,
    "type": "NewBusiness",
    "priority": "High",
    "lossReason": null,
    "winReason": null,
    "description": "Enterprise license with SAP integration",
    "assignedToUserId": "user-guid",
    "assignedToUserName": "Sales Rep 1",
    "createdAt": "2026-01-10T10:00:00Z",
    "updatedAt": "2026-01-15T14:00:00Z"
  }
}
```

---

### 3. Create Opportunity

Tạo opportunity mới.

```
POST /api/v1/opportunities
```

**Permission Required:** `opportunity.create`

#### Request Body

```json
{
  "name": "ABC Corp - Enterprise License",
  "customerId": "customer-guid",
  "primaryContactId": "contact-guid",
  "pipelineId": "pipeline-guid",
  "stageId": "stage-guid",
  "amount": 100000.00,
  "currency": "USD",
  "probability": 60,
  "expectedCloseDate": "2026-02-15",
  "type": 0,
  "priority": 2,
  "description": "Enterprise license with SAP integration",
  "assignedToUserId": "user-guid"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Tên opportunity |
| `customerId` | Guid | No | ID customer |
| `primaryContactId` | Guid | No | ID contact chính |
| `pipelineId` | Guid | **Yes** | ID pipeline |
| `stageId` | Guid | **Yes** | ID stage ban đầu |
| `amount` | decimal | **Yes** | Giá trị deal |
| `currency` | string | No | Đơn vị tiền (default: USD) |
| `probability` | int | No | % xác suất (auto từ stage nếu không set) |
| `expectedCloseDate` | DateTime | No | Ngày dự kiến close |
| `type` | OpportunityType | No | Loại opportunity |
| `priority` | OpportunityPriority | No | Độ ưu tiên |
| `description` | string | No | Mô tả chi tiết |
| `assignedToUserId` | Guid | No | ID người phụ trách |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-opportunity-guid",
    "name": "ABC Corp - Enterprise License",
    "status": "Open",
    "amount": 100000.00,
    "probability": 60,
    "createdAt": "2026-01-18T12:00:00Z"
  }
}
```

---

### 4. Update Opportunity

Cập nhật thông tin opportunity.

```
PUT /api/v1/opportunities/{id}
```

**Permission Required:** `opportunity.update`

#### Request Body (All fields optional)

```json
{
  "name": "ABC Corp - Enterprise License + Training",
  "stageId": "new-stage-guid",
  "amount": 120000.00,
  "probability": 75,
  "expectedCloseDate": "2026-02-28",
  "description": "Added training package"
}
```

---

### 5. Move Stage

Chuyển opportunity sang stage khác trong pipeline.

```
POST /api/v1/opportunities/{id}/move-stage
```

**Permission Required:** `opportunity.update`

#### Request Body

```json
{
  "stageId": "closed-won-stage-guid",
  "reason": "Customer signed contract after successful POC"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `stageId` | Guid | **Yes** | ID stage mới |
| `reason` | string | No | Lý do (required cho Won/Lost stages) |

#### Response

```json
{
  "success": true,
  "message": "Stage updated successfully"
}
```

**Lưu ý:**
- Probability tự động update theo stage mới
- Nếu stage có `isWon = true`: Status = Won, Actual Close Date = Now
- Nếu stage có `isLost = true`: Status = Lost, Actual Close Date = Now

---

### 6. Delete Opportunity

Xóa mềm opportunity.

```
DELETE /api/v1/opportunities/{id}
```

**Permission Required:** `opportunity.delete`

---

### 7. Get Forecast

Dự báo doanh thu theo tháng.

```
GET /api/v1/opportunities/forecast
```

**Permission Required:** `opportunity.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `startDate` | DateTime | No | -1 month | Ngày bắt đầu |
| `endDate` | DateTime | No | +3 months | Ngày kết thúc |

#### Response

```json
{
  "success": true,
  "data": {
    "totalPipeline": 530000.00,
    "weightedPipeline": 290000.00,
    "opportunityCount": 30,
    "byMonth": [
      {
        "year": 2026,
        "month": 1,
        "total": 150000.00,
        "weighted": 85000.00,
        "count": 8
      },
      {
        "year": 2026,
        "month": 2,
        "total": 200000.00,
        "weighted": 110000.00,
        "count": 12
      },
      {
        "year": 2026,
        "month": 3,
        "total": 180000.00,
        "weighted": 95000.00,
        "count": 10
      }
    ]
  }
}
```

---

## Enums

### OpportunityStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Open | Đang mở, chưa đóng |
| 1 | Won | Đã thắng |
| 2 | Lost | Đã thua |

### OpportunityType

| Value | Name | Description |
|-------|------|-------------|
| 0 | NewBusiness | Khách hàng mới |
| 1 | ExistingBusiness | Khách hàng cũ mua thêm |
| 2 | Renewal | Gia hạn |
| 3 | Upsell | Nâng cấp |

### OpportunityPriority

| Value | Name | Description |
|-------|------|-------------|
| 0 | Low | Thấp |
| 1 | Medium | Trung bình |
| 2 | High | Cao |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `opportunity.view` | Xem danh sách và chi tiết opportunity |
| `opportunity.create` | Tạo opportunity mới |
| `opportunity.update` | Cập nhật thông tin, move stage |
| `opportunity.delete` | Xóa opportunity |

---

## Example: Complete Opportunity Lifecycle

### 1. Tạo Opportunity từ Lead Convert

```bash
curl -X POST "http://localhost:5000/api/v1/opportunities" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "ABC Corp - Enterprise License",
    "customerId": "customer-guid",
    "pipelineId": "default-pipeline-guid",
    "stageId": "prospecting-stage-guid",
    "amount": 100000,
    "expectedCloseDate": "2026-03-15",
    "type": 0,
    "priority": 2,
    "description": "Enterprise license for 500 users"
  }'
```

### 2. Move qua Qualification

```bash
curl -X POST "http://localhost:5000/api/v1/opportunities/{id}/move-stage" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"stageId": "qualification-stage-guid"}'
```

### 3. Update Amount sau khi thảo luận

```bash
curl -X PUT "http://localhost:5000/api/v1/opportunities/{id}" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 120000,
    "description": "Added training package +$20K"
  }'
```

### 4. Close Won

```bash
curl -X POST "http://localhost:5000/api/v1/opportunities/{id}/move-stage" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "stageId": "closed-won-stage-guid",
    "reason": "Best features and competitive pricing"
  }'
```

### 5. Get Forecast

```bash
curl -X GET "http://localhost:5000/api/v1/opportunities/forecast?startDate=2026-01-01&endDate=2026-03-31" \
  -H "Authorization: Bearer {token}"
```

---

## Best Practices

### 1. Luôn có Expected Close Date

- Mọi opportunity cần có deadline
- Review và update nếu deal kéo dài
- Helps forecast accuracy

### 2. Update Probability realistically

- Không quá lạc quan (over-forecast)
- Dựa trên criteria khách quan, không cảm tính
- Stage probability là baseline, có thể adjust

### 3. Document Win/Loss Reasons

- Analyze patterns để improve
- Share learnings với team
- Product feedback từ loss reasons

### 4. Regular Pipeline Reviews

- Weekly review với manager
- Focus on stuck deals
- Update stale opportunities

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
