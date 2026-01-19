# Workflows API Documentation

## Tổng quan Module

### Workflow là gì?

**Workflow (Quy trình tự động)** là hệ thống automation giúp tự động hóa các business process trong CRM. Khi một sự kiện xảy ra (tạo lead, update opportunity, v.v.), workflow sẽ tự động thực hiện các actions như send email, create task, update fields, assign owner.

### Tại sao Workflow quan trọng?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  WORKFLOWS = CRM AUTOMATION ENGINE                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Without Workflows:              With Workflows:                            │
│  ──────────────────              ───────────────                            │
│  ❌ Manual follow-up            ✅ Auto create task when lead created      │
│  ❌ Forget to assign            ✅ Auto assign based on territory          │
│  ❌ Inconsistent process        ✅ Standardized automated process          │
│  ❌ Miss notifications          ✅ Auto notify manager on big deal         │
│  ❌ Human errors                ✅ 100% consistent execution                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Workflow Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      WORKFLOW HIERARCHY                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │ WORKFLOW: "Lead Assignment & Follow-up"                      │          │
│  │ ─────────────────────────────────────────────────────────    │          │
│  │ Entity: Lead                                                 │          │
│  │ Trigger: OnCreate                                            │          │
│  │ IsActive: true                                               │          │
│  └──────────────────┬───────────────────────────────────────────┘          │
│                     │                                                       │
│                     │ has many                                              │
│                     ▼                                                       │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │ RULE 1: "Assign to Sales Rep"                                │          │
│  │ ─────────────────────────────────────────────────────────    │          │
│  │ Conditions:                                                   │          │
│  │   • Lead.Source == "Website"                                 │          │
│  │   • Lead.Budget > 10000                                      │          │
│  │ Logic: AND (all conditions must match)                       │          │
│  └──────────────────┬───────────────────────────────────────────┘          │
│                     │                                                       │
│                     │ has many                                              │
│                     ▼                                                       │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │ ACTION 1: Assign Owner                                        │          │
│  │   Type: AssignOwner                                           │          │
│  │   Config: {"UserId": "sales-rep-guid"}                       │          │
│  │   Order: 1                                                    │          │
│  └──────────────────────────────────────────────────────────────┘          │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │ ACTION 2: Send Welcome Email                                 │          │
│  │   Type: SendEmail                                             │          │
│  │   Config: {"TemplateId": "xxx", "To": "{{Lead.Email}}"}     │          │
│  │   Order: 2                                                    │          │
│  └──────────────────────────────────────────────────────────────┘          │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │ ACTION 3: Create Follow-up Task                              │          │
│  │   Type: CreateTask                                            │          │
│  │   Config: {"Subject": "Call lead", "DueDate": "+3d"}        │          │
│  │   DelayMinutes: 1440 (wait 24 hours)                         │          │
│  │   Order: 3                                                    │          │
│  └──────────────────────────────────────────────────────────────┘          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Workflow Components

### 1. Workflow (Container)

```
┌───────────────────────────────────────────────────────────┐
│ WORKFLOW                                                  │
├───────────────────────────────────────────────────────────┤
│ • Name: "Lead Assignment & Follow-up"                    │
│ • EntityType: "Lead" (Customer/Opportunity/Ticket/etc.)  │
│ • TriggerType: OnCreate/OnUpdate/OnDelete/Scheduled      │
│ • IsActive: true/false                                   │
│ • ExecutionOrder: 0 (priority)                           │
│ • StopOnMatch: false (continue to next workflows)        │
└───────────────────────────────────────────────────────────┘
```

### 2. Rule (Conditions)

```
┌───────────────────────────────────────────────────────────┐
│ RULE                                                      │
├───────────────────────────────────────────────────────────┤
│ • Name: "High-value website leads"                       │
│ • Conditions: JSON array of conditions                   │
│   [                                                       │
│     {"Field": "Source", "Operator": "Equals",            │
│      "Value": "Website"},                                │
│     {"Field": "Budget", "Operator": "GreaterThan",       │
│      "Value": 10000}                                     │
│   ]                                                       │
│ • ConditionLogic: AND/OR                                 │
│ • IsActive: true/false                                   │
│ • Order: 0 (execution order)                             │
└───────────────────────────────────────────────────────────┘
```

### 3. Action (What to do)

```
┌───────────────────────────────────────────────────────────┐
│ ACTION                                                    │
├───────────────────────────────────────────────────────────┤
│ • ActionType: SendEmail/CreateTask/AssignOwner/etc.      │
│ • ActionConfig: JSON configuration                       │
│   {                                                       │
│     "TemplateId": "welcome-email-guid",                  │
│     "To": "{{Lead.Email}}",                              │
│     "Subject": "Welcome {{Lead.FirstName}}!"             │
│   }                                                       │
│ • DelayMinutes: 0 (immediate) or 1440 (24 hours)        │
│ • IsActive: true/false                                   │
│ • Order: 0 (execution order)                             │
└───────────────────────────────────────────────────────────┘
```

---

## Trigger Types

### 1. OnCreate - Khi tạo record mới

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          OnCreate TRIGGER                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Event: New Lead created                                                    │
│  ─────────────────────────────────────────────────────────────────────────  │
│  User creates Lead:                                                         │
│    • Name: "ABC Corp"                                                       │
│    • Source: "Website"                                                      │
│    • Budget: $50,000                                                        │
│                                                                             │
│  Workflow triggers immediately after creation                               │
│                                                                             │
│  Actions executed:                                                          │
│    ✓ Assign to sales rep                                                   │
│    ✓ Send welcome email                                                    │
│    ✓ Create follow-up task (24h delay)                                     │
│    ✓ Notify manager (big budget)                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2. OnUpdate - Khi update record

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          OnUpdate TRIGGER                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Workflow: "Opportunity Won Automation"                                     │
│  TriggerFields: ["Status"] (only trigger if Status changed)                │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  Before Update:                                                             │
│    Opportunity Status: Negotiation                                          │
│                                                                             │
│  After Update:                                                              │
│    Opportunity Status: Won                                                  │
│                                                                             │
│  Workflow triggers → Actions:                                               │
│    ✓ Send congratulations email to sales rep                               │
│    ✓ Create contract from opportunity                                      │
│    ✓ Create onboarding tasks                                               │
│    ✓ Update customer status to "Active"                                    │
│    ✓ Webhook to accounting system                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3. OnDelete - Khi xóa record (soft delete)

### 4. Scheduled - Time-based trigger

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SCHEDULED TRIGGER                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Workflow: "Weekly Overdue Tasks Reminder"                                  │
│  ScheduleExpression: "0 9 * * MON" (every Monday at 9 AM)                  │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  Every Monday 9 AM:                                                         │
│    1. Query all tasks where DueDate < Today AND Status != Completed        │
│    2. For each overdue task:                                                │
│       • Send reminder email to assigned user                                │
│       • Send notification to manager                                        │
│       • Create escalation record if >7 days overdue                         │
│                                                                             │
│  Cron Examples:                                                             │
│  ───────────────                                                            │
│  "0 9 * * *"     - Every day at 9 AM                                        │
│  "0 */4 * * *"   - Every 4 hours                                            │
│  "0 9 * * MON"   - Every Monday at 9 AM                                     │
│  "0 9 1 * *"     - First day of every month at 9 AM                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5. Manual - User triggered

---

## Condition Operators

### Comparison Operators

| Operator | Description | Example |
|----------|-------------|---------|
| **Equals** | Field == Value | Status equals "Won" |
| **NotEquals** | Field != Value | Status not equals "Lost" |
| **GreaterThan** | Field > Value | Amount > 10000 |
| **GreaterThanOrEqual** | Field >= Value | Amount >= 5000 |
| **LessThan** | Field < Value | DaysOpen < 30 |
| **LessThanOrEqual** | Field <= Value | Priority <= 2 |

### String Operators

| Operator | Description | Example |
|----------|-------------|---------|
| **Contains** | String contains substring | Name contains "Corp" |
| **NotContains** | String not contains | Email not contains "gmail" |
| **StartsWith** | String starts with | Phone starts with "+1" |
| **EndsWith** | String ends with | Email ends with ".com" |

### Null Operators

| Operator | Description | Example |
|----------|-------------|---------|
| **IsNull** | Field is null/empty | Description is null |
| **IsNotNull** | Field has value | AssignedTo is not null |

### List Operators

| Operator | Description | Example |
|----------|-------------|---------|
| **In** | Value in list | Status in ["New", "Contacted"] |
| **NotIn** | Value not in list | Source not in ["Spam"] |
| **Between** | Value between range | Amount between [5000, 10000] |

### Change Operators (for OnUpdate)

| Operator | Description | Example |
|----------|-------------|---------|
| **Changed** | Field value changed | Status changed (any value) |
| **ChangedTo** | Changed to specific value | Status changed to "Won" |
| **ChangedFrom** | Changed from specific value | Status changed from "Draft" |

---

## Action Types

### 1. UpdateField - Update field value

```json
{
  "ActionType": "UpdateField",
  "ActionConfig": {
    "Field": "Status",
    "Value": "Contacted"
  }
}
```

### 2. SendEmail - Send email notification

```json
{
  "ActionType": "SendEmail",
  "ActionConfig": {
    "TemplateId": "template-guid",
    "To": "{{Lead.Email}}",
    "Cc": "manager@company.com",
    "Subject": "Welcome {{Lead.FirstName}}!",
    "Variables": {
      "leadName": "{{Lead.Name}}",
      "assignedTo": "{{AssignedToUser.FullName}}"
    }
  }
}
```

### 3. CreateTask - Create activity/task

```json
{
  "ActionType": "CreateTask",
  "ActionConfig": {
    "Subject": "Follow up with {{Lead.Name}}",
    "Description": "Call to discuss requirements",
    "Type": "Call",
    "Priority": "High",
    "DueDate": "+3d",
    "AssignedTo": "{{OwnerId}}"
  }
}
```

### 4. AssignOwner - Change record owner

```json
{
  "ActionType": "AssignOwner",
  "ActionConfig": {
    "UserId": "user-guid"
  }
}
```

```json
{
  "ActionType": "AssignOwner",
  "ActionConfig": {
    "RoundRobin": true,
    "Team": "Sales"
  }
}
```

### 5. CreateActivity - Create follow-up activity

```json
{
  "ActionType": "CreateActivity",
  "ActionConfig": {
    "Type": "Meeting",
    "Subject": "Product demo with {{Customer.Name}}",
    "StartDate": "+2d",
    "DurationMinutes": 60,
    "AssignedTo": "{{OwnerId}}"
  }
}
```

### 6. SendWebhook - Call external API

```json
{
  "ActionType": "SendWebhook",
  "ActionConfig": {
    "Url": "https://external-system.com/api/webhook",
    "Method": "POST",
    "Headers": {
      "Authorization": "Bearer {{API_KEY}}",
      "Content-Type": "application/json"
    },
    "Payload": {
      "event": "opportunity_won",
      "opportunityId": "{{Opportunity.Id}}",
      "amount": "{{Opportunity.Amount}}",
      "customerName": "{{Customer.Name}}"
    }
  }
}
```

### 7. SendNotification - In-app notification

```json
{
  "ActionType": "SendNotification",
  "ActionConfig": {
    "UserId": "{{ManagerId}}",
    "Title": "High-value opportunity created",
    "Message": "{{User.FullName}} created ${{Amount}} opportunity",
    "Type": "Info",
    "Link": "/opportunities/{{Id}}"
  }
}
```

### 8. SendSms - SMS notification

```json
{
  "ActionType": "SendSms",
  "ActionConfig": {
    "To": "{{Lead.Phone}}",
    "Message": "Thanks for your interest! We'll contact you within 24 hours."
  }
}
```

---

## Real-World Examples

### Example 1: Lead Auto-Assignment

```
Workflow: "Auto-assign Website Leads"
Entity: Lead
Trigger: OnCreate
─────────────────────────────────────────────

Rule: "High-value website leads"
Conditions:
  • Source == "Website" AND
  • Budget > 10000

Actions:
  1. AssignOwner (Round-robin to Sales team)
  2. SendEmail (Welcome email to lead)
  3. CreateTask (Follow-up call in 24 hours)
  4. SendNotification (Notify manager about big lead)
```

### Example 2: Opportunity Won Automation

```
Workflow: "Opportunity Won Process"
Entity: Opportunity
Trigger: OnUpdate
TriggerFields: ["Status"]
─────────────────────────────────────────────

Rule: "Status changed to Won"
Conditions:
  • Status ChangedTo "Won"

Actions:
  1. UpdateField (Customer.Status = "Active")
  2. CreateRecord (Create Contract from Opportunity)
  3. CreateActivity (Schedule kickoff meeting)
  4. SendEmail (Congratulations to sales rep)
  5. SendWebhook (Notify accounting system)
```

### Example 3: Overdue Tasks Reminder

```
Workflow: "Overdue Tasks Daily Reminder"
Entity: Activity
Trigger: Scheduled
ScheduleExpression: "0 9 * * *" (daily 9 AM)
─────────────────────────────────────────────

Rule: "Tasks overdue"
Conditions:
  • DueDate < Today AND
  • Status != "Completed"

Actions:
  1. SendEmail (Reminder to assigned user)
  2. SendNotification (In-app reminder)
  3. If >7 days overdue:
     • SendEmail (Escalate to manager)
```

### Example 4: Customer Satisfaction Follow-up

```
Workflow: "Post-Ticket Resolution Survey"
Entity: Ticket
Trigger: OnUpdate
TriggerFields: ["Status"]
─────────────────────────────────────────────

Rule: "Ticket resolved"
Conditions:
  • Status ChangedTo "Resolved"

Actions:
  1. Delay: 1440 minutes (24 hours)
  2. SendEmail (Customer satisfaction survey)
  3. CreateTask (Follow-up if no response in 3 days)
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Workflow (Create Workflow)

**Nghiệp vụ thực tế:**
- Admin define automation rules
- Standardize business processes
- Reduce manual work

**Ví dụ thực tế:**
> Sales Manager muốn auto-assign leads:
> - Create workflow:
>   * Name: "Website Lead Assignment"
>   * Entity: Lead
>   * Trigger: OnCreate
>   * IsActive: true
> - Save workflow
> → Now define rules and actions

---

### 2. Tạo Rule (Create Workflow Rule)

**Nghiệp vụ thực tế:**
- Define conditions for automation
- Specify when actions should run
- Support complex logic (AND/OR)

**Ví dụ thực tế:**
> Add rule to workflow:
> - Rule: "High-value website leads"
> - Conditions:
>   * Lead.Source == "Website"
>   * Lead.Budget > 10000
> - Logic: AND (both must be true)
> → Rule will match high-value website leads only

---

### 3. Tạo Action (Create Workflow Action)

**Nghiệp vụ thực tế:**
- Define what to do when rule matches
- Support multiple actions in sequence
- Can delay actions

**Ví dụ thực tế:**
> Add actions to rule:
> - Action 1: Assign owner
>   * Config: Round-robin to Sales team
>   * Order: 1
> - Action 2: Send welcome email
>   * Config: Use template, send to {{Lead.Email}}
>   * Order: 2
> - Action 3: Create follow-up task
>   * Config: Call lead in 3 days
>   * DelayMinutes: 4320 (3 days)
>   * Order: 3
> → Actions execute in order: Assign → Email → Task (delayed)

---

### 4. Get All Workflows (Get Workflows)

**Nghiệp vụ thực tế:**
- View all automation workflows
- Filter by entity type, active status
- Manage workflow library

**Ví dụ thực tế:**
> Admin xem workflows:
> - Filter: EntityType = "Lead"
> - Result:
>   * "Website Lead Assignment" (Active)
>   * "Cold Lead Nurture" (Active)
>   * "Spam Lead Filter" (Inactive)
> → 3 lead workflows, 2 active

---

### 5. Get Workflow Details (Get Workflow by ID)

**Nghiệp vụ thực tế:**
- View full workflow configuration
- See rules and actions
- Debug workflow issues

**Ví dụ thực tế:**
> Click "Website Lead Assignment":
> - Workflow details:
>   * Entity: Lead
>   * Trigger: OnCreate
>   * 2 Rules:
>     1. High-value leads (3 actions)
>     2. Low-value leads (2 actions)
> → Full workflow structure visible

---

### 6. Update Workflow (Update Workflow)

**Nghiệp vụ thực tế:**
- Modify workflow settings
- Change trigger type
- Adjust execution order

---

### 7. Toggle Workflow (Toggle Active/Inactive)

**Nghiệp vụ thực tế:**
- Enable/disable workflow
- Test without deleting
- Temporarily stop automation

**Ví dụ thực tế:**
> Workflow causing issues:
> - Current: Active
> - Click "Toggle" → Inactive
> - Fix workflow configuration
> - Click "Toggle" → Active
> → Workflow now working correctly

---

### 8. Get Execution Logs (Get Workflow Logs)

**Nghiệp vụ thực tế:**
- Audit trail of workflow executions
- Debug failed workflows
- Track automation performance

**Ví dụ thực tế:**
> Check why lead not assigned:
> - View logs for "Website Lead Assignment"
> - Last execution:
>   * Entity: Lead ABC Corp
>   * Status: Failed
>   * Error: "Sales team has no available users"
> → Found issue: All sales reps at capacity

---

## Best Practices

### 1. Start Simple

```
❌ Complex workflow with 20 rules:
   • Hard to understand
   • Difficult to debug
   • Performance issues

✅ Multiple simple workflows:
   • "Lead Assignment" (1-2 rules)
   • "Lead Nurture" (2-3 rules)
   • "Lead Spam Filter" (1 rule)
   → Easier to manage and maintain
```

### 2. Use Execution Order

```
Workflow 1: "Spam Filter" (Order: 0, StopOnMatch: true)
  → Runs first, stops if spam detected

Workflow 2: "High-Value Assignment" (Order: 1)
  → Runs second, only if not spam

Workflow 3: "Standard Assignment" (Order: 2)
  → Runs last, fallback assignment
```

### 3. Test Before Activating

```
Development Process:
──────────────────────
1. Create workflow (IsActive: false)
2. Test with sample data
3. Review execution logs
4. Fix any issues
5. Activate (IsActive: true)
6. Monitor for 24 hours
7. Adjust if needed
```

### 4. Use Delays Strategically

```
Example: Post-purchase follow-up
─────────────────────────────────
Action 1: Send thank you email (immediate)
Action 2: Request review (delay: 3 days)
Action 3: Upsell offer (delay: 7 days)
Action 4: Renewal reminder (delay: 30 days)
```

### 5. Monitor Performance

```
Key Metrics:
────────────
• Success rate: >95% successful executions
• Avg duration: <500ms per workflow
• Failed executions: Investigate all failures
• Execution count: Track most-used workflows
```

---

## Technical Overview

**Base URLs:**
- Workflows: `/api/v1/workflows`
- Rules: `/api/v1/workflows/{workflowId}/rules`
- Actions: `/api/v1/workflows/{workflowId}/rules/{ruleId}/actions`

**Authentication:** Bearer Token (JWT)

---

## Workflows Endpoints

### 1. Get All Workflows

```
GET /api/v1/workflows
```

**Permission Required:** `workflow.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `entityType` | string | No | - | Filter by entity (Lead/Customer/Opportunity) |
| `isActive` | bool | No | - | Filter active/inactive |
| `page` | int | No | 1 | Page number |
| `pageSize` | int | No | 50 | Items per page |

---

### 2. Get Workflow by ID

```
GET /api/v1/workflows/{id}
```

**Permission Required:** `workflow.view`

---

### 3. Create Workflow

```
POST /api/v1/workflows
```

**Permission Required:** `workflow.create`

#### Request Body

```json
{
  "name": "Website Lead Assignment",
  "description": "Auto-assign website leads to sales team",
  "entityType": "Lead",
  "triggerType": 0,
  "isActive": true,
  "executionOrder": 0,
  "stopOnMatch": false
}
```

---

### 4. Update Workflow

```
PUT /api/v1/workflows/{id}
```

**Permission Required:** `workflow.edit`

---

### 5. Delete Workflow

```
DELETE /api/v1/workflows/{id}
```

**Permission Required:** `workflow.delete`

---

### 6. Toggle Workflow Active Status

```
PUT /api/v1/workflows/{id}/toggle
```

**Permission Required:** `workflow.edit`

---

### 7. Get Workflow Execution Logs

```
GET /api/v1/workflows/{id}/logs
```

**Permission Required:** `workflow.view`

---

## Rules Endpoints

### 1. Get All Rules for Workflow

```
GET /api/v1/workflows/{workflowId}/rules
```

---

### 2. Get Rule by ID

```
GET /api/v1/workflows/{workflowId}/rules/{ruleId}
```

---

### 3. Create Rule

```
POST /api/v1/workflows/{workflowId}/rules
```

#### Request Body

```json
{
  "name": "High-value website leads",
  "description": "Leads from website with budget > $10K",
  "conditions": "[{\"Field\":\"Source\",\"Operator\":\"Equals\",\"Value\":\"Website\"},{\"Field\":\"Budget\",\"Operator\":\"GreaterThan\",\"Value\":10000}]",
  "conditionLogic": 0,
  "isActive": true,
  "order": 0
}
```

---

### 4. Update Rule

```
PUT /api/v1/workflows/{workflowId}/rules/{ruleId}
```

---

### 5. Delete Rule

```
DELETE /api/v1/workflows/{workflowId}/rules/{ruleId}
```

---

### 6. Toggle Rule Active Status

```
PUT /api/v1/workflows/{workflowId}/rules/{ruleId}/toggle
```

---

## Actions Endpoints

### 1. Get All Actions for Rule

```
GET /api/v1/workflows/{workflowId}/rules/{ruleId}/actions
```

---

### 2. Get Action by ID

```
GET /api/v1/workflows/{workflowId}/rules/{ruleId}/actions/{actionId}
```

---

### 3. Create Action

```
POST /api/v1/workflows/{workflowId}/rules/{ruleId}/actions
```

#### Request Body

```json
{
  "actionType": 3,
  "actionConfig": "{\"RoundRobin\":true,\"Team\":\"Sales\"}",
  "delayMinutes": 0,
  "isActive": true,
  "order": 1
}
```

---

### 4. Update Action

```
PUT /api/v1/workflows/{workflowId}/rules/{ruleId}/actions/{actionId}
```

---

### 5. Delete Action

```
DELETE /api/v1/workflows/{workflowId}/rules/{ruleId}/actions/{actionId}
```

---

## Enums

### WorkflowTriggerType

| Value | Name | Description |
|-------|------|-------------|
| 0 | OnCreate | When entity created |
| 1 | OnUpdate | When entity updated |
| 2 | OnDelete | When entity deleted |
| 3 | Scheduled | Time-based (cron) |
| 4 | Manual | User triggered |

### ConditionLogic

| Value | Name | Description |
|-------|------|-------------|
| 0 | And | All conditions must match |
| 1 | Or | Any condition can match |

### WorkflowActionType

| Value | Name | Description |
|-------|------|-------------|
| 0 | UpdateField | Update entity field |
| 1 | SendEmail | Send email |
| 2 | CreateTask | Create task |
| 3 | AssignOwner | Change owner |
| 4 | CreateActivity | Create activity |
| 5 | SendWebhook | Call external API |
| 6 | CreateRecord | Create related record |
| 7 | UpdateRelated | Update related records |
| 8 | SendNotification | In-app notification |
| 9 | SendSms | SMS message |

### WorkflowExecutionStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Success | Executed successfully |
| 1 | Failed | Execution failed |
| 2 | Skipped | Conditions not met |
| 3 | Pending | Queued for execution |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `workflow.view` | View workflows, rules, actions, logs |
| `workflow.create` | Create workflows, rules, actions |
| `workflow.edit` | Update workflows, rules, actions |
| `workflow.delete` | Delete workflows, rules, actions |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
