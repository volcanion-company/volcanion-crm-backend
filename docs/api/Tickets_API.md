# Tickets API Documentation

## Tổng quan Module

### Ticket là gì?

**Ticket (Phiếu hỗ trợ)** là yêu cầu hỗ trợ, khiếu nại, hoặc câu hỏi từ khách hàng cần được xử lý bởi đội ngũ Customer Support/Service. Mỗi ticket đại diện cho một vấn đề cần giải quyết.

### Tại sao cần hệ thống Ticketing?

Trong dịch vụ khách hàng hiện đại, hệ thống ticketing là thiết yếu:

| Vấn đề thực tế | Giải pháp của module Ticket |
|----------------|----------------------------|
| Email support bị mất, trùng lặp | Mỗi yêu cầu có ticket number duy nhất |
| Không biết ai đang xử lý vấn đề gì | Assignment system, status tracking |
| Không ưu tiên được ticket khẩn cấp | Priority system với SLA |
| Khách hàng hỏi "vấn đề của tôi đến đâu rồi?" | Status và timeline rõ ràng |
| Không đo lường được chất lượng support | Response time, resolution time, satisfaction rating |

### Support Workflow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        TICKET LIFECYCLE                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────┐    ┌──────┐    ┌────────────┐    ┌──────────┐    ┌──────────┐     │
│  │ NEW  │───►│ OPEN │───►│ IN PROGRESS│───►│ RESOLVED │───►│ CLOSED   │     │
│  └──────┘    └──────┘    └────────────┘    └──────────┘    └──────────┘     │
│     │           │               │                 │              ▲          │
│     │           │               │                 │              │          │
│     │           │               ▼                 │              │          │
│     │           │         ┌──────────┐            │              │          │
│     │           │         │ PENDING  │            │              │          │
│     │           │         │(Waiting  │────────────┘              │          │
│     │           │         │Customer) │                           │          │
│     │           │         └──────────┘                           │          │
│     │           │               │                                │          │
│     │           │               ▼                                │          │
│     │           │         ┌──────────┐                           │          │
│     │           │         │ ON HOLD  │───────────────────────────┘          │
│     │           │         └──────────┘                                      │
│     │           │                                                           │
│     │           ▼                                                           │
│     │      ┌──────────┐                                                     │
│     └─────►│ REOPENED │─────────────────────────────────────────►           │
│            └──────────┘                                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## SLA (Service Level Agreement)

### SLA là gì?

**SLA** là cam kết về thời gian xử lý ticket dựa trên priority. Ví dụ:

| Priority | First Response | Resolution Time |
|----------|----------------|-----------------|
| **Critical** | 15 phút | 2 giờ |
| **High** | 1 giờ | 8 giờ |
| **Medium** | 4 giờ | 24 giờ |
| **Low** | 8 giờ | 48 giờ |

### SLA Metrics

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        TICKET TKT-000123                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Priority: High                                                             │
│  SLA: Standard Support                                                      │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ First Response Target: 1 hour                                        │   │
│  │ Actual First Response: 45 minutes                                    │   │
│  │ Status: MET                                                          │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ Resolution Target: 8 hours                                           │   │
│  │ Time Elapsed: 6 hours 30 minutes                                     │   │
│  │ Time Remaining: 1 hour 30 minutes                                    │   │
│  │ Time Paused: 1 hour (waiting for customer info)                      │   │
│  │ Status: ON TRACK                                                     │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  SLA Breached: No                                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### SLA Pause/Resume

**Khi nào pause SLA?**
- Chờ customer cung cấp thêm thông tin
- Chờ bên thứ 3 (vendor, partner)
- Khách hàng yêu cầu hoãn xử lý

**Ví dụ:**
> Ticket critical cần log file từ khách. Support pause SLA lúc 10:00, khách gửi log lúc 14:00, resume SLA. 4 giờ chờ không tính vào SLA time.

---

## Priority System

### Cách phân loại Priority

| Priority | Khi nào sử dụng | Ví dụ |
|----------|-----------------|-------|
| **Critical** | Hệ thống down, mất dữ liệu, ảnh hưởng toàn bộ users | "Không login được, tất cả users bị affected" |
| **High** | Tính năng quan trọng không hoạt động, ảnh hưởng nhiều users | "Payment gateway không hoạt động" |
| **Medium** | Lỗi không nghiêm trọng, có workaround | "Report export chậm" |
| **Low** | Câu hỏi, yêu cầu cải tiến, vấn đề nhỏ | "Làm sao đổi password?" |

### Priority Escalation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     AUTOMATIC PRIORITY ESCALATION                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Ticket: TKT-000456                                                         │
│  Original Priority: Medium                                                  │
│                                                                             │
│  Timeline:                                                                  │
│  ─────────────────────────────────────────────────────────────────────────  │
│  10:00 - Ticket created (Medium Priority)                                   │
│  14:00 - No first response after 4h → Auto escalate to High                 │
│  18:00 - Still no response after 8h → Auto escalate to Critical             │
│  18:15 - Assign to senior support engineer                                  │
│  18:30 - First response sent                                                │
│                                                                             │
│  Escalation Count: 2                                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ticket Types

### Phân loại theo tính chất

| Type | Mô tả | Ví dụ | Typical Flow |
|------|-------|-------|--------------|
| **Question** | Câu hỏi về sử dụng | "Làm sao export data?" | New → Open → Resolved → Closed |
| **Problem** | Vấn đề kỹ thuật cần troubleshoot | "Không thể sync data" | New → InProgress → Resolved |
| **Incident** | Sự cố nghiêm trọng | "Server down" | New → Critical → Escalate → Resolved |
| **Feature Request** | Yêu cầu tính năng mới | "Cần integration với Slack" | New → Pending → (chuyển Product team) |
| **Task** | Công việc cần làm | "Setup account cho 10 users" | New → InProgress → Closed |

---

## Channels (Kênh tiếp nhận)

| Channel | Mô tả | Automation |
|---------|-------|------------|
| **Email** | Từ support@company.com | Auto-create ticket từ email |
| **Web** | Submit form trên website | Instant ticket creation |
| **Phone** | Gọi hotline | Agent tạo ticket thủ công |
| **Chat** | Live chat | Bot hoặc agent tạo ticket |
| **Social Media** | Facebook, Twitter | Monitor và tạo ticket |
| **API** | Từ ứng dụng khác | Programmatic ticket creation |

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Ticket (Create Ticket)

**Nghiệp vụ thực tế:**
- Khách gửi email support → Tự động tạo ticket
- Khách gọi hotline → Agent tạo ticket
- Khách điền form trên website → Tạo ticket với thông tin đầy đủ

**Ví dụ thực tế:**
> Khách hàng ABC Corp gửi email: "Không login được hệ thống". Support agent:
> - Tạo ticket mới
> - Subject: "Login issue - ABC Corp"
> - Priority: High (ảnh hưởng công việc)
> - Type: Problem
> - Customer: ABC Corporation
> - Channel: Email
> - Hệ thống tự động assign SLA và generate ticket number TKT-000789

---

### 2. Xem danh sách Tickets (Get All Tickets)

**Nghiệp vụ thực tế:**
- Support team xem tất cả tickets cần xử lý
- Filter theo status để xem tickets đang open
- Filter theo priority để ưu tiên critical tickets
- Search theo ticket number hoặc customer name

**Ví dụ thực tế:**
> Support Manager mỗi sáng:
> - Filter Status = Open, Priority = Critical → 3 tickets cần attention ngay
> - Filter Status = Pending → 15 tickets đang chờ customer response
> - Filter Assigned To = Me → 8 tickets của tôi cần follow-up

---

### 3. My Tickets (Get My Tickets)

**Nghiệp vụ thực tế:**
- Support agent xem các tickets được assign cho mình
- Ưu tiên xử lý tickets theo priority và due date

**Ví dụ thực tế:**
> Support Agent A login vào hệ thống, vào "My Tickets":
> - 2 Critical tickets (SLA sắp breach)
> - 5 High tickets
> - 10 Medium tickets
> → Focus vào Critical trước

---

### 4. Overdue Tickets (Get Overdue Tickets)

**Nghiệp vụ thực tế:**
- Xem các tickets đã quá due date, SLA breach
- Cần xử lý khẩn cấp để không mất khách hàng

**Ví dụ thực tế:**
> Support Manager nhận alert: 5 tickets overdue
> - TKT-000123: Overdue 2 hours, Customer VIP → Escalate immediately
> - TKT-000456: Overdue 30 minutes → Follow up with agent
> - Gọi meeting để giải quyết

---

### 5. Assign Ticket (Assign Ticket)

**Nghiệp vụ thực tế:**
- Phân công ticket cho agent phù hợp (theo skill, workload)
- Re-assign khi agent quá tải hoặc nghỉ

**Ví dụ thực tế:**
> Ticket về database issue:
> - Manager assign cho Agent B (chuyên database)
> - Agent B có 5 tickets, workload vừa phải
> - Agent A có 15 tickets → Không assign thêm

---

### 6. Resolve Ticket (Resolve Ticket)

**Nghiệp vụ thực tế:**
- Agent đã giải quyết xong vấn đề
- Chờ customer confirm trước khi close

**Ví dụ thực tế:**
> Agent fix xong login issue, reply email:
> - "Đã fix xong, vui lòng thử lại"
> - Update status: Resolved
> - Auto send email cho customer
> - Nếu customer không phản hồi sau 3 ngày → Auto close

---

### 7. Close Ticket (Close Ticket)

**Nghiệp vụ thực tế:**
- Ticket đã resolved và customer confirm OK
- Hoặc customer không phản hồi sau thời gian chờ

**Ví dụ thực tế:**
> Customer reply: "Đã OK rồi, cảm ơn!"
> - Agent close ticket
> - Gửi satisfaction survey
> - Ticket chuyển sang Closed, không thể reopen (trừ khi admin)

---

### 8. Pause/Resume SLA

**Nghiệp vụ thực tế:**
- Pause SLA khi chờ customer/vendor
- Resume khi nhận được thông tin

**Ví dụ thực tế:**
> Ticket critical cần database backup file từ customer:
> - 10:00: Agent request file, pause SLA (reason: "Waiting for backup file")
> - 14:00: Customer gửi file, resume SLA
> - 4 giờ chờ không tính vào SLA → Không breach

---

### 9. Escalate Ticket

**Nghiệp vụ thực tế:**
- Ticket quá khó, cần senior xử lý
- Ticket SLA sắp breach, cần attention
- Customer VIP yêu cầu escalate

**Ví dụ thực tế:**
> Agent không troubleshoot được issue phức tạp:
> - Escalate lên Senior Engineer
> - Priority tự động tăng: Medium → High
> - Escalation Count: 1
> - Senior nhận ticket và xử lý

---

### 10. Xóa Ticket (Delete Ticket)

**Nghiệp vụ thực tế:**
- Xóa ticket spam, duplicate
- Soft delete để giữ lịch sử

---

## Satisfaction Rating

### Đánh giá chất lượng support

Sau khi ticket closed, gửi survey:

| Rating | Ý nghĩa | Action |
|--------|---------|--------|
| ⭐⭐⭐⭐⭐ (5) | Excellent | Share với team, reward agent |
| ⭐⭐⭐⭐ (4) | Good | OK |
| ⭐⭐⭐ (3) | Average | Review, improve |
| ⭐⭐ (2) | Poor | Manager follow up, training |
| ⭐ (1) | Very Poor | Manager call customer, investigate |

### Ví dụ Report

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SUPPORT QUALITY REPORT - Q1 2026                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Total Tickets Closed: 450                                                  │
│  Rated Tickets: 360 (80%)                                                   │
│                                                                             │
│  Rating Distribution:                                                       │
│  ⭐⭐⭐⭐⭐ (5): 180 (50%)  ████████████████████████████                  │
│  ⭐⭐⭐⭐ (4):  108 (30%)  ████████████████                               │
│  ⭐⭐⭐ (3):    54 (15%)  ████████                                         │
│  ⭐⭐ (2):     14 (4%)   ██                                                │
│  ⭐ (1):       4 (1%)   █                                                   │
│                                                                             │
│  Average Rating: 4.2 / 5.0                                                  │
│  CSAT (Customer Satisfaction): 80%                                          │
│                                                                             │
│  Top Agent: Agent B - 4.8 avg rating                                        │
│  Needs Improvement: Agent C - 3.5 avg rating                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Auto-categorization & Auto-assignment

### Auto-categorization

Dựa trên subject/description, AI tự động phân loại:

| Keywords | Category | SubCategory |
|----------|----------|-------------|
| "login", "password", "authentication" | Account | Login Issues |
| "payment", "invoice", "billing" | Billing | Payment Problems |
| "slow", "performance", "loading" | Technical | Performance |
| "bug", "error", "crash" | Technical | Bug Report |

### Auto-assignment

Dựa trên category, priority, agent workload:

| Rule | Assignment |
|------|-----------|
| Category = "Billing" | → Billing Team |
| Priority = Critical + Category = "Technical" | → Senior Engineer |
| Agent A workload < 10 tickets | → Agent A |
| After hours | → On-call agent |

---

## Tích hợp với các module khác

```
                    ┌───────────────┐
                    │    TICKET     │
                    │  (Support)    │
                    └───────┬───────┘
                            │
      ┌─────────────────────┼──────────────────────┐
      │              │            │                │
      ▼              ▼            ▼                ▼
┌──────────┐  ┌──────────┐  ┌─────────┐  ┌────────────┐
│ CUSTOMER │  │ CONTACT  │  │   SLA   │  │  ACTIVITY  │
│          │  │          │  │         │  │            │
│ Who      │  │ Reporter │  │ Timing  │  │ Follow-up  │
└──────────┘  └──────────┘  └─────────┘  └────────────┘
      │              │
      │              ▼
      │       ┌──────────────┐
      │       │   COMMENTS   │
      │       │              │
      │       │ Conversation │
      │       └──────────────┘
      ▼
┌──────────────┐
│ INTERACTIONS │
│              │
│   History    │
└──────────────┘
```

| Module | Quan hệ | Mô tả |
|--------|---------|-------|
| **Customer** | Ticket của Customer | Xem tất cả tickets của một khách hàng |
| **Contact** | Ticket từ Contact | Người báo cáo issue |
| **SLA** | Ticket có SLA | Cam kết thời gian xử lý |
| **Activity** | Ticket có Activities | Follow-up tasks |
| **Comments** | Ticket có Comments | Conversation thread |
| **Interactions** | Ticket là Interaction | Ghi nhận vào lịch sử |

---

## Technical Overview

**Base URL:** `/api/v1/tickets`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Tickets

Lấy danh sách tickets với phân trang và filter.

```
GET /api/v1/tickets
```

**Permission Required:** `ticket.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Số items mỗi trang (max: 100) |
| `sortBy` | string | No | "CreatedAt" | Field để sắp xếp |
| `sortDescending` | bool | No | false | Sắp xếp giảm dần |
| `search` | string | No | - | Tìm kiếm theo ticketNumber, subject |
| `status` | TicketStatus | No | - | Filter theo status |
| `priority` | TicketPriority | No | - | Filter theo priority |
| `assignedTo` | Guid | No | - | Filter theo user được assign |
| `customerId` | Guid | No | - | Filter theo customer |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "ticketNumber": "TKT-000123",
        "subject": "Cannot login to system",
        "customerName": "ABC Corporation",
        "status": "InProgress",
        "priority": "High",
        "type": "Problem",
        "channel": "Email",
        "assignedToUserName": "Support Agent 1",
        "slaName": "Standard Support",
        "dueDate": "2026-01-18T18:00:00Z",
        "slaBreached": false,
        "createdAt": "2026-01-18T10:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 48,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

---

### 2. Get My Tickets

Lấy danh sách tickets được assign cho user hiện tại.

```
GET /api/v1/tickets/my-tickets
```

**Permission Required:** `ticket.view`

---

### 3. Get Overdue Tickets

Lấy danh sách tickets quá hạn (SLA breach).

```
GET /api/v1/tickets/overdue
```

**Permission Required:** `ticket.view`

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "ticket-guid",
      "ticketNumber": "TKT-000456",
      "subject": "Database connection timeout",
      "customerName": "XYZ Technology",
      "status": "Open",
      "priority": "Critical",
      "assignedToUserName": "Support Agent 2",
      "dueDate": "2026-01-18T08:00:00Z",
      "slaBreached": true,
      "createdAt": "2026-01-18T06:00:00Z"
    }
  ]
}
```

---

### 4. Get Ticket by ID

Lấy chi tiết một ticket.

```
GET /api/v1/tickets/{id}
```

**Permission Required:** `ticket.view`

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "ticketNumber": "TKT-000123",
    "subject": "Cannot login to system",
    "description": "After latest update, getting 'Invalid credentials' error even with correct password.",
    "customerName": "ABC Corporation",
    "contactName": "Nguyen Van A",
    "status": "InProgress",
    "priority": "High",
    "type": "Problem",
    "channel": "Email",
    "category": "Account",
    "subCategory": "Login Issues",
    "assignedToUserName": "Support Agent 1",
    "slaName": "Standard Support",
    "dueDate": "2026-01-18T18:00:00Z",
    "firstResponseDate": "2026-01-18T10:30:00Z",
    "resolvedDate": null,
    "closedDate": null,
    "satisfactionRating": null,
    "satisfactionComment": null,
    "slaBreached": false,
    "slaPaused": false,
    "slaPausedMinutes": 0,
    "escalationCount": 0,
    "tags": "[\"login\", \"urgent\"]",
    "createdAt": "2026-01-18T10:00:00Z"
  }
}
```

---

### 5. Create Ticket

Tạo ticket mới.

```
POST /api/v1/tickets
```

**Permission Required:** `ticket.create`

#### Request Body

```json
{
  "subject": "Cannot export reports",
  "description": "When clicking 'Export to Excel', getting timeout error after 30 seconds.",
  "customerId": "customer-guid",
  "contactId": "contact-guid",
  "type": 1,
  "priority": 1,
  "channel": "Email",
  "slaId": "sla-guid",
  "assignedToUserId": "user-guid",
  "tags": "[\"export\", \"reports\"]",
  "dueDate": "2026-01-19T10:00:00Z"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `subject` | string | **Yes** | Tiêu đề ticket |
| `description` | string | No | Mô tả chi tiết vấn đề |
| `customerId` | Guid | No | ID customer |
| `contactId` | Guid | No | ID contact báo cáo |
| `type` | TicketType | No | Loại ticket (default: Question) |
| `priority` | TicketPriority | No | Độ ưu tiên (default: Medium) |
| `channel` | string | No | Kênh tiếp nhận |
| `slaId` | Guid | No | ID SLA áp dụng |
| `assignedToUserId` | Guid | No | ID agent xử lý |
| `tags` | string | No | Tags (JSON array) |
| `dueDate` | DateTime | No | Due date (auto tính nếu có SLA) |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-ticket-guid",
    "ticketNumber": "TKT-000790",
    "subject": "Cannot export reports",
    "status": "New",
    "priority": "Medium",
    "createdAt": "2026-01-18T12:00:00Z"
  }
}
```

---

### 6. Update Ticket

Cập nhật thông tin ticket.

```
PUT /api/v1/tickets/{id}
```

**Permission Required:** `ticket.update`

#### Request Body (All fields optional)

```json
{
  "subject": "Cannot export reports - URGENT",
  "description": "Updated: Affects all users in Finance department",
  "priority": 2,
  "type": 1,
  "dueDate": "2026-01-18T18:00:00Z"
}
```

---

### 7. Assign Ticket

Phân công ticket cho agent.

```
POST /api/v1/tickets/{id}/assign
```

**Permission Required:** `ticket.assign`

#### Request Body

```json
{
  "userId": "agent-user-guid"
}
```

---

### 8. Resolve Ticket

Đánh dấu ticket đã giải quyết.

```
POST /api/v1/tickets/{id}/resolve
```

**Permission Required:** `ticket.update`

#### Response

```json
{
  "success": true,
  "message": "Ticket resolved"
}
```

---

### 9. Close Ticket

Đóng ticket.

```
POST /api/v1/tickets/{id}/close
```

**Permission Required:** `ticket.update`

---

### 10. Pause SLA

Tạm dừng SLA tracking.

```
POST /api/v1/tickets/{id}/pause-sla
```

**Permission Required:** `ticket.update`

#### Request Body

```json
{
  "reason": "Waiting for customer to provide database backup file"
}
```

---

### 11. Resume SLA

Tiếp tục SLA tracking.

```
POST /api/v1/tickets/{id}/resume-sla
```

**Permission Required:** `ticket.update`

#### Response

```json
{
  "success": true,
  "data": {
    "message": "SLA resumed successfully",
    "totalPausedMinutes": 240
  }
}
```

---

### 12. Escalate Ticket

Leo thang ticket lên level cao hơn.

```
POST /api/v1/tickets/{id}/escalate
```

**Permission Required:** `ticket.update`

#### Response

```json
{
  "success": true,
  "data": {
    "message": "Ticket escalated successfully",
    "escalatedTo": "Senior Engineer Name",
    "newPriority": "Critical",
    "escalationCount": 1
  }
}
```

---

### 13. Delete Ticket

Xóa mềm ticket.

```
DELETE /api/v1/tickets/{id}
```

**Permission Required:** `ticket.delete`

---

## Enums

### TicketStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | New | Mới tạo |
| 1 | Open | Đã mở |
| 2 | InProgress | Đang xử lý |
| 3 | Pending | Chờ (customer/vendor) |
| 4 | OnHold | Tạm hoãn |
| 5 | Resolved | Đã giải quyết |
| 6 | Closed | Đã đóng |
| 7 | Reopened | Mở lại |

### TicketPriority

| Value | Name | Description |
|-------|------|-------------|
| 0 | Low | Thấp |
| 1 | Medium | Trung bình |
| 2 | High | Cao |
| 3 | Critical | Khẩn cấp |

### TicketType

| Value | Name | Description |
|-------|------|-------------|
| 0 | Question | Câu hỏi |
| 1 | Problem | Vấn đề |
| 2 | Incident | Sự cố |
| 3 | FeatureRequest | Yêu cầu tính năng |
| 4 | Task | Công việc |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `ticket.view` | Xem danh sách và chi tiết ticket |
| `ticket.create` | Tạo ticket mới |
| `ticket.update` | Cập nhật thông tin, resolve, close |
| `ticket.assign` | Phân công ticket |
| `ticket.delete` | Xóa ticket |

---

## Example: Complete Ticket Lifecycle

### 1. Customer gửi email support

```bash
# System tự động tạo ticket từ email
curl -X POST "http://localhost:5000/api/v1/tickets" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "Cannot login - getting timeout error",
    "description": "Email content...",
    "customerId": "customer-guid",
    "contactId": "contact-guid",
    "type": 1,
    "priority": 2,
    "channel": "Email"
  }'
```

### 2. Auto-assign dựa trên rules

```bash
# Hệ thống tự động assign cho agent có workload thấp nhất
```

### 3. Agent xử lý và update

```bash
curl -X PUT "http://localhost:5000/api/v1/tickets/{ticket-id}" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "priority": 3,
    "description": "Found issue: Redis cache timeout"
  }'
```

### 4. Cần thêm info, pause SLA

```bash
curl -X POST "http://localhost:5000/api/v1/tickets/{ticket-id}/pause-sla" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Waiting for customer log files"
  }'
```

### 5. Nhận info, resume SLA

```bash
curl -X POST "http://localhost:5000/api/v1/tickets/{ticket-id}/resume-sla" \
  -H "Authorization: Bearer {token}"
```

### 6. Fix xong, resolve ticket

```bash
curl -X POST "http://localhost:5000/api/v1/tickets/{ticket-id}/resolve" \
  -H "Authorization: Bearer {token}"
```

### 7. Customer confirm OK, close ticket

```bash
curl -X POST "http://localhost:5000/api/v1/tickets/{ticket-id}/close" \
  -H "Authorization: Bearer {token}"
```

---

## API Integration Guide

### 1. Authentication Setup

**Bước 1: Lấy Access Token**

```javascript
// JavaScript/TypeScript
async function login() {
  const response = await fetch('https://api.yourcrm.com/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      email: 'support@company.com',
      password: 'your-password'
    })
  });
  
  const data = await response.json();
  const token = data.data.accessToken;
  
  // Lưu token
  localStorage.setItem('accessToken', token);
  return token;
}
```

**Bước 2: Sử dụng Token cho mọi request**

```javascript
const token = localStorage.getItem('accessToken');

fetch('https://api.yourcrm.com/api/v1/tickets', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});
```

---

### 2. Complete Integration Examples

#### A. JavaScript/TypeScript (React/Node.js)

```typescript
// ticket-service.ts
import axios from 'axios';

const API_BASE_URL = 'https://api.yourcrm.com/api/v1';
const axiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Thêm token vào mọi request
axiosInstance.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Ticket Service
export class TicketService {
  
  // Lấy danh sách tickets
  async getTickets(params: {
    pageNumber?: number;
    pageSize?: number;
    status?: string;
    priority?: string;
    search?: string;
  }) {
    const response = await axiosInstance.get('/tickets', { params });
    return response.data;
  }
  
  // Lấy tickets của tôi
  async getMyTickets() {
    const response = await axiosInstance.get('/tickets/my-tickets');
    return response.data.data;
  }
  
  // Lấy tickets quá hạn
  async getOverdueTickets() {
    const response = await axiosInstance.get('/tickets/overdue');
    return response.data.data;
  }
  
  // Tạo ticket mới
  async createTicket(ticket: {
    subject: string;
    description?: string;
    customerId?: string;
    contactId?: string;
    type?: number;
    priority?: number;
    channel?: string;
  }) {
    const response = await axiosInstance.post('/tickets', ticket);
    return response.data.data;
  }
  
  // Lấy chi tiết ticket
  async getTicketById(id: string) {
    const response = await axiosInstance.get(`/tickets/${id}`);
    return response.data.data;
  }
  
  // Cập nhật ticket
  async updateTicket(id: string, updates: {
    subject?: string;
    description?: string;
    priority?: number;
    type?: number;
  }) {
    const response = await axiosInstance.put(`/tickets/${id}`, updates);
    return response.data;
  }
  
  // Assign ticket
  async assignTicket(ticketId: string, userId: string) {
    const response = await axiosInstance.post(
      `/tickets/${ticketId}/assign`,
      { userId }
    );
    return response.data;
  }
  
  // Resolve ticket
  async resolveTicket(ticketId: string) {
    const response = await axiosInstance.post(`/tickets/${ticketId}/resolve`);
    return response.data;
  }
  
  // Close ticket
  async closeTicket(ticketId: string) {
    const response = await axiosInstance.post(`/tickets/${ticketId}/close`);
    return response.data;
  }
  
  // Pause SLA
  async pauseSLA(ticketId: string, reason: string) {
    const response = await axiosInstance.post(
      `/tickets/${ticketId}/pause-sla`,
      { reason }
    );
    return response.data;
  }
  
  // Resume SLA
  async resumeSLA(ticketId: string) {
    const response = await axiosInstance.post(`/tickets/${ticketId}/resume-sla`);
    return response.data;
  }
  
  // Escalate ticket
  async escalateTicket(ticketId: string) {
    const response = await axiosInstance.post(`/tickets/${ticketId}/escalate`);
    return response.data;
  }
  
  // Delete ticket
  async deleteTicket(ticketId: string) {
    await axiosInstance.delete(`/tickets/${ticketId}`);
  }
}

// Sử dụng trong React component
export default function TicketList() {
  const [tickets, setTickets] = React.useState([]);
  const ticketService = new TicketService();
  
  React.useEffect(() => {
    loadTickets();
  }, []);
  
  async function loadTickets() {
    try {
      const data = await ticketService.getTickets({
        pageNumber: 1,
        pageSize: 20,
        status: 'Open'
      });
      setTickets(data.data.items);
    } catch (error) {
      console.error('Failed to load tickets:', error);
    }
  }
  
  async function handleCreateTicket() {
    try {
      const newTicket = await ticketService.createTicket({
        subject: 'Customer needs help with billing',
        description: 'Invoice not received',
        priority: 2, // High
        type: 0, // Question
        channel: 'Email'
      });
      
      alert(`Ticket created: ${newTicket.ticketNumber}`);
      loadTickets(); // Refresh list
    } catch (error) {
      console.error('Failed to create ticket:', error);
    }
  }
  
  return (
    <div>
      <button onClick={handleCreateTicket}>Create Ticket</button>
      <ul>
        {tickets.map(ticket => (
          <li key={ticket.id}>
            {ticket.ticketNumber} - {ticket.subject} ({ticket.status})
          </li>
        ))}
      </ul>
    </div>
  );
}
```

---

#### B. C# (.NET Client)

```csharp
// TicketApiClient.cs
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class TicketApiClient
{
    private readonly HttpClient _httpClient;
    private string _accessToken;
    
    public TicketApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }
    
    // Authentication
    public async Task<string> LoginAsync(string email, string password)
    {
        var loginRequest = new { email, password };
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResult>>();
        _accessToken = result.Data.AccessToken;
        
        // Set default authorization header
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        
        return _accessToken;
    }
    
    // Get all tickets
    public async Task<PagedResult<TicketDto>> GetTicketsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string status = null,
        string priority = null,
        string search = null)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };
        
        if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
        if (!string.IsNullOrEmpty(priority)) queryParams.Add($"priority={priority}");
        if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={search}");
        
        var queryString = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"/api/v1/tickets?{queryString}");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TicketDto>>>();
        return result.Data;
    }
    
    // Get my tickets
    public async Task<List<TicketDto>> GetMyTicketsAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/tickets/my-tickets");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TicketDto>>>();
        return result.Data;
    }
    
    // Get overdue tickets
    public async Task<List<TicketDto>> GetOverdueTicketsAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/tickets/overdue");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TicketDto>>>();
        return result.Data;
    }
    
    // Create ticket
    public async Task<TicketDto> CreateTicketAsync(CreateTicketRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/tickets", request);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDto>>();
        return result.Data;
    }
    
    // Get ticket by ID
    public async Task<TicketDetailDto> GetTicketByIdAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"/api/v1/tickets/{id}");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>();
        return result.Data;
    }
    
    // Update ticket
    public async Task UpdateTicketAsync(Guid id, UpdateTicketRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/tickets/{id}", request);
        response.EnsureSuccessStatusCode();
    }
    
    // Assign ticket
    public async Task AssignTicketAsync(Guid ticketId, Guid userId)
    {
        var request = new { userId };
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/tickets/{ticketId}/assign", request);
        response.EnsureSuccessStatusCode();
    }
    
    // Resolve ticket
    public async Task ResolveTicketAsync(Guid ticketId)
    {
        var response = await _httpClient.PostAsync($"/api/v1/tickets/{ticketId}/resolve", null);
        response.EnsureSuccessStatusCode();
    }
    
    // Close ticket
    public async Task CloseTicketAsync(Guid ticketId)
    {
        var response = await _httpClient.PostAsync($"/api/v1/tickets/{ticketId}/close", null);
        response.EnsureSuccessStatusCode();
    }
    
    // Pause SLA
    public async Task PauseSLAAsync(Guid ticketId, string reason)
    {
        var request = new { reason };
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/tickets/{ticketId}/pause-sla", request);
        response.EnsureSuccessStatusCode();
    }
    
    // Resume SLA
    public async Task ResumeSLAAsync(Guid ticketId)
    {
        var response = await _httpClient.PostAsync($"/api/v1/tickets/{ticketId}/resume-sla", null);
        response.EnsureSuccessStatusCode();
    }
    
    // Escalate ticket
    public async Task<EscalationResult> EscalateTicketAsync(Guid ticketId)
    {
        var response = await _httpClient.PostAsync($"/api/v1/tickets/{ticketId}/escalate", null);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EscalationResult>>();
        return result.Data;
    }
    
    // Delete ticket
    public async Task DeleteTicketAsync(Guid ticketId)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/tickets/{ticketId}");
        response.EnsureSuccessStatusCode();
    }
}

// Usage Example
public class Program
{
    public static async Task Main(string[] args)
    {
        var client = new TicketApiClient("https://api.yourcrm.com");
        
        // Login
        await client.LoginAsync("support@company.com", "your-password");
        
        // Get my tickets
        var myTickets = await client.GetMyTicketsAsync();
        Console.WriteLine($"I have {myTickets.Count} tickets assigned to me");
        
        // Create new ticket
        var newTicket = await client.CreateTicketAsync(new CreateTicketRequest
        {
            Subject = "Customer billing inquiry",
            Description = "Customer asking about invoice #12345",
            Priority = TicketPriority.Medium,
            Type = TicketType.Question,
            Channel = "Email"
        });
        
        Console.WriteLine($"Created ticket: {newTicket.TicketNumber}");
        
        // Get overdue tickets
        var overdueTickets = await client.GetOverdueTicketsAsync();
        if (overdueTickets.Any())
        {
            Console.WriteLine($"WARNING: {overdueTickets.Count} overdue tickets!");
            foreach (var ticket in overdueTickets)
            {
                Console.WriteLine($"  - {ticket.TicketNumber}: {ticket.Subject}");
            }
        }
        
        // Resolve a ticket
        await client.ResolveTicketAsync(newTicket.Id);
        Console.WriteLine($"Ticket {newTicket.TicketNumber} resolved");
    }
}

// DTOs
public class CreateTicketRequest
{
    public string Subject { get; set; }
    public string Description { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ContactId { get; set; }
    public TicketType Type { get; set; }
    public TicketPriority Priority { get; set; }
    public string Channel { get; set; }
    public Guid? SlaId { get; set; }
    public Guid? AssignedToUserId { get; set; }
}

public enum TicketPriority { Low = 0, Medium = 1, High = 2, Critical = 3 }
public enum TicketType { Question = 0, Problem = 1, Incident = 2, FeatureRequest = 3, Task = 4 }
```

---

#### C. Python Client

```python
# ticket_client.py
import requests
from typing import Optional, List, Dict
from datetime import datetime

class TicketAPIClient:
    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip('/')
        self.session = requests.Session()
        self.access_token = None
    
    def login(self, email: str, password: str) -> str:
        """Authenticate and get access token"""
        response = self.session.post(
            f"{self.base_url}/api/v1/auth/login",
            json={"email": email, "password": password}
        )
        response.raise_for_status()
        
        data = response.json()
        self.access_token = data['data']['accessToken']
        
        # Set authorization header for all future requests
        self.session.headers.update({
            'Authorization': f'Bearer {self.access_token}'
        })
        
        return self.access_token
    
    def get_tickets(
        self,
        page_number: int = 1,
        page_size: int = 10,
        status: Optional[str] = None,
        priority: Optional[str] = None,
        search: Optional[str] = None
    ) -> Dict:
        """Get list of tickets with pagination and filters"""
        params = {
            'pageNumber': page_number,
            'pageSize': page_size
        }
        
        if status:
            params['status'] = status
        if priority:
            params['priority'] = priority
        if search:
            params['search'] = search
        
        response = self.session.get(
            f"{self.base_url}/api/v1/tickets",
            params=params
        )
        response.raise_for_status()
        return response.json()['data']
    
    def get_my_tickets(self) -> List[Dict]:
        """Get tickets assigned to me"""
        response = self.session.get(f"{self.base_url}/api/v1/tickets/my-tickets")
        response.raise_for_status()
        return response.json()['data']
    
    def get_overdue_tickets(self) -> List[Dict]:
        """Get overdue tickets (SLA breached)"""
        response = self.session.get(f"{self.base_url}/api/v1/tickets/overdue")
        response.raise_for_status()
        return response.json()['data']
    
    def create_ticket(
        self,
        subject: str,
        description: Optional[str] = None,
        customer_id: Optional[str] = None,
        contact_id: Optional[str] = None,
        ticket_type: int = 0,  # Question
        priority: int = 1,     # Medium
        channel: str = "Web"
    ) -> Dict:
        """Create new ticket"""
        payload = {
            "subject": subject,
            "description": description,
            "customerId": customer_id,
            "contactId": contact_id,
            "type": ticket_type,
            "priority": priority,
            "channel": channel
        }
        
        # Remove None values
        payload = {k: v for k, v in payload.items() if v is not None}
        
        response = self.session.post(
            f"{self.base_url}/api/v1/tickets",
            json=payload
        )
        response.raise_for_status()
        return response.json()['data']
    
    def get_ticket_by_id(self, ticket_id: str) -> Dict:
        """Get ticket details"""
        response = self.session.get(f"{self.base_url}/api/v1/tickets/{ticket_id}")
        response.raise_for_status()
        return response.json()['data']
    
    def update_ticket(
        self,
        ticket_id: str,
        subject: Optional[str] = None,
        description: Optional[str] = None,
        priority: Optional[int] = None,
        ticket_type: Optional[int] = None
    ):
        """Update ticket information"""
        payload = {}
        if subject:
            payload['subject'] = subject
        if description:
            payload['description'] = description
        if priority is not None:
            payload['priority'] = priority
        if ticket_type is not None:
            payload['type'] = ticket_type
        
        response = self.session.put(
            f"{self.base_url}/api/v1/tickets/{ticket_id}",
            json=payload
        )
        response.raise_for_status()
    
    def assign_ticket(self, ticket_id: str, user_id: str):
        """Assign ticket to user"""
        response = self.session.post(
            f"{self.base_url}/api/v1/tickets/{ticket_id}/assign",
            json={"userId": user_id}
        )
        response.raise_for_status()
    
    def resolve_ticket(self, ticket_id: str):
        """Mark ticket as resolved"""
        response = self.session.post(
            f"{self.base_url}/api/v1/tickets/{ticket_id}/resolve"
        )
        response.raise_for_status()
    
    def close_ticket(self, ticket_id: str):
        """Close ticket"""
        response = self.session.post(
            f"{self.base_url}/api/v1/tickets/{ticket_id}/close"
        )
        response.raise_for_status()
    
    def pause_sla(self, ticket_id: str, reason: str):
        """Pause SLA tracking"""
        response = self.session.post(
            f"{self.base_url}/api/v1/tickets/{ticket_id}/pause-sla",
            json={"reason": reason}
        )
        response.raise_for_status()
    
    def resume_sla(self, ticket_id: str) -> Dict:
        """Resume SLA tracking"""
        response = self.session.post(
            f"{self.base_url}/api/v1/tickets/{ticket_id}/resume-sla"
        )
        response.raise_for_status()
        return response.json()['data']
    
    def escalate_ticket(self, ticket_id: str) -> Dict:
        """Escalate ticket to higher priority"""
        response = self.session.post(
            f"{self.base_url}/api/v1/tickets/{ticket_id}/escalate"
        )
        response.raise_for_status()
        return response.json()['data']
    
    def delete_ticket(self, ticket_id: str):
        """Delete ticket (soft delete)"""
        response = self.session.delete(
            f"{self.base_url}/api/v1/tickets/{ticket_id}"
        )
        response.raise_for_status()


# Usage Example
if __name__ == "__main__":
    # Initialize client
    client = TicketAPIClient("https://api.yourcrm.com")
    
    # Login
    client.login("support@company.com", "your-password")
    print("✓ Logged in successfully")
    
    # Get my tickets
    my_tickets = client.get_my_tickets()
    print(f"✓ I have {len(my_tickets)} tickets assigned to me")
    
    for ticket in my_tickets[:5]:  # Show first 5
        print(f"  - {ticket['ticketNumber']}: {ticket['subject']} ({ticket['status']})")
    
    # Create new ticket
    new_ticket = client.create_ticket(
        subject="Customer cannot access dashboard",
        description="Customer reports 404 error when accessing dashboard page",
        priority=2,  # High
        ticket_type=1,  # Problem
        channel="Email"
    )
    print(f"✓ Created ticket: {new_ticket['ticketNumber']}")
    
    # Check for overdue tickets
    overdue = client.get_overdue_tickets()
    if overdue:
        print(f"⚠ WARNING: {len(overdue)} overdue tickets!")
        for ticket in overdue:
            print(f"  - {ticket['ticketNumber']}: {ticket['subject']}")
            # Auto-escalate
            client.escalate_ticket(ticket['id'])
            print(f"    → Escalated")
    
    # Pause SLA (waiting for customer)
    client.pause_sla(
        new_ticket['id'],
        reason="Waiting for customer to provide error logs"
    )
    print(f"✓ Paused SLA for ticket {new_ticket['ticketNumber']}")
    
    # Get all open tickets
    tickets = client.get_tickets(
        page_number=1,
        page_size=20,
        status="Open",
        priority="High"
    )
    print(f"✓ Found {tickets['totalCount']} open high-priority tickets")
```

---

### 3. Webhook Integration (Real-time Updates)

**Setup Webhook để nhận real-time notifications khi có ticket event:**

```javascript
// Node.js Express webhook receiver
const express = require('express');
const app = express();

app.use(express.json());

// Webhook endpoint
app.post('/webhooks/tickets', (req, res) => {
  const event = req.body;
  
  console.log(`Received ticket event: ${event.eventType}`);
  
  switch (event.eventType) {
    case 'ticket.created':
      handleTicketCreated(event.data);
      break;
    
    case 'ticket.assigned':
      handleTicketAssigned(event.data);
      break;
    
    case 'ticket.updated':
      handleTicketUpdated(event.data);
      break;
    
    case 'ticket.resolved':
      handleTicketResolved(event.data);
      break;
    
    case 'ticket.closed':
      handleTicketClosed(event.data);
      break;
    
    case 'ticket.sla_breach':
      handleSLABreach(event.data);
      break;
    
    case 'ticket.escalated':
      handleTicketEscalated(event.data);
      break;
  }
  
  // Acknowledge receipt
  res.status(200).json({ received: true });
});

function handleTicketCreated(ticket) {
  console.log(`New ticket created: ${ticket.ticketNumber} - ${ticket.subject}`);
  
  // Send notification to Slack
  sendToSlack({
    text: `🎫 New Ticket: *${ticket.ticketNumber}*`,
    attachments: [{
      color: '#36a64f',
      fields: [
        { title: 'Subject', value: ticket.subject, short: false },
        { title: 'Priority', value: ticket.priority, short: true },
        { title: 'Customer', value: ticket.customerName, short: true }
      ]
    }]
  });
}

function handleTicketAssigned(ticket) {
  console.log(`Ticket ${ticket.ticketNumber} assigned to ${ticket.assignedToUserName}`);
  
  // Send email to assigned agent
  sendEmail({
    to: ticket.assignedToUserEmail,
    subject: `Ticket ${ticket.ticketNumber} assigned to you`,
    body: `You have been assigned ticket: ${ticket.subject}`
  });
}

function handleSLABreach(ticket) {
  console.log(`⚠️ SLA BREACH: ${ticket.ticketNumber}`);
  
  // Alert via PagerDuty
  triggerPagerDutyAlert({
    summary: `SLA Breach: Ticket ${ticket.ticketNumber}`,
    severity: 'critical',
    source: 'CRM Ticket System'
  });
  
  // Send urgent Slack notification
  sendToSlack({
    text: `🚨 *SLA BREACH ALERT*`,
    attachments: [{
      color: 'danger',
      fields: [
        { title: 'Ticket', value: ticket.ticketNumber, short: true },
        { title: 'Priority', value: ticket.priority, short: true },
        { title: 'Subject', value: ticket.subject, short: false },
        { title: 'Assigned To', value: ticket.assignedToUserName, short: true }
      ]
    }]
  });
}

app.listen(3000, () => {
  console.log('Webhook receiver running on port 3000');
});
```

**Đăng ký Webhook trong CRM:**

```bash
curl -X POST "https://api.yourcrm.com/api/v1/webhooks" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://your-server.com/webhooks/tickets",
    "events": [
      "ticket.created",
      "ticket.assigned",
      "ticket.updated",
      "ticket.resolved",
      "ticket.closed",
      "ticket.sla_breach",
      "ticket.escalated"
    ],
    "isActive": true
  }'
```

---

### 4. Error Handling Best Practices

```typescript
// Error handling wrapper
async function apiCall<T>(
  apiFunc: () => Promise<T>,
  errorMessage: string
): Promise<T> {
  try {
    return await apiFunc();
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const status = error.response?.status;
      const message = error.response?.data?.message || error.message;
      
      switch (status) {
        case 401:
          // Token expired, refresh and retry
          await refreshToken();
          return await apiFunc(); // Retry
        
        case 403:
          throw new Error('Permission denied: ' + message);
        
        case 404:
          throw new Error('Ticket not found');
        
        case 429:
          // Rate limited, wait and retry
          await delay(5000);
          return await apiFunc();
        
        case 500:
          throw new Error('Server error: ' + message);
        
        default:
          throw new Error(`${errorMessage}: ${message}`);
      }
    }
    
    throw error;
  }
}

// Usage
const ticket = await apiCall(
  () => ticketService.getTicketById(ticketId),
  'Failed to load ticket'
);
```

---

### 5. Rate Limiting & Batch Operations

```typescript
// Batch create tickets (avoid rate limits)
async function batchCreateTickets(tickets: CreateTicketRequest[]) {
  const batchSize = 10;
  const delay = 1000; // 1 second between batches
  
  const results = [];
  
  for (let i = 0; i < tickets.length; i += batchSize) {
    const batch = tickets.slice(i, i + batchSize);
    
    // Process batch in parallel
    const batchResults = await Promise.all(
      batch.map(ticket => ticketService.createTicket(ticket))
    );
    
    results.push(...batchResults);
    
    // Wait before next batch (rate limiting)
    if (i + batchSize < tickets.length) {
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }
  
  return results;
}

// Usage
const ticketsToCreate = [
  { subject: 'Issue 1', priority: 1 },
  { subject: 'Issue 2', priority: 2 },
  // ... 100 tickets
];

const created = await batchCreateTickets(ticketsToCreate);
console.log(`Created ${created.length} tickets`);
```

---

### 6. Real-time Dashboard with Polling

```typescript
// Ticket dashboard with auto-refresh
class TicketDashboard {
  private ticketService: TicketService;
  private refreshInterval: number = 30000; // 30 seconds
  private intervalId?: NodeJS.Timer;
  
  async start() {
    // Initial load
    await this.refresh();
    
    // Auto-refresh
    this.intervalId = setInterval(async () => {
      await this.refresh();
    }, this.refreshInterval);
  }
  
  stop() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }
  
  private async refresh() {
    try {
      // Get stats
      const [myTickets, overdue, openCount] = await Promise.all([
        this.ticketService.getMyTickets(),
        this.ticketService.getOverdueTickets(),
        this.getOpenTicketCount()
      ]);
      
      // Update UI
      this.updateDashboard({
        myTicketsCount: myTickets.length,
        overdueCount: overdue.length,
        openCount: openCount
      });
      
      // Alert if overdue
      if (overdue.length > 0) {
        this.showAlert(`You have ${overdue.length} overdue tickets!`);
      }
    } catch (error) {
      console.error('Dashboard refresh failed:', error);
    }
  }
  
  private async getOpenTicketCount(): Promise<number> {
    const result = await this.ticketService.getTickets({
      pageNumber: 1,
      pageSize: 1,
      status: 'Open'
    });
    return result.totalCount;
  }
}

// Usage
const dashboard = new TicketDashboard();
dashboard.start(); // Auto-refresh every 30 seconds
```

---

## Best Practices

### 1. Luôn có SLA cho mọi ticket

- Set default SLA cho tất cả tickets
- Custom SLA cho VIP customers
- Monitor SLA breach daily

### 2. Phản hồi nhanh First Response

- First response trong 1 giờ (high priority)
- Ngay cả khi chưa có solution, confirm đã nhận ticket

### 3. Update status thường xuyên

- Không để ticket ở status "Open" quá lâu
- Update sang "InProgress" khi bắt đầu xử lý
- Pending khi chờ customer

### 4. Pause SLA đúng lúc

- Pause khi chờ customer/vendor
- Document reason rõ ràng
- Resume ngay khi nhận được info

### 5. Escalate kịp thời

- Không giữ ticket quá khả năng
- Escalate khi SLA sắp breach
- Communicate với customer về escalation

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
