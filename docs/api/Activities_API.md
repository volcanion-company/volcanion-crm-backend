# Activities API Documentation

## Tổng quan Module

### Activity là gì?

**Activity (Hoạt động)** là bất kỳ task, cuộc gọi, meeting, hoặc hành động nào cần thực hiện để tương tác với khách hàng, xử lý lead, close deal, hoặc support ticket. Activity là xương sống của CRM - giúp track mọi công việc cần làm.

### Tại sao Activity quan trọng?

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    ACTIVITIES = CRM HEARTBEAT                                │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Without Activities:                  With Activities:                       │
│  ─────────────────────                ──────────────────                     │
│  ❌ "Ai gọi customer này?"           ✅ Activity log: John called on 1/15   │
│  ❌ "Lead này follow-up chưa?"       ✅ Auto reminder: Follow-up today      │
│  ❌ "Meeting với ai lúc mấy giờ?"    ✅ Calendar sync: Meeting 2PM today    │
│  ❌ "Ticket này ai đang handle?"     ✅ Task assigned to Agent A            │
│  ❌ "Quên call customer"             ✅ Reminder sent 15 mins before        │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Activity trong CRM Workflow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     ACTIVITIES ACROSS CRM MODULES                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────┐    ┌────────────┐    ┌────────┐    ┌────────────┐                 │
│  │ LEAD │───►│  Activity  │───►│CUSTOMER│───►│ Activity   │                 │
│  │      │    │ "Call Lead"│    │        │    │ "Meeting"  │                 │
│  └──────┘    └────────────┘    └────────┘    └────────────┘                 │
│     │              │                 │               │                      │
│     │              ▼                 │               ▼                      │
│     │        ┌──────────┐            │         ┌──────────┐                 │
│     │        │Follow-up │            │         │ Demo     │                 │
│     │        │Task      │            │         │ Activity │                 │
│     │        └──────────┘            │         └──────────┘                 │
│     │                                │                                      │
│     ▼                                ▼                                      │
│  ┌────────────┐                ┌──────────────┐                             │
│  │OPPORTUNITY │───────────────►│  Activities  │                             │
│  │            │                │  Timeline    │                             │
│  │"Call to    │                │  - Call      │                             │
│  │ close deal"│                │  - Meeting   │                             │
│  └────────────┘                │  - Email     │                             │
│       │                        │  - Follow-up │                             │
│       │                        └──────────────┘                             │
│       ▼                                                                     │
│  ┌────────┐    ┌────────────┐                                               │
│  │ TICKET │───►│  Activity  │                                               │
│  │        │    │"Follow-up" │                                               │
│  └────────┘    └────────────┘                                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Activity Types

### Phân loại theo tính chất

| Type | Mô tả | Duration | Ví dụ |
|------|-------|----------|-------|
| **Task** | Công việc cần làm | Variable | "Gửi proposal cho ABC Corp" |
| **Call** | Cuộc gọi | 5-30 mins | "Call customer về pricing" |
| **Meeting** | Cuộc họp | 30-120 mins | "Demo meeting với client" |
| **Email** | Email cần gửi | - | "Send follow-up email" |
| **FollowUp** | Follow-up sau event | - | "Follow-up sau meeting" |
| **Demo** | Demonstration | 30-60 mins | "Product demo for prospect" |
| **Other** | Khác | Variable | - |

### Real-world Examples

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    TYPICAL SALES REP'S DAY                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  09:00 AM - Meeting with ABC Corp (Type: Meeting)                           │
│             Status: Completed                                               │
│             Duration: 60 minutes                                            │
│             Outcome: Interested, send proposal                              │
│                                                                             │
│  10:30 AM - Call Lead: XYZ Company (Type: Call)                             │
│             Status: In Progress                                             │
│             Priority: High                                                  │
│             Note: Discuss pricing                                           │
│                                                                             │
│  02:00 PM - Product Demo: DEF Corp (Type: Demo)                             │
│             Status: Not Started                                             │
│             Duration: 45 minutes                                            │
│             Reminder: 15 mins before                                        │
│                                                                             │
│  03:30 PM - Follow-up Email to GHI Ltd (Type: Email)                        │
│             Status: Not Started                                             │
│             Priority: Medium                                                │
│             Due: Today EOD                                                  │
│                                                                             │
│  05:00 PM - Task: Update opportunity pipeline (Type: Task)                  │
│             Status: Not Started                                             │
│             Priority: Low                                                   │
│             Due: Today                                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Activity Status Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ACTIVITY STATUS LIFECYCLE                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌───────────┐                        │
│  │ NOT STARTED │───►│ IN PROGRESS │───►│ COMPLETED │                        │
│  └──────┬──────┘    └──────┬──────┘    └───────────┘                        │
│         │                  │                                                │
│         │                  │                                                │
│         │                  ▼                                                │
│         │            ┌──────────┐                                           │
│         │            │ WAITING  │                                           │
│         │            │(for info)│                                           │
│         │            └────┬─────┘                                           │
│         │                 │                                                 │
│         │                 ▼                                                 │
│         │            ┌──────────┐                                           │
│         │            │ DEFERRED │                                           │
│         │            │(postpone)│                                           │
│         │            └──────────┘                                           │
│         │                                                                   │
│         ▼                                                                   │
│    ┌───────────┐                                                            │
│    │ CANCELLED │                                                            │
│    └───────────┘                                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Chi tiết từng Status

| Status | Ý nghĩa | Action | Color Code |
|--------|---------|--------|------------|
| **NotStarted** | Chưa bắt đầu | Start hoặc Reschedule | 🔵 Blue |
| **InProgress** | Đang làm | Complete hoặc Defer | 🟡 Yellow |
| **Completed** | Hoàn thành | Archive | 🟢 Green |
| **Waiting** | Chờ thông tin | Continue khi có info | 🟠 Orange |
| **Deferred** | Hoãn lại | Reschedule | 🟣 Purple |
| **Cancelled** | Hủy bỏ | - | 🔴 Red |

---

## Priority Levels

### Phân loại độ ưu tiên

| Priority | Khi nào sử dụng | SLA | Ví dụ |
|----------|-----------------|-----|-------|
| **Urgent** | Cần làm ngay, không trì hoãn | Same day | "Call VIP customer về complaint" |
| **High** | Quan trọng, làm trong 1-2 ngày | 1-2 days | "Follow-up hot lead" |
| **Medium** | Bình thường | 3-5 days | "Send proposal" |
| **Low** | Không gấp | This week | "Update CRM data" |

### Priority Matrix

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       PRIORITY vs URGENCY MATRIX                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Important    ┌──────────────────┬──────────────────┐                       │
│       ▲       │                  │                  │                       │
│       │       │   DO FIRST       │   DO TODAY       │                       │
│       │       │                  │                  │                       │
│       │       │ Priority: HIGH   │ Priority: URGENT │                       │
│       │       │ "Close hot deal" │ "VIP complaint"  │                       │
│       │       │                  │                  │                       │
│       │       ├──────────────────┼──────────────────┤                       │
│       │       │                  │                  │                       │
│       │       │   SCHEDULE       │   DELEGATE       │                       │
│       │       │                  │                  │                       │
│       │       │ Priority: MEDIUM │ Priority: LOW    │                       │
│       │       │ "Send proposal"  │ "Update data"    │                       │
│       │       │                  │                  │                       │
│               └──────────────────┴──────────────────┘                       │
│                                                                             │
│               ────────────────────────────────►                             │
│                         Urgent                                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Recurring Activities

### Cơ chế Recurrence

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     RECURRING ACTIVITY PATTERNS                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Pattern: Weekly                                                            │
│  Activity: "Sales Team Meeting"                                             │
│  Interval: Every Monday at 10:00 AM                                         │
│  End Date: December 31, 2026                                                │
│                                                                             │
│  Generated Activities:                                                      │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Jan 20 (Mon) - Sales Team Meeting [Auto-created]                           │
│  Jan 27 (Mon) - Sales Team Meeting [Auto-created]                           │
│  Feb 03 (Mon) - Sales Team Meeting [Auto-created]                           │
│  ...                                                                        │
│  Dec 28 (Mon) - Sales Team Meeting [Auto-created]                           │
│                                                                             │
│  ═══════════════════════════════════════════════════════════════════════    │
│                                                                             │
│  Pattern: Monthly                                                           │
│  Activity: "Customer Health Check Call"                                     │
│  Interval: 1st day of every month                                           │
│  Customer: ABC Corp                                                         │
│                                                                             │
│  Generated Activities:                                                      │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Feb 01 - Health Check Call - ABC Corp                                      │
│  Mar 01 - Health Check Call - ABC Corp                                      │
│  Apr 01 - Health Check Call - ABC Corp                                      │
│  ...                                                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Recurrence Patterns

| Pattern | Description | Use Case |
|---------|-------------|----------|
| **Daily** | Hàng ngày | "Daily standup meeting" |
| **Weekly** | Hàng tuần | "Weekly sales review" |
| **Monthly** | Hàng tháng | "Monthly customer check-in" |
| **Yearly** | Hàng năm | "Annual contract renewal call" |

---

## Reminders

### Reminder System

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ACTIVITY REMINDER FLOW                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Activity: "Meeting with ABC Corp"                                          │
│  Due Date: Jan 18, 2026 at 2:00 PM                                          │
│                                                                             │
│  Reminders:                                                                 │
│  ─────────────────────────────────────────────────────────────────────────  │
│  1. Email:  Jan 17 (1 day before) - "Meeting tomorrow"                      │
│     Status: Sent                                                            │
│                                                                             │
│  2. In-App: Jan 18 at 1:45 PM (15 mins before) - "Meeting in 15 mins"       │
│     Status: Pending                                                         │
│                                                                             │
│  3. Push:   Jan 18 at 1:55 PM (5 mins before) - "Meeting starting soon"     │
│     Status: Pending                                                         │
│                                                                             │
│  ═══════════════════════════════════════════════════════════════════════    │
│                                                                             │
│  Reminder Methods:                                                          │
│  - In-App Notification: Bell icon in app                                    │
│  - Email: Sent to assigned user's email                                     │
│  - SMS: Text message (if configured)                                        │
│  - Push: Mobile push notification                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Best Practices cho Reminders

| Activity Type | Suggested Reminders |
|---------------|---------------------|
| **Meeting** | 1 day before, 15 mins before |
| **Call** | 30 mins before, 5 mins before |
| **Demo** | 1 day before, 30 mins before |
| **Task** | On due date morning (9 AM) |
| **Follow-up** | On due date |

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Activity (Create Activity)

**Nghiệp vụ thực tế:**
- Sales Rep tạo task follow-up lead
- Manager schedule meeting với customer
- Support assign task xử lý ticket
- System auto-create activity từ workflow

**Ví dụ thực tế:**
> Sales Rep gọi lead ABC Corp:
> - Lead interested → Tạo activity:
>   * Subject: "Send proposal to ABC Corp"
>   * Type: Task
>   * Priority: High
>   * Due Date: Tomorrow (Jan 19, 2026)
>   * Assigned To: Me
>   * Link to: Lead ABC Corp
>   * Reminder: Tomorrow 9 AM
> - Activity created → Reminder scheduled
> - Tomorrow 9 AM → Notification: "Task due: Send proposal"

---

### 2. My Activities (Get My Activities)

**Nghiệp vụ thực tế:**
- User login → Xem tasks của mình
- Personal task list
- Focus vào công việc assigned

**Ví dụ thực tế:**
> Sales Rep login hệ thống mỗi sáng:
> - "My Activities" tab:
>   * 3 Urgent tasks (call VIP customers)
>   * 5 High priority (follow-up hot leads)
>   * 8 Medium priority (send emails)
>   * 2 Overdue tasks (missed yesterday)
> - Sort by Priority desc
> - Start with Urgent tasks first

---

### 3. Today's Activities (Get Today Activities)

**Nghiệp vụ thực tế:**
- Daily schedule
- Focus vào activities due today
- Plan workday

**Ví dụ thực tế:**
> Sales Rep check "Today" tab:
> - 09:00: Meeting với ABC Corp
> - 10:30: Call Lead XYZ
> - 14:00: Demo cho DEF Corp
> - 17:00: Send proposal to GHI Ltd
> → Có 4 activities today, plan accordingly

---

### 4. Upcoming Activities (Get Upcoming)

**Nghiệp vụ thực tế:**
- Xem activities trong 7 ngày tới
- Plan ahead
- Reschedule nếu conflict

**Ví dụ thực tế:**
> Manager check "Upcoming (7 days)":
> - Monday (Jan 20): 5 activities
> - Tuesday (Jan 21): 3 activities
> - Wednesday (Jan 22): 8 activities (busy day!)
> - Thursday (Jan 23): 2 activities
> - Friday (Jan 24): 4 activities
> → Wednesday quá tải, reschedule 2 tasks sang Thursday

---

### 5. Xem chi tiết Activity (Get Activity by ID)

**Nghiệp vụ thực tế:**
- Click vào activity để xem full details
- Check reminders, related entities (lead, opportunity, ticket)
- See completion status

**Ví dụ thực tế:**
> Click activity "Meeting with ABC Corp":
> - Subject: "Discuss pricing and close deal"
> - Type: Meeting
> - Status: Completed ✓
> - Priority: High
> - Customer: ABC Corporation
> - Opportunity: "$50K CRM Deal"
> - Duration: 60 minutes
> - Start: 2:00 PM
> - Completed: 3:00 PM
> - Notes: "Customer agreed, send contract"
> - Next Steps: Create follow-up task "Send contract"

---

### 6. Cập nhật Activity (Update Activity)

**Nghiệp vụ thực tế:**
- Change priority: Medium → High
- Reschedule due date
- Reassign to another user
- Update description

**Ví dụ thực tế:**
> Activity "Call Lead XYZ" priority Medium:
> - Lead responded: Very interested, want demo ASAP
> - Update:
>   * Priority: Medium → Urgent
>   * Due Date: Today → Now (ASAP)
>   * Add note: "Hot lead, need demo today"
> → Activity updated, move to top of list

---

### 7. Start Activity (Start Activity)

**Nghiệp vụ thực tế:**
- Click "Start" khi bắt đầu làm task
- Track time spent
- Update status: NotStarted → InProgress

**Ví dụ thực tế:**
> Activity "Call Lead ABC Corp":
> - 10:00 AM: Click "Start Activity"
> - Status: NotStarted → InProgress
> - Timer starts
> - Make phone call
> → System tracks: Activity in progress

---

### 8. Complete Activity (Complete Activity)

**Nghiệp vụ thực tế:**
- Mark task done
- Trigger: Auto-create follow-up activities
- Update related entities

**Ví dụ thực tế:**
> Activity "Demo for DEF Corp" completed:
> - Click "Complete"
> - Status: InProgress → Completed
> - CompletedDate: Jan 18, 2026 3:00 PM
> - Add outcome: "Customer interested, send proposal"
> - System auto:
>   * Update Opportunity stage: Demo → Proposal
>   * Create follow-up task: "Send proposal to DEF Corp"
>   * Send notification to manager: "Demo completed successfully"

---

### 9. Cancel Activity (Cancel Activity)

**Nghiệp vụ thực tế:**
- Customer cancel meeting
- Task không cần làm nữa
- Delete reminder

**Ví dụ thực tế:**
> Meeting "Demo for GHI Ltd" scheduled 2 PM:
> - Customer email: "Sorry, need to reschedule"
> - Click "Cancel Activity"
> - Status: InProgress → Cancelled
> - Delete reminders
> - Create new activity: "Reschedule demo with GHI Ltd"

---

### 10. Reschedule Activity (Reschedule Activity)

**Nghiệp vụ thực tế:**
- Change meeting time
- Postpone task
- Update due date

**Ví dụ thực tế:**
> Meeting scheduled Jan 18 at 2 PM:
> - Customer request: "Can we do 3 PM instead?"
> - Click "Reschedule"
> - New StartDate: Jan 18 at 3 PM
> - Update reminders: 15 mins before → 2:45 PM
> - Send update notification to attendees

---

## Tích hợp với các module khác

```
                        ┌─────────────┐
                        │  ACTIVITY   │
                        │ (Hoạt động) │
                        └──────┬──────┘
                               │
      ┌────────────────────────┼────────────────────────┐
      │         │         │         │         │         │
      ▼         ▼         ▼         ▼         ▼         ▼
┌──────────┐ ┌────┐ ┌────────────┐ ┌──────┐ ┌────────┐ ┌────────┐
│ CUSTOMER │ │LEAD│ │OPPORTUNITY │ │TICKET│ │CONTACT │ │  USER  │
│          │ │    │ │            │ │      │ │        │ │        │
│ Context  │ │For │ │   For      │ │ For  │ │  For   │ │Assigned│
└──────────┘ └────┘ └────────────┘ └──────┘ └────────┘ └────────┘
      │                    │
      ▼                    ▼
┌──────────┐        ┌──────────┐
│REMINDERS │        │ CALENDAR │
│          │        │          │
│ Notify   │        │ Schedule │
└──────────┘        └──────────┘
```

| Module | Quan hệ | Mô tả |
|--------|---------|-------|
| **Customer** | Activity for Customer | Track tất cả activities với customer |
| **Lead** | Activity for Lead | Follow-up, nurture lead |
| **Opportunity** | Activity for Opportunity | Close deal activities |
| **Ticket** | Activity for Ticket | Support follow-up tasks |
| **Contact** | Activity for Contact | Personal interactions |
| **User** | Activity assigned to User | Personal task list |
| **Calendar** | Activity sync to Calendar | Schedule meetings |
| **Reminder** | Activity has Reminders | Notification system |

---

## Best Practices

### 1. Always set Due Date

- Every activity must have due date
- Avoid "someday" tasks
- Use Priority để xác định urgency

### 2. Use Reminders wisely

- Critical activities: Multiple reminders
- Routine tasks: Single reminder
- Don't over-remind (notification fatigue)

### 3. Complete or Cancel - Don't leave hanging

- Update status promptly
- Don't let tasks "InProgress" forever
- Cancel nếu không cần làm

### 4. Link Activities to related entities

- Always link to Customer/Lead/Opportunity
- Provide context
- Enable full timeline view

### 5. Use Recurring for routine tasks

- Weekly meetings
- Monthly check-ins
- Annual renewals
- → Automate repetitive tasks

---

## Technical Overview

**Base URL:** `/api/v1/activities`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Activities

Lấy danh sách activities với filter.

```
GET /api/v1/activities
```

**Permission Required:** `activity.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Số items mỗi trang |
| `sortBy` | string | No | "DueDate" | Field để sắp xếp |
| `sortDescending` | bool | No | false | Sắp xếp giảm dần |
| `search` | string | No | - | Tìm kiếm theo subject |
| `type` | ActivityType | No | - | Filter theo type |
| `status` | ActivityStatus | No | - | Filter theo status |
| `assignedTo` | Guid | No | - | Filter theo user assigned |
| `customerId` | Guid | No | - | Filter theo customer |
| `startDate` | DateTime | No | - | Filter from date |
| `endDate` | DateTime | No | - | Filter to date |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "subject": "Call ABC Corp about pricing",
        "type": "Call",
        "status": "NotStarted",
        "priority": "High",
        "customerName": "ABC Corporation",
        "assignedToUserName": "John Doe",
        "startDate": "2026-01-18T10:00:00Z",
        "dueDate": "2026-01-18T11:00:00Z",
        "durationMinutes": 30,
        "isOverdue": false,
        "createdAt": "2026-01-17T15:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 48
  }
}
```

---

### 2. Get My Activities

Lấy activities assigned cho user hiện tại.

```
GET /api/v1/activities/my-activities
```

**Permission Required:** `activity.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `status` | ActivityStatus | No | - | Filter theo status |
| `overdue` | bool | No | - | Filter overdue tasks |

---

### 3. Get Today's Activities

Lấy activities due today.

```
GET /api/v1/activities/today
```

**Permission Required:** `activity.view`

---

### 4. Get Upcoming Activities

Lấy activities trong N ngày tới.

```
GET /api/v1/activities/upcoming?days=7
```

**Permission Required:** `activity.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `days` | int | No | 7 | Số ngày tới |

---

### 5. Get Activity by ID

Lấy chi tiết một activity.

```
GET /api/v1/activities/{id}
```

**Permission Required:** `activity.view`

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "subject": "Demo meeting with ABC Corp",
    "description": "Product demo, discuss features and pricing",
    "type": "Demo",
    "status": "NotStarted",
    "priority": "High",
    "customerId": "customer-guid",
    "customerName": "ABC Corporation",
    "contactId": "contact-guid",
    "contactName": "Nguyen Van A",
    "opportunityId": "opportunity-guid",
    "opportunityName": "ABC Corp - CRM Deal",
    "assignedToUserId": "user-guid",
    "assignedToUserName": "John Doe",
    "startDate": "2026-01-18T14:00:00Z",
    "dueDate": "2026-01-18T15:00:00Z",
    "durationMinutes": 60,
    "isOverdue": false,
    "isRecurring": false,
    "tags": "[\"demo\", \"high-value\"]",
    "reminders": [
      {
        "id": "reminder-guid",
        "subject": "Demo in 15 minutes",
        "message": "Meeting with ABC Corp at 2:00 PM",
        "reminderDate": "2026-01-18T13:45:00Z",
        "method": "InApp",
        "isSent": false
      }
    ],
    "createdAt": "2026-01-17T10:00:00Z",
    "updatedAt": "2026-01-17T15:00:00Z"
  }
}
```

---

### 6. Create Activity

Tạo activity mới.

```
POST /api/v1/activities
```

**Permission Required:** `activity.create`

#### Request Body

```json
{
  "subject": "Follow-up call to ABC Corp",
  "description": "Discuss proposal feedback and next steps",
  "type": 1,
  "priority": 2,
  "startDate": "2026-01-19T10:00:00Z",
  "dueDate": "2026-01-19T11:00:00Z",
  "durationMinutes": 30,
  "assignedToUserId": "user-guid",
  "customerId": "customer-guid",
  "opportunityId": "opportunity-guid",
  "isRecurring": false,
  "tags": "[\"follow-up\", \"urgent\"]",
  "reminders": [
    {
      "subject": "Call in 15 minutes",
      "message": "Follow-up call with ABC Corp",
      "reminderDate": "2026-01-19T09:45:00Z",
      "method": 0
    }
  ]
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `subject` | string | **Yes** | Tiêu đề activity |
| `description` | string | No | Mô tả chi tiết |
| `type` | ActivityType | **Yes** | Loại activity |
| `priority` | ActivityPriority | No | Độ ưu tiên (default: Medium) |
| `startDate` | DateTime | No | Thời gian bắt đầu |
| `dueDate` | DateTime | No | Thời gian due |
| `durationMinutes` | int | No | Thời lượng (phút) |
| `assignedToUserId` | Guid | No | User assigned (default: current user) |
| `customerId` | Guid | No | Link to customer |
| `contactId` | Guid | No | Link to contact |
| `leadId` | Guid | No | Link to lead |
| `opportunityId` | Guid | No | Link to opportunity |
| `ticketId` | Guid | No | Link to ticket |
| `isRecurring` | bool | No | Recurring activity |
| `recurrencePattern` | RecurrencePattern | No | Daily/Weekly/Monthly/Yearly |
| `recurrenceInterval` | int | No | Interval (e.g., every 2 weeks) |
| `recurrenceEndDate` | DateTime | No | End date for recurrence |
| `tags` | string | No | Tags (JSON array) |
| `reminders` | array | No | Danh sách reminders |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-activity-guid",
    "subject": "Follow-up call to ABC Corp",
    "type": "Call",
    "status": "NotStarted",
    "priority": "High",
    "dueDate": "2026-01-19T11:00:00Z",
    "createdAt": "2026-01-18T12:00:00Z"
  }
}
```

---

### 7. Update Activity

Cập nhật thông tin activity.

```
PUT /api/v1/activities/{id}
```

**Permission Required:** `activity.update`

#### Request Body (All fields optional)

```json
{
  "subject": "Updated: Follow-up call to ABC Corp",
  "description": "Discuss pricing and timeline",
  "priority": 3,
  "dueDate": "2026-01-19T14:00:00Z",
  "tags": "[\"follow-up\", \"urgent\", \"pricing\"]"
}
```

---

### 8. Start Activity

Bắt đầu activity (NotStarted → InProgress).

```
POST /api/v1/activities/{id}/start
```

**Permission Required:** `activity.update`

---

### 9. Complete Activity

Hoàn thành activity.

```
POST /api/v1/activities/{id}/complete
```

**Permission Required:** `activity.update`

---

### 10. Cancel Activity

Hủy activity.

```
POST /api/v1/activities/{id}/cancel
```

**Permission Required:** `activity.update`

---

### 11. Reschedule Activity

Đổi lịch activity.

```
POST /api/v1/activities/{id}/reschedule
```

**Permission Required:** `activity.update`

#### Request Body

```json
{
  "startDate": "2026-01-20T10:00:00Z",
  "dueDate": "2026-01-20T11:00:00Z"
}
```

---

### 12. Delete Activity

Xóa activity.

```
DELETE /api/v1/activities/{id}
```

**Permission Required:** `activity.delete`

---

## Enums

### ActivityType

| Value | Name | Description |
|-------|------|-------------|
| 0 | Task | Công việc |
| 1 | Call | Cuộc gọi |
| 2 | Meeting | Cuộc họp |
| 3 | Email | Email |
| 4 | FollowUp | Follow-up |
| 5 | Demo | Demonstration |
| 6 | Other | Khác |

### ActivityStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | NotStarted | Chưa bắt đầu |
| 1 | InProgress | Đang làm |
| 2 | Completed | Hoàn thành |
| 3 | Waiting | Chờ |
| 4 | Deferred | Hoãn lại |
| 5 | Cancelled | Hủy bỏ |

### ActivityPriority

| Value | Name | Description |
|-------|------|-------------|
| 0 | Low | Thấp |
| 1 | Medium | Trung bình |
| 2 | High | Cao |
| 3 | Urgent | Khẩn cấp |

### RecurrencePattern

| Value | Name | Description |
|-------|------|-------------|
| 0 | Daily | Hàng ngày |
| 1 | Weekly | Hàng tuần |
| 2 | Monthly | Hàng tháng |
| 3 | Yearly | Hàng năm |

### ReminderMethod

| Value | Name | Description |
|-------|------|-------------|
| 0 | InApp | Trong app |
| 1 | Email | Email |
| 2 | SMS | SMS |
| 3 | Push | Push notification |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `activity.view` | Xem activities |
| `activity.create` | Tạo activity |
| `activity.update` | Cập nhật, start, complete, cancel |
| `activity.delete` | Xóa activity |

---

## Example: Daily Workflow

### Scenario: Sales Rep's typical day

#### Morning: Check today's activities

```bash
curl -X GET "http://localhost:5000/api/v1/activities/today" \
  -H "Authorization: Bearer {token}"
```

#### Create follow-up task

```bash
curl -X POST "http://localhost:5000/api/v1/activities" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "Call ABC Corp",
    "type": 1,
    "priority": 2,
    "dueDate": "2026-01-18T15:00:00Z",
    "customerId": "customer-guid",
    "reminders": [{
      "reminderDate": "2026-01-18T14:45:00Z",
      "method": 0
    }]
  }'
```

#### Start activity

```bash
curl -X POST "http://localhost:5000/api/v1/activities/{id}/start" \
  -H "Authorization: Bearer {token}"
```

#### Complete activity

```bash
curl -X POST "http://localhost:5000/api/v1/activities/{id}/complete" \
  -H "Authorization: Bearer {token}"
```

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
