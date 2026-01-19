# Webhooks API Documentation

## Tổng quan Module

### Webhook là gì?

**Webhook** là cơ chế cho phép CRM tự động gửi dữ liệu real-time đến external systems khi có sự kiện xảy ra (customer created, opportunity won, ticket resolved, v.v.). Thay vì external system phải liên tục polling API, webhook sẽ "push" data ngay lập tức khi event xảy ra.

### Tại sao Webhook quan trọng?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   WEBHOOKS = REAL-TIME INTEGRATION                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Without Webhooks (Polling):     With Webhooks (Push):                     │
│  ────────────────────────────    ───────────────────────                   │
│  External System:                External System:                           │
│    Every 5 mins:                   Just waits...                            │
│    ┌─► "Any new data?"                                                      │
│    │   "No"                       CRM Event occurs:                         │
│    │   (waste bandwidth)            • Customer created                      │
│    │                                                                         │
│    ├─► "Any new data?"           CRM immediately sends:                     │
│    │   "No"                         POST https://external.com/webhook       │
│    │   (waste bandwidth)            {                                       │
│    │                                  "event": "customer.created",          │
│    ├─► "Any new data?"                "data": {...}                         │
│    │   "Yes! 1 customer"            }                                       │
│    └─► Fetch customer                                                       │
│        (delayed by 5 mins)        External system receives instantly!       │
│                                                                             │
│  ❌ Delayed notifications          ✅ Real-time notifications (< 1 sec)     │
│  ❌ Waste bandwidth/resources      ✅ Efficient, event-driven               │
│  ❌ Miss time-sensitive events     ✅ Immediate action possible             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Webhook Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        WEBHOOK FLOW                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. SUBSCRIBE                                                               │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ External System subscribes to events:                    │           │
│     │   • URL: https://external-system.com/crm-webhook         │           │
│     │   • Events: ["customer.created", "opportunity.won"]      │           │
│     │   • Secret: "secret-key-for-signature"                   │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
│  2. EVENT OCCURS                                                            │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ CRM Event: Customer "ABC Corp" created                   │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│  3. WEBHOOK TRIGGERED                                                       │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ CRM finds matching subscriptions:                        │           │
│     │   • Subscription: "External System Integration"          │           │
│     │   • Subscribed to: "customer.created" ✓                  │           │
│     │   • IsActive: true ✓                                     │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│  4. PAYLOAD PREPARATION                                                     │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ {                                                         │           │
│     │   "event": "customer.created",                           │           │
│     │   "timestamp": "2026-01-18T10:00:00Z",                   │           │
│     │   "data": {                                              │           │
│     │     "id": "customer-guid",                               │           │
│     │     "name": "ABC Corp",                                  │           │
│     │     "type": "Company",                                   │           │
│     │     "status": "Active"                                   │           │
│     │   }                                                       │           │
│     │ }                                                         │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│  5. HTTP POST                                                               │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ POST https://external-system.com/crm-webhook             │           │
│     │ Headers:                                                  │           │
│     │   Content-Type: application/json                         │           │
│     │   X-Webhook-Signature: sha256=abc123...                  │           │
│     │   X-Webhook-Event: customer.created                      │           │
│     │ Body: {payload}                                           │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│  6. RESPONSE                                                                │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ Success: HTTP 200 OK                                     │           │
│     │ Delivery logged: Status = Success                        │           │
│     │                                                           │           │
│     │ OR                                                        │           │
│     │                                                           │           │
│     │ Failure: HTTP 500 Error                                  │           │
│     │ Delivery logged: Status = Failed                         │           │
│     │ Retry scheduled: +5 mins (attempt 1/3)                   │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Webhook Events

### Available Events

| Category | Event Name | Description | When Triggered |
|----------|------------|-------------|----------------|
| **Customer** | `customer.created` | New customer created | Create customer |
| | `customer.updated` | Customer info updated | Update customer |
| | `customer.deleted` | Customer deleted | Delete customer |
| **Lead** | `lead.created` | New lead created | Create lead |
| | `lead.updated` | Lead info updated | Update lead |
| | `lead.converted` | Lead converted to customer | Convert lead |
| | `lead.deleted` | Lead deleted | Delete lead |
| **Opportunity** | `opportunity.created` | New opportunity | Create opportunity |
| | `opportunity.updated` | Opportunity updated | Update opportunity |
| | `opportunity.won` | Deal won! | Status → Won |
| | `opportunity.lost` | Deal lost | Status → Lost |
| | `opportunity.deleted` | Opportunity deleted | Delete opportunity |
| **Ticket** | `ticket.created` | New support ticket | Create ticket |
| | `ticket.updated` | Ticket updated | Update ticket |
| | `ticket.assigned` | Ticket assigned | Change assignee |
| | `ticket.resolved` | Ticket resolved | Status → Resolved |
| | `ticket.closed` | Ticket closed | Status → Closed |
| | `ticket.escalated` | Ticket escalated | Priority increased |
| **Order** | `order.created` | New order placed | Create order |
| | `order.updated` | Order updated | Update order |
| | `order.cancelled` | Order cancelled | Status → Cancelled |
| **Contract** | `contract.created` | New contract | Create contract |
| | `contract.renewed` | Contract renewed | Renew contract |
| | `contract.expired` | Contract expired | EndDate passed |
| **Campaign** | `campaign.started` | Campaign started | Activate campaign |
| | `campaign.completed` | Campaign ended | Complete campaign |
| **Activity** | `activity.created` | New activity/task | Create activity |
| | `activity.completed` | Activity done | Complete activity |

---

## Webhook Security

### HMAC Signature Validation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     WEBHOOK SIGNATURE VERIFICATION                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. CRM generates signature:                                                │
│     ─────────────────────────────────────────────────────────────────────   │
│     Secret: "my-secret-key"                                                 │
│     Payload: '{"event":"customer.created","data":{...}}'                    │
│                                                                             │
│     Signature = HMAC-SHA256(Payload, Secret)                                │
│              = "a1b2c3d4e5f6..."                                            │
│                                                                             │
│  2. CRM sends request:                                                      │
│     ─────────────────────────────────────────────────────────────────────   │
│     POST https://external-system.com/webhook                                │
│     Headers:                                                                │
│       X-Webhook-Signature: sha256=a1b2c3d4e5f6...                           │
│     Body:                                                                   │
│       {"event":"customer.created","data":{...}}                             │
│                                                                             │
│  3. External system validates:                                              │
│     ─────────────────────────────────────────────────────────────────────   │
│     Received signature: "sha256=a1b2c3d4e5f6..."                            │
│     Received payload: '{"event":"customer.created","data":{...}}'           │
│     Known secret: "my-secret-key"                                           │
│                                                                             │
│     Computed signature = HMAC-SHA256(Payload, Secret)                       │
│                        = "a1b2c3d4e5f6..."                                  │
│                                                                             │
│     If received == computed:                                                │
│       ✅ Authentic webhook from CRM                                         │
│       → Process the event                                                   │
│     Else:                                                                   │
│       ❌ Invalid signature, possible attack                                 │
│       → Reject request (HTTP 401)                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Example Validation Code (Node.js)

```javascript
const crypto = require('crypto');

function validateWebhookSignature(payload, receivedSignature, secret) {
    // Extract signature value (remove "sha256=" prefix)
    const signature = receivedSignature.replace('sha256=', '');
    
    // Compute expected signature
    const expectedSignature = crypto
        .createHmac('sha256', secret)
        .update(payload)
        .digest('hex');
    
    // Constant-time comparison to prevent timing attacks
    return crypto.timingSafeEqual(
        Buffer.from(signature),
        Buffer.from(expectedSignature)
    );
}

// Express.js webhook endpoint
app.post('/crm-webhook', (req, res) => {
    const signature = req.headers['x-webhook-signature'];
    const payload = JSON.stringify(req.body);
    const secret = 'my-secret-key';
    
    if (!validateWebhookSignature(payload, signature, secret)) {
        return res.status(401).send('Invalid signature');
    }
    
    // Process webhook
    const event = req.body.event;
    const data = req.body.data;
    
    console.log(`Received event: ${event}`, data);
    
    // Return 200 OK to acknowledge receipt
    res.status(200).send('OK');
});
```

---

## Retry Policy

### Retry Strategy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          WEBHOOK RETRY LOGIC                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Initial Attempt (t=0):                                                     │
│  ─────────────────────────────────────────────────────────────────────────  │
│  POST https://external.com/webhook                                          │
│  Result: HTTP 503 Service Unavailable                                       │
│  Status: Failed                                                             │
│  NextRetryAt: t+5 minutes                                                   │
│                                                                             │
│  Retry Attempt 1 (t+5 mins):                                                │
│  ─────────────────────────────────────────────────────────────────────────  │
│  POST https://external.com/webhook                                          │
│  Result: Timeout (30 seconds)                                               │
│  Status: Failed                                                             │
│  NextRetryAt: t+15 minutes                                                  │
│                                                                             │
│  Retry Attempt 2 (t+15 mins):                                               │
│  ─────────────────────────────────────────────────────────────────────────  │
│  POST https://external.com/webhook                                          │
│  Result: HTTP 500 Internal Server Error                                     │
│  Status: Failed                                                             │
│  NextRetryAt: t+30 minutes                                                  │
│                                                                             │
│  Retry Attempt 3 (t+30 mins):                                               │
│  ─────────────────────────────────────────────────────────────────────────  │
│  POST https://external.com/webhook                                          │
│  Result: HTTP 200 OK                                                        │
│  Status: Success ✓                                                          │
│  CompletedAt: 2026-01-18T11:00:00Z                                         │
│                                                                             │
│  ═══════════════════════════════════════════════════════════════════════    │
│                                                                             │
│  Exponential Backoff:                                                       │
│  ─────────────────────                                                      │
│  Attempt 1: +5 minutes  (300 seconds)                                       │
│  Attempt 2: +10 minutes (600 seconds)                                       │
│  Attempt 3: +15 minutes (900 seconds)                                       │
│                                                                             │
│  Max Retries: 3 (configurable)                                              │
│  Timeout: 30 seconds (configurable)                                         │
│                                                                             │
│  If all retries fail → Status: Failed (final)                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Subscribe to Webhook (Create Subscription)

**Nghiệp vụ thực tế:**
- External system đăng ký nhận events từ CRM
- Define URL endpoint để nhận webhooks
- Specify events cần subscribe

**Ví dụ thực tế:**
> Accounting system muốn sync customers từ CRM:
> - Create webhook subscription:
>   * Name: "Accounting System Sync"
>   * URL: https://accounting.company.com/api/crm-webhook
>   * Events: ["customer.created", "customer.updated"]
>   * Secret: "secret-key-abc123"
>   * MaxRetries: 3
> - Save subscription
> → Now accounting system receives customer events real-time

---

### 2. Get All Subscriptions (Get Subscriptions)

**Nghiệp vụ thực tế:**
- View tất cả webhook subscriptions
- Monitor active integrations
- Manage external connections

**Ví dụ thực tế:**
> Admin xem subscriptions:
> - Active subscriptions:
>   * Accounting System Sync (customer.*)
>   * Marketing Automation (lead.*, campaign.*)
>   * Help Desk System (ticket.*)
> - Inactive subscriptions:
>   * Old Analytics Tool (deprecated)
> → 3 active, 1 inactive

---

### 3. Get Subscription Details (Get Subscription by ID)

**Nghiệp vụ thực tế:**
- View full subscription config
- Check delivery statistics
- Debug webhook issues

**Ví dụ thực tế:**
> Click "Accounting System Sync":
> - URL: https://accounting.company.com/api/crm-webhook
> - Events: ["customer.created", "customer.updated"]
> - IsActive: true
> - Success Rate: 98.5%
> - SuccessCount: 1,234
> - FailureCount: 19
> - LastSuccessAt: 2 minutes ago
> → High success rate, working well

---

### 4. Update Subscription (Update Subscription)

**Nghiệp vụ thực tế:**
- Change webhook URL
- Add/remove events
- Update configuration

**Ví dụ thực tế:**
> Accounting system migrated to new server:
> - Old URL: https://old-server.com/webhook
> - New URL: https://new-server.com/webhook
> - Update subscription URL
> - Test webhook
> → All future webhooks go to new server

---

### 5. Delete Subscription (Delete Subscription)

**Nghiệp vụ thực tế:**
- Remove integration
- Stop sending webhooks
- Clean up old subscriptions

**Ví dụ thực tế:**
> Old analytics tool no longer used:
> - Subscription: "Old Analytics Tool"
> - LastSuccessAt: 6 months ago
> - Click "Delete"
> → Subscription removed, no more webhooks sent

---

### 6. Test Webhook (Test Subscription)

**Nghiệp vụ thực tế:**
- Verify webhook endpoint working
- Test before going live
- Debug connection issues

**Ví dụ thực tế:**
> After creating new subscription:
> - Click "Test Webhook"
> - CRM sends test payload:
>   ```json
>   {
>     "event": "test.webhook",
>     "timestamp": "2026-01-18T10:00:00Z",
>     "data": {"message": "Test webhook"}
>   }
>   ```
> - Result:
>   * HTTP Status: 200 OK
>   * Response Time: 245ms
>   * Success: true
> → Webhook endpoint working correctly!

---

### 7. Get Statistics (Get Webhook Stats)

**Nghiệp vụ thực tế:**
- Monitor webhook health
- Track delivery success rates
- Identify problematic integrations

**Ví dụ thực tế:**
> Weekly webhook report:
> - Total Deliveries: 5,432
> - Successful: 5,301 (97.6%)
> - Failed: 131 (2.4%)
> - Average Response Time: 320ms
> - Top Events:
>   * customer.updated: 2,134 deliveries
>   * opportunity.created: 876 deliveries
>   * ticket.created: 654 deliveries
> → Overall healthy, investigate failures

---

## Real-World Use Cases

### Use Case 1: Accounting System Integration

```
Scenario: Sync customers to QuickBooks automatically
─────────────────────────────────────────────────────

CRM Event: Customer "ABC Corp" created
  ↓
Webhook triggered: customer.created
  ↓
Payload sent to QuickBooks:
{
  "event": "customer.created",
  "data": {
    "id": "customer-guid",
    "name": "ABC Corp",
    "type": "Company",
    "email": "contact@abccorp.com",
    "phone": "+1-555-1234"
  }
}
  ↓
QuickBooks receives webhook:
  • Creates customer in QuickBooks
  • Returns 200 OK
  ↓
Customer synced! No manual data entry needed.
```

### Use Case 2: Marketing Automation

```
Scenario: Trigger email campaign when lead created
───────────────────────────────────────────────────

CRM Event: Lead "John Doe" created from website form
  ↓
Webhook triggered: lead.created
  ↓
Payload sent to Mailchimp:
{
  "event": "lead.created",
  "data": {
    "email": "john@example.com",
    "name": "John Doe",
    "source": "Website",
    "tags": ["website-lead", "hot"]
  }
}
  ↓
Mailchimp receives webhook:
  • Adds contact to list
  • Triggers welcome email sequence
  • Tags as "website-lead"
  ↓
Automated nurture campaign started!
```

### Use Case 3: Slack Notifications

```
Scenario: Notify sales team when big deal won
──────────────────────────────────────────────

CRM Event: Opportunity "$500K Enterprise Deal" status → Won
  ↓
Webhook triggered: opportunity.won
  ↓
Payload sent to Slack webhook:
{
  "event": "opportunity.won",
  "data": {
    "name": "$500K Enterprise Deal",
    "amount": 500000,
    "customer": "ABC Corp",
    "closedBy": "John Smith"
  }
}
  ↓
Slack receives webhook:
  • Posts message to #sales channel:
    "🎉 DEAL WON! John Smith closed $500K deal with ABC Corp!"
  • @mentions @sales-team
  ↓
Team celebrates instantly! 🎊
```

### Use Case 4: Help Desk Integration

```
Scenario: Create Zendesk ticket when CRM ticket created
────────────────────────────────────────────────────────

CRM Event: Support ticket created
  ↓
Webhook triggered: ticket.created
  ↓
Payload sent to Zendesk:
{
  "event": "ticket.created",
  "data": {
    "subject": "Login issue",
    "description": "Customer cannot login",
    "priority": "High",
    "customer": "ABC Corp"
  }
}
  ↓
Zendesk receives webhook:
  • Creates ticket in Zendesk
  • Assigns to support agent
  • Sends auto-reply to customer
  ↓
Unified support across platforms!
```

---

## Best Practices

### 1. Return 200 OK Quickly

```javascript
// ❌ BAD: Process webhook synchronously
app.post('/webhook', async (req, res) => {
    const event = req.body;
    
    // This takes 5 seconds!
    await processEvent(event);
    await updateDatabase(event);
    await sendEmail(event);
    
    res.status(200).send('OK'); // Too slow!
});

// ✅ GOOD: Acknowledge immediately, process async
app.post('/webhook', async (req, res) => {
    const event = req.body;
    
    // Acknowledge receipt immediately
    res.status(200).send('OK');
    
    // Process asynchronously
    queueJob(async () => {
        await processEvent(event);
        await updateDatabase(event);
        await sendEmail(event);
    });
});
```

### 2. Validate Signature

```javascript
// ❌ BAD: Trust any incoming webhook
app.post('/webhook', (req, res) => {
    processEvent(req.body); // Dangerous!
    res.status(200).send('OK');
});

// ✅ GOOD: Validate signature
app.post('/webhook', (req, res) => {
    const signature = req.headers['x-webhook-signature'];
    
    if (!validateSignature(req.body, signature, SECRET)) {
        return res.status(401).send('Invalid signature');
    }
    
    processEvent(req.body);
    res.status(200).send('OK');
});
```

### 3. Handle Idempotency

```javascript
// ✅ Handle duplicate deliveries (retries)
app.post('/webhook', async (req, res) => {
    const event = req.body;
    const eventId = event.id;
    
    // Check if already processed
    const alreadyProcessed = await db.query(
        'SELECT * FROM processed_webhooks WHERE event_id = ?',
        [eventId]
    );
    
    if (alreadyProcessed.length > 0) {
        // Already processed, return success
        return res.status(200).send('Already processed');
    }
    
    // Process event
    await processEvent(event);
    
    // Mark as processed
    await db.query(
        'INSERT INTO processed_webhooks (event_id) VALUES (?)',
        [eventId]
    );
    
    res.status(200).send('OK');
});
```

### 4. Implement Timeout & Retry Logic

- **Timeout**: CRM waits max 30 seconds for response
- **Retry**: Automatic retry with exponential backoff
- **Max Retries**: 3 attempts (configurable)

### 5. Monitor & Alert

```
Set up monitoring:
──────────────────
• Alert if success rate < 95%
• Alert if avg response time > 5 seconds
• Alert if >10 consecutive failures
• Weekly digest: Total deliveries, top events
```

---

## Technical Overview

**Base URL:** `/api/v1/webhooks`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Subscriptions

Lấy danh sách tất cả webhook subscriptions.

```
GET /api/v1/webhooks
```

**Permission Required:** `webhook.view`

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Accounting System Sync",
      "targetUrl": "https://accounting.company.com/api/webhook",
      "events": ["customer.created", "customer.updated"],
      "isActive": true,
      "contentType": "application/json",
      "maxRetries": 3,
      "timeoutSeconds": 30,
      "lastSuccessAt": "2026-01-18T09:55:00Z",
      "lastFailureAt": "2026-01-15T14:30:00Z",
      "successCount": 1234,
      "failureCount": 19,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### 2. Get Subscription by ID

Lấy chi tiết một subscription.

```
GET /api/v1/webhooks/{id}
```

**Permission Required:** `webhook.view`

---

### 3. Create Subscription

Tạo webhook subscription mới.

```
POST /api/v1/webhooks
```

**Permission Required:** `webhook.create`

#### Request Body

```json
{
  "name": "Accounting System Sync",
  "targetUrl": "https://accounting.company.com/api/webhook",
  "secret": "my-secret-key-abc123",
  "events": ["customer.created", "customer.updated", "customer.deleted"],
  "isActive": true,
  "contentType": "application/json",
  "maxRetries": 3,
  "timeoutSeconds": 30,
  "description": "Sync customer data to QuickBooks",
  "customHeaders": "{\"Authorization\": \"Bearer token123\"}"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Subscription name |
| `targetUrl` | string | **Yes** | Webhook URL endpoint |
| `secret` | string | No | Secret for HMAC signature |
| `events` | array | **Yes** | Events to subscribe |
| `isActive` | bool | No | Active status (default: true) |
| `contentType` | string | No | Content type (default: application/json) |
| `maxRetries` | int | No | Max retry attempts (default: 3) |
| `timeoutSeconds` | int | No | Timeout (default: 30) |
| `description` | string | No | Description |
| `customHeaders` | string | No | Custom headers (JSON) |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-subscription-guid",
    "name": "Accounting System Sync",
    "targetUrl": "https://accounting.company.com/api/webhook",
    "events": ["customer.created", "customer.updated", "customer.deleted"],
    "isActive": true,
    "createdAt": "2026-01-18T10:00:00Z"
  }
}
```

---

### 4. Update Subscription

Cập nhật subscription.

```
PUT /api/v1/webhooks/{id}
```

**Permission Required:** `webhook.update`

#### Request Body (All fields optional)

```json
{
  "name": "Updated Subscription Name",
  "targetUrl": "https://new-server.com/webhook",
  "events": ["customer.created", "customer.updated"],
  "isActive": true
}
```

---

### 5. Delete Subscription

Xóa subscription.

```
DELETE /api/v1/webhooks/{id}
```

**Permission Required:** `webhook.delete`

---

### 6. Test Subscription

Test webhook bằng cách gửi test payload.

```
POST /api/v1/webhooks/{id}/test
```

**Permission Required:** `webhook.test`

#### Response

```json
{
  "success": true,
  "data": {
    "success": true,
    "statusCode": 200,
    "responseTime": 245,
    "message": "Webhook test successful"
  }
}
```

---

### 7. Get Statistics

Lấy webhook delivery statistics.

```
GET /api/v1/webhooks/stats
```

**Permission Required:** `webhook.view`

#### Response

```json
{
  "success": true,
  "data": {
    "totalDeliveries": 5432,
    "successCount": 5301,
    "failureCount": 131,
    "successRate": 97.6,
    "avgResponseTimeMs": 320,
    "topEvents": [
      {"event": "customer.updated", "count": 2134},
      {"event": "opportunity.created", "count": 876},
      {"event": "ticket.created", "count": 654}
    ]
  }
}
```

---

## Webhook Payload Format

### Standard Payload Structure

```json
{
  "id": "delivery-guid",
  "event": "customer.created",
  "timestamp": "2026-01-18T10:00:00Z",
  "data": {
    "id": "entity-guid",
    "...": "entity data"
  }
}
```

### Example: customer.created

```json
{
  "id": "webhook-delivery-guid",
  "event": "customer.created",
  "timestamp": "2026-01-18T10:00:00Z",
  "data": {
    "id": "customer-guid",
    "name": "ABC Corporation",
    "type": "Company",
    "status": "Active",
    "email": "contact@abccorp.com",
    "phone": "+1-555-1234",
    "website": "https://abccorp.com",
    "createdAt": "2026-01-18T10:00:00Z"
  }
}
```

### Example: opportunity.won

```json
{
  "id": "webhook-delivery-guid",
  "event": "opportunity.won",
  "timestamp": "2026-01-18T15:30:00Z",
  "data": {
    "id": "opportunity-guid",
    "name": "$500K Enterprise Deal",
    "amount": 500000,
    "customerId": "customer-guid",
    "customerName": "ABC Corporation",
    "stage": "Closed Won",
    "closeDate": "2026-01-18",
    "closedBy": "John Smith",
    "probability": 100
  }
}
```

---

## Enums

### WebhookDeliveryStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Pending | Queued for delivery |
| 1 | Sending | Currently sending |
| 2 | Success | Delivered successfully |
| 3 | Failed | Delivery failed |
| 4 | Retrying | Retry scheduled |
| 5 | Cancelled | Delivery cancelled |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `webhook.view` | View subscriptions & stats |
| `webhook.create` | Create subscriptions |
| `webhook.update` | Update subscriptions |
| `webhook.delete` | Delete subscriptions |
| `webhook.test` | Test webhooks |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
