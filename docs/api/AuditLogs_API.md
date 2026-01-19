# Audit Logs API Documentation

## Tổng quan Module

### Audit Log là gì?

**Audit Log (nhật ký kiểm toán)** là hệ thống ghi lại tất cả các hành động quan trọng trong hệ thống để theo dõi, compliance, và security monitoring.

### Tại sao cần Audit Logs?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    WHY AUDIT LOGS ARE CRITICAL                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Compliance & Security:                                                     │
│  ─────────────────────────────────────────────────────────────────────     │
│  • GDPR/HIPAA/SOX compliance requirements                                   │
│  • Security incident investigation                                          │
│  • "Who accessed what, when, and from where?"                               │
│  • Detect unauthorized access or data breaches                              │
│                                                                             │
│  Accountability:                                                            │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Track who made changes                                                   │
│  • Prevent "wasn't me" scenarios                                            │
│  • Manager: "Who deleted this customer?"                                    │
│  • Audit log: "John Doe, Jan 18 10:30am, IP 192.168.1.100"                  │
│                                                                             │
│  Data Recovery & Debugging:                                                 │
│  ─────────────────────────────────────────────────────────────────────     │
│  • What was the old value before update?                                    │
│  • Why did this record disappear?                                           │
│  • Restore deleted data from audit trail                                    │
│                                                                             │
│  Business Intelligence:                                                     │
│  ─────────────────────────────────────────────────────────────────────     │
│  • User activity patterns                                                   │
│  • Most accessed/modified entities                                          │
│  • Peak usage times                                                         │
│  • Feature adoption metrics                                                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Audit Log Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        AUDIT LOGGING FLOW                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. User Action                                                             │
│     ┌────────────────────────────────────────┐                             │
│     │ PUT /api/customers/123                 │                             │
│     │ {                                      │                             │
│     │   "name": "Acme Corporation",          │                             │
│     │   "status": "Active"                   │                             │
│     │ }                                      │                             │
│     └────────────────┬───────────────────────┘                             │
│                      ▼                                                      │
│  2. Middleware Intercepts                                                   │
│     ┌──────────────────────────────────────┐                               │
│     │ AuditMiddleware                      │                               │
│     │   • Capture request details          │                               │
│     │   • Extract user info (JWT)          │                               │
│     │   • Get IP address                   │                               │
│     │   • Log request start                │                               │
│     └────────────────┬─────────────────────┘                               │
│                      ▼                                                      │
│  3. Entity Framework Change Tracking                                        │
│     ┌──────────────────────────────────────┐                               │
│     │ SaveChanges Interceptor              │                               │
│     │   • Detect modified entities         │                               │
│     │   • Compare old vs new values        │                               │
│     │     Before: { "name": "Acme Corp",   │                               │
│     │               "status": "Inactive" }  │                               │
│     │     After:  { "name": "Acme Corp",   │                               │
│     │               "status": "Active" }    │                               │
│     │   • Changed: ["status"]              │                               │
│     └────────────────┬─────────────────────┘                               │
│                      ▼                                                      │
│  4. Create Audit Log Entry                                                  │
│     ┌──────────────────────────────────────┐                               │
│     │ AuditLog:                            │                               │
│     │   Action: "Update"                   │                               │
│     │   EntityType: "Customer"             │                               │
│     │   EntityId: 123                      │                               │
│     │   EntityName: "Acme Corporation"     │                               │
│     │   UserId: john-guid                  │                               │
│     │   UserEmail: john@example.com        │                               │
│     │   IpAddress: 192.168.1.100           │                               │
│     │   OldValues: { "status": "Inactive" }│                               │
│     │   NewValues: { "status": "Active" }  │                               │
│     │   ChangedProperties: ["status"]      │                               │
│     │   Timestamp: 2026-01-18 10:30:00     │                               │
│     └──────────────────────────────────────┘                               │
│                      ▼                                                      │
│  5. Save to Database (Master DB)                                            │
│     INSERT INTO AuditLogs VALUES (...)                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Audit Action Types

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AUDITABLE ACTIONS                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  CRUD Operations:                                                           │
│  • Create: New customer/lead/opportunity created                            │
│  • Update: Existing record modified                                         │
│  • Delete: Record permanently deleted                                       │
│  • SoftDelete: Record marked as deleted (IsDeleted = true)                  │
│  • Restore: Soft-deleted record restored                                    │
│                                                                             │
│  Authentication:                                                            │
│  • Login: User successfully logged in                                       │
│  • LoginFailed: Failed login attempt (wrong password)                       │
│  • Logout: User logged out                                                  │
│  • PasswordChanged: Password updated                                        │
│  • PasswordReset: Admin reset user password                                 │
│                                                                             │
│  Status Changes:                                                            │
│  • StatusChange: Entity status modified (Lead: New → Qualified)             │
│  • Assign: Record assigned to different user                                │
│  • Convert: Lead converted to customer                                      │
│                                                                             │
│  Data Operations:                                                           │
│  • View: Sensitive data accessed (audit trail for privacy)                  │
│  • Import: Bulk data imported                                               │
│  • Export: Data exported (compliance requirement)                           │
│                                                                             │
│  System Actions:                                                            │
│  • PermissionGranted: User given new permission                             │
│  • PermissionRevoked: Permission removed                                    │
│  • ConfigChange: System configuration modified                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Get All Audit Logs (Danh sách audit logs)

**Nghiệp vụ thực tế:**
- Admin xem activity history
- Filter by action, entity, user, date range

**Ví dụ thực tế:**
> Security team investigating incident:
> - Filter: EntityType = "Customer", Action = "Delete", Last 7 days
> - Results:
>   * Jan 15, 2pm: John Doe deleted "Beta Inc" (IP: 192.168.1.50)
>   * Jan 16, 10am: Sarah Smith deleted "Gamma LLC" (IP: 192.168.1.75)
>   * Jan 17, 4pm: Bob Johnson deleted "Delta Corp" (IP: 192.168.1.100)
> → Identify who deleted customers

---

### 2. Get Audit Log by ID (Chi tiết audit log)

**Nghiệp vụ thực tế:**
- View full details of specific action
- See before/after values

**Ví dụ thực tế:**
> Manager clicks audit log entry:
> - Action: Update
> - Entity: Opportunity "Enterprise Deal"
> - User: Sarah Smith (sarah@example.com)
> - Timestamp: Jan 18, 2026 2:30pm
> - IP Address: 192.168.1.75
> - Old Values:
>   {
>     "stage": "Proposal",
>     "value": 25000,
>     "closeDate": "2026-02-01"
>   }
> - New Values:
>   {
>     "stage": "Negotiation",
>     "value": 50000,
>     "closeDate": "2026-01-25"
>   }
> - Changed Properties: ["stage", "value", "closeDate"]
> → Sarah increased deal value and moved stage

---

### 3. Get Actions (List available actions)

**Nghiệp vụ thực tế:**
- Dropdown filter for action type
- Search logs by action

**Ví dụ thực tế:**
> Admin building audit report filter:
> - Available Actions:
>   * Create
>   * Update
>   * Delete
>   * SoftDelete
>   * Login
>   * Logout
>   * Export
>   * ...
> → Select "Export" to see all data export activities

---

### 4. Get Entity Types (List entity types)

**Nghiệp vụ thực tế:**
- Filter logs by entity type
- See which entities are most modified

**Ví dụ thực tế:**
> Compliance officer reviewing activity:
> - Entity Types in system:
>   * Customer (2,345 logs)
>   * Lead (1,890 logs)
>   * Opportunity (1,456 logs)
>   * User (234 logs)
>   * Contract (189 logs)
> → Customer is most frequently modified entity

---

### 5. Get Audit Statistics (Activity analytics)

**Nghiệp vụ thực tế:**
- Analyze user activity patterns
- Identify suspicious behavior

**Ví dụ thực tế:**
> Security dashboard for last 30 days:
> 
> **Total Actions:** 5,678
> 
> **By Action:**
> - Update: 2,345 (41%)
> - Create: 1,890 (33%)
> - View: 987 (17%)
> - Delete: 234 (4%)
> - Export: 122 (2%)
> - Login: 100 (2%)
> 
> **By Entity Type:**
> - Customer: 1,456 (26%)
> - Lead: 1,234 (22%)
> - Opportunity: 1,089 (19%)
> - Contact: 876 (15%)
> - Ticket: 567 (10%)
> 
> **Top 10 Most Active Users:**
> 1. sarah@example.com: 456 actions
> 2. john@example.com: 389 actions
> 3. bob@example.com: 345 actions
> ...
> 
> **Activity by Day:**
> - Jan 15: 234 actions
> - Jan 16: 189 actions
> - Jan 17: 267 actions (spike - investigate?)
> - Jan 18: 145 actions
> 
> → Identify patterns, anomalies, peak usage times

---

## Investigation Scenarios

### Scenario 1: Data Breach Investigation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SECURITY INCIDENT RESPONSE                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Incident: Unauthorized customer data export                                │
│  ─────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  Step 1: Find export actions                                                │
│    Filter: Action = "Export", EntityType = "Customer", Last 24 hours        │
│                                                                             │
│  Step 2: Review suspicious exports                                          │
│    Results:                                                                 │
│    • 10:30pm: user@example.com exported 5,000 customers                     │
│      IP: 203.0.113.45 (unusual location - Russia)                           │
│      User Agent: wget/1.20.3 (automated tool)                               │
│      → RED FLAG: After-hours, foreign IP, bot user agent                    │
│                                                                             │
│  Step 3: Check user's other activities                                      │
│    Filter: UserId = user-guid, Last 24 hours                                │
│    • 10:25pm: Login from 203.0.113.45                                       │
│    • 10:27pm: PasswordChanged                                               │
│    • 10:30pm: Export 5,000 customers                                        │
│    • 10:35pm: Logout                                                        │
│    → Account compromised, data exfiltrated                                  │
│                                                                             │
│  Step 4: Response actions                                                   │
│    • Disable user account immediately                                       │
│    • Force password reset for all users                                     │
│    • Notify affected customers (GDPR requirement)                           │
│    • Contact law enforcement                                                │
│    • Audit log provides evidence for investigation                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Scenario 2: Accidental Deletion Recovery

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      DATA RECOVERY SCENARIO                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Problem: Important customer "MegaCorp" accidentally deleted                │
│  ─────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  Step 1: Find deletion event                                                │
│    Filter: EntityType = "Customer", EntityName contains "MegaCorp",         │
│            Action = "Delete", Last 7 days                                   │
│                                                                             │
│  Step 2: View audit log details                                             │
│    • Action: Delete                                                         │
│    • Entity: Customer "MegaCorp International"                              │
│    • EntityId: customer-guid-123                                            │
│    • User: john@example.com                                                 │
│    • Timestamp: Jan 17, 2026 3:45pm                                         │
│    • OldValues: {                                                           │
│        "name": "MegaCorp International",                                    │
│        "email": "contact@megacorp.com",                                     │
│        "phone": "+1-555-0199",                                              │
│        "industry": "Technology",                                            │
│        "annualRevenue": 50000000,                                           │
│        "employeeCount": 500,                                                │
│        ...                                                                  │
│      }                                                                      │
│                                                                             │
│  Step 3: Restore from audit log                                             │
│    • Use OldValues to recreate customer record                              │
│    • Or: Implement soft delete + restore from "IsDeleted" records           │
│    → Customer data recovered!                                               │
│                                                                             │
│  Step 4: Prevent future accidents                                           │
│    • Require confirmation for delete actions                                │
│    • Implement soft delete by default                                       │
│    • Restrict delete permission to managers only                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Best Practices

### 1. What to Audit

```
✅ ALWAYS Audit:
  • Authentication (login/logout/failed attempts)
  • Authorization changes (permissions granted/revoked)
  • Data modifications (CRUD operations)
  • Sensitive data access (customer PII, financial data)
  • Data exports (compliance requirement)
  • Configuration changes
  • Admin actions

❌ DON'T Audit:
  • Read operations (too much noise, except sensitive data)
  • Internal system processes
  • Health checks, monitoring pings
  • Static file access
```

### 2. Performance Considerations

```csharp
// ❌ BAD: Synchronous audit logging blocks request
public async Task<IActionResult> UpdateCustomer(Guid id, CustomerUpdateDto dto)
{
    var customer = await _db.Customers.FindAsync(id);
    customer.Name = dto.Name;
    await _db.SaveChangesAsync();
    
    // This blocks the response!
    await _auditService.LogAsync(AuditActions.Update, nameof(Customer), id, ...);
    
    return Ok(customer);
}

// ✅ GOOD: Fire-and-forget audit logging
public async Task<IActionResult> UpdateCustomer(Guid id, CustomerUpdateDto dto)
{
    var customer = await _db.Customers.FindAsync(id);
    customer.Name = dto.Name;
    await _db.SaveChangesAsync();
    
    // Fire and forget (non-blocking)
    _ = Task.Run(async () => 
        await _auditService.LogAsync(AuditActions.Update, nameof(Customer), id, ...));
    
    return Ok(customer);
}

// ✅ EVEN BETTER: Use background queue
public async Task<IActionResult> UpdateCustomer(Guid id, CustomerUpdateDto dto)
{
    var customer = await _db.Customers.FindAsync(id);
    customer.Name = dto.Name;
    await _db.SaveChangesAsync();
    
    // Queue for background processing (Hangfire, etc.)
    _backgroundQueue.QueueAuditLog(new AuditLogEntry { ... });
    
    return Ok(customer);
}
```

### 3. Sensitive Data Redaction

```csharp
public class AuditService
{
    private static readonly string[] SensitiveProperties = 
    { 
        "password", "passwordHash", "creditCard", "ssn", "taxId" 
    };
    
    public string RedactSensitiveData(string jsonValues)
    {
        var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonValues);
        
        foreach (var prop in SensitiveProperties)
        {
            if (obj.ContainsKey(prop))
            {
                obj[prop] = "***REDACTED***";
            }
        }
        
        return JsonSerializer.Serialize(obj);
    }
}

// Example:
// Before: { "email": "john@example.com", "password": "MyPass123!" }
// After:  { "email": "john@example.com", "password": "***REDACTED***" }
```

### 4. Retention Policy

```csharp
public async Task EnforceRetentionPolicyAsync()
{
    // Keep audit logs for compliance period (e.g., 7 years for financial)
    var retentionDays = _config.GetValue<int>("AuditLog:RetentionDays", 2555); // ~7 years
    var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
    
    // Archive old logs to cold storage before deletion
    var logsToArchive = await _db.AuditLogs
        .Where(a => a.Timestamp < cutoffDate)
        .ToListAsync();
    
    if (logsToArchive.Any())
    {
        await ArchiveToColdStorageAsync(logsToArchive);
        await _db.AuditLogs.Where(a => a.Timestamp < cutoffDate).ExecuteDeleteAsync();
        
        _logger.LogInformation($"Archived and deleted {logsToArchive.Count} old audit logs");
    }
}
```

---

## Technical Overview

**Base URL:** `/api/v1/audit-logs`

**Authentication:** Bearer Token (JWT)

**Permission Required:** `audit.view` (typically Admin/Manager only)

---

## Endpoints

### 1. Get All Audit Logs

Lấy danh sách audit logs với filter & pagination.

```
GET /api/v1/audit-logs
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `pageNumber` | int | Page number (default: 1) |
| `pageSize` | int | Page size (default: 20, max: 100) |
| `sortBy` | string | Sort field (default: "Timestamp") |
| `sortDescending` | boolean | Sort order (default: true) |
| `action` | string | Filter by action (e.g., "Create", "Update", "Delete") |
| `entityType` | string | Filter by entity type (e.g., "Customer", "Lead") |
| `userId` | Guid | Filter by user ID |
| `startDate` | DateTime | Filter logs after this date |
| `endDate` | DateTime | Filter logs before this date |

**Response:**

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "log-guid-1",
        "action": "Update",
        "entityType": "Customer",
        "entityId": "customer-guid",
        "entityName": "Acme Corporation",
        "userId": "user-guid",
        "userEmail": "john@example.com",
        "ipAddress": "192.168.1.100",
        "timestamp": "2026-01-18T14:30:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 5678,
    "totalPages": 284
  }
}
```

---

### 2. Get Audit Log by ID

Lấy chi tiết full audit log entry.

```
GET /api/v1/audit-logs/{id}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "id": "log-guid",
    "action": "Update",
    "entityType": "Opportunity",
    "entityId": "opp-guid",
    "entityName": "Enterprise Deal",
    "oldValues": "{\"stage\":\"Proposal\",\"value\":25000}",
    "newValues": "{\"stage\":\"Negotiation\",\"value\":50000}",
    "changedProperties": "[\"stage\",\"value\"]",
    "userId": "user-guid",
    "userEmail": "sarah@example.com",
    "userName": "Sarah Smith",
    "ipAddress": "192.168.1.75",
    "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
    "requestPath": "/api/v1/opportunities/opp-guid",
    "httpMethod": "PUT",
    "additionalData": "{\"reason\":\"Customer requested expedited timeline\"}",
    "timestamp": "2026-01-18T14:30:00Z"
  }
}
```

---

### 3. Get Available Actions

Lấy list các action types để filter.

```
GET /api/v1/audit-logs/actions
```

**Response:**

```json
{
  "success": true,
  "data": [
    "Create",
    "Update",
    "Delete",
    "SoftDelete",
    "Restore",
    "Login",
    "Logout",
    "LoginFailed",
    "PasswordChanged",
    "PasswordReset",
    "StatusChange",
    "Assign",
    "Convert",
    "View",
    "Import",
    "Export"
  ]
}
```

---

### 4. Get Entity Types

Lấy list entity types có trong audit logs.

```
GET /api/v1/audit-logs/entity-types
```

**Response:**

```json
{
  "success": true,
  "data": [
    "Customer",
    "Lead",
    "Opportunity",
    "Contact",
    "Ticket",
    "User",
    "Role",
    "Contract",
    "Campaign"
  ]
}
```

---

### 5. Get Audit Statistics

Lấy statistics về activity patterns.

```
GET /api/v1/audit-logs/statistics
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `startDate` | DateTime | Start date (default: 30 days ago) |
| `endDate` | DateTime | End date (default: now) |

**Response:**

```json
{
  "success": true,
  "data": {
    "totalActions": 5678,
    "byAction": [
      { "action": "Update", "count": 2345 },
      { "action": "Create", "count": 1890 },
      { "action": "View", "count": 987 },
      { "action": "Delete", "count": 234 }
    ],
    "byEntityType": [
      { "entityType": "Customer", "count": 1456 },
      { "entityType": "Lead", "count": 1234 },
      { "entityType": "Opportunity", "count": 1089 }
    ],
    "byUser": [
      {
        "userId": "user-guid-1",
        "userEmail": "sarah@example.com",
        "actionCount": 456
      },
      {
        "userId": "user-guid-2",
        "userEmail": "john@example.com",
        "actionCount": 389
      }
    ],
    "byDay": [
      { "date": "2026-01-15", "count": 234 },
      { "date": "2026-01-16", "count": 189 },
      { "date": "2026-01-17", "count": 267 },
      { "date": "2026-01-18", "count": 145 }
    ]
  }
}
```

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `audit.view` | View audit logs and statistics |

**Note:** Typically restricted to Admin, Manager, or Compliance Officer roles.

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
