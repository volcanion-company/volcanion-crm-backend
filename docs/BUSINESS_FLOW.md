# CRM SaaS Business Flow - Luồng Nghiệp Vụ Chi Tiết

## Tổng quan

Document này mô tả chi tiết luồng nghiệp vụ của hệ thống CRM từ khi có thông tin khách hàng tiềm năng (Lead) cho đến khi ký hợp đồng và hỗ trợ sau bán hàng.

---

## 1. OVERALL BUSINESS FLOW

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     CRM COMPLETE BUSINESS CYCLE                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  MARKETING          SALES              SALES OPS          POST-SALES        │
│  ─────────────────  ─────────────────  ────────────────  ────────────────   │
│                                                                             │
│  1. CAMPAIGN        2. LEAD           3. OPPORTUNITY    4. CONTRACT         │
│     Marketing          Lead              Deal             Signed            │
│     activities         generation        pipeline         agreement         │
│     ↓                  ↓                 ↓                ↓                 │
│  ┌──────────┐      ┌──────────┐     ┌──────────────┐  ┌──────────┐          │
│  │ Campaign │─────►│   Lead   │────►│ Opportunity  │─►│ Contract │          │
│  │          │      │          │     │              │  │          │          │
│  │ • Email  │      │ • Name   │     │ • Pipeline   │  │ • Terms  │          │
│  │ • Social │      │ • Contact│     │ • Value      │  │ • Price  │          │
│  │ • Ads    │      │ • Score  │     │ • Stage      │  │ • Period │          │
│  └──────────┘      └─────┬────┘     └──────────────┘  └──────────┘          │
│                          │                                  │               │
│                          │ Qualify                          │               │
│                          ▼                                  ▼               │
│                    ┌──────────┐                       ┌──────────┐          │
│                    │ Customer │◄──────────────────────│  Order   │          │
│                    │          │                       │          │          │
│                    │ • Active │                       │ • Items  │          │
│                    │ • Profile│                       │ • Total  │          │
│                    │ • History│                       │ • Status │          │
│                    └─────┬────┘                       └──────────┘          │
│                          │                                                  │
│                          │ Support                                          │
│                          ▼                                                  │
│                    ┌──────────┐                                             │
│  5. SUPPORT        │  Ticket  │                                             │
│     Customer       │          │                                             │
│     service        │ • Issues │                                             │
│                    │ • SLA    │                                             │
│                    └──────────┘                                             │
│                                                                             │
│  Throughout: Activities, Interactions, Notifications, Audit Logs            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. DETAILED STEP-BY-STEP FLOW

### Phase 1: Marketing & Lead Generation

#### Step 1.1: Tạo Campaign (Marketing Department)

**Khi nào:** Khi marketing muốn chạy chiến dịch thu hút khách hàng

**Ai làm:** Marketing Manager

**Làm gì:**
```
POST /api/v1/campaigns

{
  "name": "Q1 2026 Product Launch",
  "type": "Email",
  "status": "Active",
  "startDate": "2026-01-01",
  "endDate": "2026-03-31",
  "budget": 50000,
  "targetAudience": "Small businesses in technology sector",
  "description": "Email campaign to promote new CRM features"
}
```

**Kết quả:** Campaign ID được tạo, có thể track performance

---

#### Step 1.2: Lead Capture (Từ nhiều nguồn)

**Nguồn Lead (Lead Sources):**

| Source | Cách thức | Automation |
|--------|-----------|------------|
| **Website Form** | Khách điền contact form | Auto-create lead via API |
| **Email Campaign** | Click link trong email marketing | Auto-create từ marketing platform |
| **Social Media** | Facebook Lead Ads, LinkedIn | Integration tự động |
| **Trade Show** | Thu thập business card | Manual entry hoặc CSV import |
| **Phone Call** | Gọi vào hotline | Agent tạo thủ công |
| **Referral** | Khách hàng cũ giới thiệu | Manual entry với referral code |
| **Organic Search** | Tìm thấy qua Google | Website form submission |

**Ví dụ: Lead từ Website Form**

```javascript
// Website form submission
<form onSubmit={handleSubmit}>
  <input name="firstName" placeholder="First Name" />
  <input name="lastName" placeholder="Last Name" />
  <input name="email" placeholder="Email" />
  <input name="company" placeholder="Company" />
  <input name="phone" placeholder="Phone" />
  <button>Request Demo</button>
</form>

// JavaScript submit
async function handleSubmit(e) {
  e.preventDefault();
  
  // Call API to create lead
  await fetch('https://api.yourcrm.com/api/v1/leads', {
    method: 'POST',
    headers: { 
      'Authorization': 'Bearer API_KEY',
      'Content-Type': 'application/json' 
    },
    body: JSON.stringify({
      firstName: formData.firstName,
      lastName: formData.lastName,
      email: formData.email,
      companyName: formData.company,
      phone: formData.phone,
      source: 'Website',
      status: 'New',
      rating: 'Warm'
    })
  });
  
  // Show thank you page
  showThankYou();
}
```

**Kết quả:** Lead mới được tạo với status = "New"

---

### Phase 2: Lead Management & Qualification

#### Step 2.1: Lead Assignment (Tự động hoặc thủ công)

**Tự động assignment theo rules:**

```javascript
// Workflow rule: Auto-assign lead
{
  "trigger": "lead.created",
  "conditions": [
    { "field": "source", "operator": "equals", "value": "Website" },
    { "field": "estimatedValue", "operator": "greaterThan", "value": 10000 }
  ],
  "actions": [
    {
      "type": "assign",
      "assignTo": "round-robin", // Phân đều cho team
      "teamId": "sales-team-id"
    },
    {
      "type": "sendNotification",
      "message": "New hot lead assigned to you"
    }
  ]
}
```

**Manual assignment:**

```
POST /api/v1/leads/{leadId}/assign

{
  "userId": "sales-rep-user-id"
}
```

**Kết quả:** Lead được assign cho Sales Rep, rep nhận notification

---

#### Step 2.2: Lead Scoring & Rating

**Lead Score tự động tính dựa trên:**

| Tiêu chí | Điểm | Lý do |
|----------|------|-------|
| Có email công ty (@company.com) | +20 | Professional |
| Job title: Director/VP | +30 | Decision maker |
| Company size: >100 employees | +25 | Enterprise potential |
| Industry: Technology | +15 | Target market |
| Downloaded whitepaper | +10 | High interest |
| Visited pricing page | +20 | Ready to buy |
| Opened 5+ emails | +15 | Engaged |

**Lead Rating (Manual hoặc Auto):**

```
Score 0-30:   Cold ❄️  - Low priority
Score 31-60:  Warm 🌤️  - Medium priority  
Score 61-80:  Hot 🔥   - High priority
Score 81-100: Very Hot 🔥🔥 - Urgent follow-up
```

**Update lead score:**

```
PUT /api/v1/leads/{leadId}

{
  "score": 75,
  "rating": "Hot",
  "notes": "Visited pricing page 3 times, downloaded case study"
}
```

---

#### Step 2.3: Lead Qualification (BANT)

**Sales Rep qualify lead theo BANT:**

- **B**udget: Có ngân sách không?
- **A**uthority: Có quyền quyết định không?
- **N**eed: Có nhu cầu thực sự không?
- **T**imeline: Khi nào cần mua?

**Ví dụ conversation:**

```
Sales Rep: "Hi John, thanks for your interest. 
           May I ask, what's your budget range for CRM solution?"
Lead: "We have $50k allocated for this quarter"
→ Budget: ✓ Qualified

Sales Rep: "Are you the decision maker for this purchase?"
Lead: "Yes, I'm the IT Director"
→ Authority: ✓ Qualified

Sales Rep: "What's the main pain point you're trying to solve?"
Lead: "Our sales team is losing track of leads in spreadsheets"
→ Need: ✓ Real need

Sales Rep: "When do you need to have this system running?"
Lead: "Ideally within 2 months"
→ Timeline: ✓ Defined timeline
```

**Update lead status:**

```
PUT /api/v1/leads/{leadId}

{
  "status": "Qualified",
  "estimatedValue": 50000,
  "qualificationNotes": "BANT qualified: $50k budget, Decision maker, Need CRM for 20 users, Timeline: 2 months"
}
```

---

#### Step 2.4: Lead Conversion

**Khi lead qualified → Convert to Customer + Opportunity**

```
POST /api/v1/leads/{leadId}/convert

{
  "createCustomer": true,
  "createOpportunity": true,
  "opportunityData": {
    "name": "Acme Corp - CRM Implementation",
    "value": 50000,
    "pipelineId": "sales-pipeline-id",
    "stageId": "qualification-stage-id",
    "expectedCloseDate": "2026-03-15"
  }
}
```

**Kết quả:**
1. ✅ Customer record mới được tạo
2. ✅ Opportunity được tạo với value $50k
3. ✅ Lead status → "Converted"
4. ✅ Link giữa Lead, Customer, Opportunity được establish

---

### Phase 3: Opportunity Management (Sales Pipeline)

#### Step 3.1: Pipeline Setup

**Standard Sales Pipeline:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│  SALES PIPELINE: Enterprise B2B                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Stage 1      Stage 2       Stage 3         Stage 4       Stage 5       │
│  ─────────    ─────────     ──────────      ─────────     ─────────     │
│                                                                         │
│  Qualification → Discovery → Proposal → Negotiation → Closed Won/Lost   │
│                                                                         │
│  10% likely   25% likely   50% likely   75% likely    100%/0%           │
│  $500k        $375k        $250k        $187.5k       Won: $150k        │
│  (5 opps)     (3 opps)     (2 opps)     (1 opp)       Lost: $50k        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

Weighted Pipeline Value = (500k×10%) + (375k×25%) + (250k×50%) + (187.5k×75%)
                        = $50k + $93.75k + $125k + $140.6k
                        = $409.35k
```

**Create pipeline:**

```
POST /api/v1/pipelines

{
  "name": "Enterprise B2B Sales",
  "stages": [
    { "name": "Qualification", "probability": 10, "order": 1 },
    { "name": "Discovery", "probability": 25, "order": 2 },
    { "name": "Proposal", "probability": 50, "order": 3 },
    { "name": "Negotiation", "probability": 75, "order": 4 },
    { "name": "Closed Won", "probability": 100, "order": 5, "isWon": true },
    { "name": "Closed Lost", "probability": 0, "order": 6, "isLost": true }
  ]
}
```

---

#### Step 3.2: Move Opportunity Through Pipeline

**Stage 1: Qualification**

```
# Tạo opportunity sau khi convert lead
POST /api/v1/opportunities

{
  "name": "Acme Corp - CRM Implementation",
  "customerId": "acme-customer-id",
  "value": 50000,
  "pipelineId": "enterprise-pipeline-id",
  "currentStageId": "qualification-stage-id",
  "expectedCloseDate": "2026-03-15",
  "assignedToUserId": "sales-rep-id"
}
```

**Stage 2: Discovery (Hiểu nhu cầu chi tiết)**

```
# Sales call để discovery requirements
POST /api/v1/activities

{
  "type": "Call",
  "subject": "Discovery Call with Acme Corp",
  "description": "Discussed their current process, pain points, team size",
  "opportunityId": "acme-opportunity-id",
  "dueDate": "2026-01-20T14:00:00Z",
  "status": "Completed",
  "outcome": "Requirements: 20 users, integrate with Salesforce, custom reports needed"
}

# Move to Discovery stage
PUT /api/v1/opportunities/{opportunityId}

{
  "currentStageId": "discovery-stage-id",
  "notes": "Requirements gathered. Need custom integration quote."
}
```

**Stage 3: Proposal (Gửi quotation)**

```
# Create quotation
POST /api/v1/quotations

{
  "customerId": "acme-customer-id",
  "opportunityId": "acme-opportunity-id",
  "validUntil": "2026-02-15",
  "items": [
    {
      "description": "CRM Professional License (20 users)",
      "quantity": 20,
      "unitPrice": 49,
      "discount": 10,
      "amount": 882
    },
    {
      "description": "Salesforce Integration Setup",
      "quantity": 1,
      "unitPrice": 5000,
      "discount": 0,
      "amount": 5000
    },
    {
      "description": "Custom Reports Development",
      "quantity": 1,
      "unitPrice": 3000,
      "discount": 0,
      "amount": 3000
    },
    {
      "description": "Training (2 sessions)",
      "quantity": 2,
      "unitPrice": 500,
      "discount": 0,
      "amount": 1000
    }
  ],
  "subtotal": 9882,
  "taxRate": 10,
  "tax": 988.2,
  "total": 10870.2
}

# Move to Proposal stage
PUT /api/v1/opportunities/{opportunityId}

{
  "currentStageId": "proposal-stage-id",
  "value": 10870.2
}

# Send quotation email
POST /api/v1/quotations/{quotationId}/send
```

**Stage 4: Negotiation**

```
# Customer negotiates price
POST /api/v1/activities

{
  "type": "Email",
  "subject": "Price negotiation with Acme Corp",
  "description": "Customer requested 15% discount. Approved by manager.",
  "opportunityId": "acme-opportunity-id"
}

# Create revised quotation
POST /api/v1/quotations

{
  ...same as above but with 15% discount...
  "total": 9239.67
}

# Move to Negotiation stage
PUT /api/v1/opportunities/{opportunityId}

{
  "currentStageId": "negotiation-stage-id",
  "value": 9239.67
}
```

**Stage 5: Closed Won**

```
# Customer agrees to terms
PUT /api/v1/opportunities/{opportunityId}

{
  "currentStageId": "closed-won-stage-id",
  "closedDate": "2026-02-20",
  "status": "Won"
}

# Create contract
POST /api/v1/contracts

{
  "customerId": "acme-customer-id",
  "opportunityId": "acme-opportunity-id",
  "name": "Acme Corp - CRM License Agreement",
  "type": "Service",
  "value": 9239.67,
  "startDate": "2026-03-01",
  "endDate": "2027-02-28",
  "status": "Draft",
  "terms": "12 months subscription, monthly billing, 30 days cancellation notice"
}
```

---

### Phase 4: Contract & Order Management

#### Step 4.1: Contract Creation & Approval

```
# Create contract (đã làm ở trên)

# Send for approval
POST /api/v1/contracts/{contractId}/submit-for-approval

# Manager approves
POST /api/v1/contracts/{contractId}/approve

{
  "approvedBy": "sales-manager-id",
  "comments": "Approved. Good deal with 15% discount."
}

# Update contract status
PUT /api/v1/contracts/{contractId}

{
  "status": "Approved"
}
```

---

#### Step 4.2: Order Creation

```
# Create order from contract
POST /api/v1/orders

{
  "customerId": "acme-customer-id",
  "contractId": "acme-contract-id",
  "orderDate": "2026-02-20",
  "items": [
    {
      "description": "CRM Professional License (20 users) - Annual",
      "quantity": 20,
      "unitPrice": 49,
      "discount": 15,
      "amount": 9996
    },
    {
      "description": "Implementation Services",
      "quantity": 1,
      "unitPrice": 8000,
      "discount": 15,
      "amount": 6800
    }
  ],
  "subtotal": 16796,
  "taxRate": 10,
  "tax": 1679.6,
  "total": 18475.6,
  "paymentTerms": "Net 30",
  "status": "Pending"
}

# Send invoice
POST /api/v1/orders/{orderId}/send-invoice

# Customer pays
PUT /api/v1/orders/{orderId}

{
  "status": "Paid",
  "paymentDate": "2026-02-25",
  "paymentMethod": "Wire Transfer"
}
```

---

### Phase 5: Customer Onboarding & Activation

#### Step 5.1: Customer Record Update

```
# Update customer với thông tin chi tiết
PUT /api/v1/customers/{customerId}

{
  "status": "Active",
  "type": "Business",
  "annualRevenue": 5000000,
  "employeeCount": 150,
  "industry": "Technology",
  "lifecycleStage": "Customer",
  "notes": "Active customer since Feb 2026. 20 user licenses."
}
```

---

#### Step 5.2: Onboarding Activities

```
# Create onboarding tasks
POST /api/v1/activities

{
  "type": "Task",
  "subject": "Schedule kickoff meeting with Acme Corp",
  "description": "Introduce implementation team, review timeline",
  "customerId": "acme-customer-id",
  "assignedToUserId": "implementation-manager-id",
  "dueDate": "2026-03-01",
  "priority": "High"
}

POST /api/v1/activities

{
  "type": "Task",
  "subject": "Setup Acme Corp CRM instance",
  "description": "Create tenant, configure users, import sample data",
  "customerId": "acme-customer-id",
  "assignedToUserId": "technical-team-id",
  "dueDate": "2026-03-05"
}

POST /api/v1/activities

{
  "type": "Meeting",
  "subject": "Training Session 1: CRM Basics",
  "description": "Train 20 users on CRM fundamentals",
  "customerId": "acme-customer-id",
  "dueDate": "2026-03-10T10:00:00Z",
  "location": "Online - Zoom"
}
```

---

### Phase 6: Post-Sales Support

#### Step 6.1: Support Ticket Creation

**Scenario: Customer gặp vấn đề**

```
# Customer emails support@company.com
# System auto-creates ticket

POST /api/v1/tickets

{
  "subject": "Cannot import contacts from CSV",
  "description": "Getting error 'Invalid file format' when uploading CSV with 500 contacts",
  "customerId": "acme-customer-id",
  "contactId": "john-doe-contact-id",
  "type": "Problem",
  "priority": "High",
  "channel": "Email",
  "status": "New"
}

# Auto-assign based on rules
POST /api/v1/tickets/{ticketId}/assign

{
  "userId": "support-agent-id"
}

# Agent responds
POST /api/v1/tickets/{ticketId}/resolve

# Customer confirms fixed
POST /api/v1/tickets/{ticketId}/close
```

---

#### Step 6.2: Customer Success Check-ins

```
# Quarterly Business Review
POST /api/v1/activities

{
  "type": "Meeting",
  "subject": "Q2 2026 - Quarterly Business Review with Acme Corp",
  "description": "Review usage stats, discuss ROI, identify upsell opportunities",
  "customerId": "acme-customer-id",
  "assignedToUserId": "customer-success-manager-id",
  "dueDate": "2026-06-15T14:00:00Z"
}
```

---

### Phase 7: Upsell & Renewal

#### Step 7.1: Upsell Opportunity

```
# Customer wants more users
POST /api/v1/opportunities

{
  "name": "Acme Corp - Expansion to 50 users",
  "customerId": "acme-customer-id",
  "value": 14700, // 30 additional users @ $49/mo x 12 months
  "pipelineId": "expansion-pipeline-id",
  "type": "Upsell",
  "expectedCloseDate": "2026-09-01"
}
```

---

#### Step 7.2: Contract Renewal

```
# 30 days before contract expiry
POST /api/v1/activities

{
  "type": "Task",
  "subject": "Renew Acme Corp contract",
  "description": "Contract expires 2027-02-28. Send renewal quotation.",
  "customerId": "acme-customer-id",
  "dueDate": "2027-01-28"
}

# Create renewal quotation
POST /api/v1/quotations

{
  "customerId": "acme-customer-id",
  "type": "Renewal",
  "validUntil": "2027-02-20",
  "items": [...],
  "total": 19800 // 50 users (after expansion)
}

# Create renewal contract
POST /api/v1/contracts

{
  "customerId": "acme-customer-id",
  "name": "Acme Corp - CRM License Renewal",
  "type": "Service",
  "value": 19800,
  "startDate": "2027-03-01",
  "endDate": "2028-02-29",
  "parentContractId": "original-contract-id"
}
```

---

## 3. DATA RELATIONSHIPS

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ENTITY RELATIONSHIP DIAGRAM                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                           ┌──────────────┐                                  │
│                           │   Campaign   │                                  │
│                           └──────┬───────┘                                  │
│                                  │                                          │
│                                  │ generates                                │
│                                  ▼                                          │
│                           ┌──────────────┐                                  │
│                      ┌────│     Lead     │                                  │
│                      │    └──────┬───────┘                                  │
│                      │           │                                          │
│                      │           │ converts to                              │
│                      │           ▼                                          │
│    ┌──────────┐      │    ┌──────────────┐                                  │
│    │ Contact  │◄─────┼────│   Customer   │                                  │
│    └──────────┘      │    └──────┬───────┘                                  │
│         │            │           │                                          │
│         │            │           │ has many                                 │
│         │            │           ▼                                          │
│         │            │    ┌──────────────┐                                  │
│         │            └───►│ Opportunity  │                                  │
│         │                 └──────┬───────┘                                  │
│         │                        │                                          │
│         │                        │ generates                                │
│         │                        ▼                                          │
│         │                 ┌──────────────┐                                  │
│         │                 │  Quotation   │                                  │
│         │                 └──────┬───────┘                                  │
│         │                        │                                          │
│         │                        │ converts to                              │
│         │                        ▼                                          │
│         │                 ┌──────────────┐      ┌──────────┐                │
│         │                 │   Contract   │─────►│  Order   │                │
│         │                 └──────────────┘      └──────────┘                │
│         │                        │                                          │
│         ▼                        ▼                                          │
│    ┌──────────┐           ┌──────────┐                                      │
│    │  Ticket  │◄──────────│ Customer │ (support)                            │
│    └──────────┘           └──────────┘                                      │
│         │                        │                                          │
│         ▼                        ▼                                          │
│    ┌──────────────────────────────────┐                                     │
│    │        Activities                │                                     │
│    │  (Tasks, Calls, Meetings, etc.)  │                                     │
│    └──────────────────────────────────┘                                     │
│                   │                                                         │
│                   ▼                                                         │
│    ┌──────────────────────────────────┐                                     │
│    │       Interactions               │                                     │
│    │    (Communication History)       │                                     │
│    └──────────────────────────────────┘                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. TYPICAL USE CASES BY ROLE

### Marketing Manager

**Daily Tasks:**

```
1. Morning: Check campaign performance
   GET /api/v1/campaigns/{campaignId}/stats

2. Review new leads from yesterday
   GET /api/v1/leads?createdDate=yesterday&source=Campaign

3. Create new email campaign
   POST /api/v1/campaigns

4. Import leads from trade show
   POST /api/v1/import-export/leads/import
```

---

### Sales Representative

**Daily Tasks:**

```
1. Check my assigned leads
   GET /api/v1/leads/my-leads

2. Follow up on hot leads
   GET /api/v1/leads?rating=Hot&assignedTo=me

3. Qualify lead and convert
   POST /api/v1/leads/{leadId}/convert

4. Move opportunity to next stage
   PUT /api/v1/opportunities/{oppId}

5. Send quotation
   POST /api/v1/quotations
   POST /api/v1/quotations/{id}/send

6. Log activity (calls, meetings)
   POST /api/v1/activities
```

---

### Sales Manager

**Daily Tasks:**

```
1. Review pipeline health
   GET /api/v1/reports/pipeline-analysis

2. Check team performance
   GET /api/v1/analytics/team-performance

3. Review contracts pending approval
   GET /api/v1/contracts?status=PendingApproval

4. Approve high-value deals
   POST /api/v1/contracts/{id}/approve

5. Weekly forecast meeting
   GET /api/v1/reports/sales-forecast
```

---

### Support Agent

**Daily Tasks:**

```
1. Check my assigned tickets
   GET /api/v1/tickets/my-tickets

2. Check overdue tickets
   GET /api/v1/tickets/overdue

3. Resolve tickets
   POST /api/v1/tickets/{id}/resolve

4. Create knowledge base article from common issue
   POST /api/v1/kb/articles
```

---

### Customer Success Manager

**Monthly Tasks:**

```
1. Review customer health score
   GET /api/v1/analytics/customer-health

2. Schedule QBR meetings
   POST /api/v1/activities (type: Meeting)

3. Identify upsell opportunities
   GET /api/v1/customers?usage>80%&planType=Professional

4. Review contract renewals
   GET /api/v1/contracts?expiryDate=next30days
```

---

## 5. AUTOMATION & WORKFLOWS

### Workflow Example 1: Auto-assign Hot Leads

```json
{
  "name": "Auto-assign hot leads to senior reps",
  "trigger": {
    "entity": "Lead",
    "event": "Created"
  },
  "conditions": [
    {
      "field": "rating",
      "operator": "equals",
      "value": "Hot"
    },
    {
      "field": "estimatedValue",
      "operator": "greaterThan",
      "value": 50000
    }
  ],
  "actions": [
    {
      "type": "Assign",
      "assignTo": "load-balance",
      "teamId": "senior-sales-team"
    },
    {
      "type": "CreateActivity",
      "activityType": "Task",
      "subject": "Follow up on hot lead within 1 hour",
      "priority": "High",
      "dueIn": "1 hour"
    },
    {
      "type": "SendNotification",
      "channels": ["Email", "SMS"],
      "message": "HOT LEAD ASSIGNED: {{lead.name}} - {{lead.companyName}}"
    }
  ]
}
```

---

### Workflow Example 2: Contract Expiry Alert

```json
{
  "name": "Alert 30 days before contract expiry",
  "trigger": {
    "entity": "Contract",
    "event": "Scheduled",
    "schedule": "daily"
  },
  "conditions": [
    {
      "field": "endDate",
      "operator": "equals",
      "value": "today + 30 days"
    },
    {
      "field": "status",
      "operator": "equals",
      "value": "Active"
    }
  ],
  "actions": [
    {
      "type": "CreateActivity",
      "activityType": "Task",
      "subject": "Prepare renewal for {{customer.name}}",
      "assignTo": "{{contract.accountManagerId}}",
      "priority": "High"
    },
    {
      "type": "SendEmail",
      "to": "{{contract.accountManagerEmail}}",
      "subject": "Contract Renewal Alert - {{customer.name}}",
      "template": "contract-renewal-alert"
    },
    {
      "type": "CreateOpportunity",
      "name": "{{customer.name}} - Contract Renewal",
      "type": "Renewal",
      "value": "{{contract.value}}",
      "expectedCloseDate": "{{contract.endDate}}"
    }
  ]
}
```

---

### Workflow Example 3: SLA Breach Alert

```json
{
  "name": "Alert when ticket SLA about to breach",
  "trigger": {
    "entity": "Ticket",
    "event": "Scheduled",
    "schedule": "every 15 minutes"
  },
  "conditions": [
    {
      "field": "status",
      "operator": "in",
      "value": ["Open", "InProgress"]
    },
    {
      "field": "slaBreachIn",
      "operator": "lessThan",
      "value": "30 minutes"
    }
  ],
  "actions": [
    {
      "type": "SendNotification",
      "to": "{{ticket.assignedToUserId}}",
      "channels": ["InApp", "Email", "SMS"],
      "priority": "Urgent",
      "message": "⚠️ SLA ALERT: Ticket {{ticket.ticketNumber}} breaches in {{ticket.slaBreachIn}}"
    },
    {
      "type": "UpdateTicket",
      "priority": "Critical"
    },
    {
      "type": "NotifyManager",
      "managerId": "{{ticket.assignedToUser.managerId}}"
    }
  ]
}
```

---

## 6. REPORTING & ANALYTICS

### Key Reports

#### 6.1 Sales Pipeline Report

```
GET /api/v1/reports/pipeline-analysis?dateFrom=2026-01-01&dateTo=2026-03-31

Response:
{
  "stages": [
    {
      "stageName": "Qualification",
      "count": 15,
      "totalValue": 750000,
      "weightedValue": 75000,
      "averageAge": 5
    },
    {
      "stageName": "Discovery",
      "count": 10,
      "totalValue": 500000,
      "weightedValue": 125000,
      "averageAge": 12
    },
    {
      "stageName": "Proposal",
      "count": 6,
      "totalValue": 300000,
      "weightedValue": 150000,
      "averageAge": 18
    },
    {
      "stageName": "Negotiation",
      "count": 3,
      "totalValue": 150000,
      "weightedValue": 112500,
      "averageAge": 25
    }
  ],
  "totalPipelineValue": 1700000,
  "weightedPipelineValue": 462500,
  "averageDealSize": 50000,
  "conversionRate": 25
}
```

---

#### 6.2 Lead Conversion Report

```
GET /api/v1/reports/lead-conversion?period=Q1-2026

Response:
{
  "totalLeads": 500,
  "convertedLeads": 125,
  "conversionRate": 25,
  "bySource": [
    { "source": "Website", "total": 200, "converted": 60, "rate": 30 },
    { "source": "Referral", "total": 100, "converted": 35, "rate": 35 },
    { "source": "Email Campaign", "total": 150, "converted": 25, "rate": 16.7 },
    { "source": "Trade Show", "total": 50, "converted": 5, "rate": 10 }
  ],
  "byRating": [
    { "rating": "Hot", "total": 50, "converted": 45, "rate": 90 },
    { "rating": "Warm", "total": 200, "converted": 70, "rate": 35 },
    { "rating": "Cold", "total": 250, "converted": 10, "rate": 4 }
  ],
  "averageTimeToConvert": 15 // days
}
```

---

#### 6.3 Customer Lifetime Value

```
GET /api/v1/analytics/customer-ltv

Response:
{
  "averageLifetimeValue": 45000,
  "averageContractLength": 24, // months
  "averageMonthlyRevenue": 1875,
  "churnRate": 5, // percent
  "topCustomers": [
    {
      "customerName": "Acme Corp",
      "totalRevenue": 250000,
      "contractsCount": 5,
      "activeYears": 3
    }
  ]
}
```

---

## 7. INTEGRATION POINTS

### 7.1 Email Integration (Send/Receive)

```javascript
// Outbound: Send email from CRM
POST /api/v1/emails/send

{
  "to": "customer@example.com",
  "subject": "Your CRM quotation",
  "body": "<html>...</html>",
  "attachments": ["quotation.pdf"],
  "trackOpens": true,
  "trackClicks": true,
  "relatedTo": {
    "type": "Quotation",
    "id": "quotation-id"
  }
}

// Inbound: Receive email → Create ticket/interaction
Webhook from email service → POST /api/v1/tickets (auto-create)
```

---

### 7.2 Calendar Sync (Google/Outlook)

```javascript
// Sync activities to calendar
POST /api/v1/calendar-sync/enable

{
  "provider": "Google",
  "calendarId": "primary",
  "syncActivities": true,
  "syncMeetings": true
}

// When activity created in CRM → Auto-create calendar event
// When event created in calendar → Auto-create activity in CRM
```

---

### 7.3 Webhooks (External Systems)

```javascript
// Register webhook to notify external system
POST /api/v1/webhooks

{
  "url": "https://external-system.com/webhooks/crm",
  "events": [
    "lead.created",
    "opportunity.won",
    "contract.signed",
    "ticket.created"
  ],
  "isActive": true
}

// External system receives:
{
  "eventType": "opportunity.won",
  "timestamp": "2026-01-18T10:00:00Z",
  "data": {
    "opportunityId": "...",
    "customerName": "Acme Corp",
    "value": 50000
  }
}
```

---

## 8. COMPLETE EXAMPLE: End-to-End

**Scenario: "Acme Corp" từ Lead đến Customer có Contract**

### Timeline

```
Day 1 (Jan 1):
  ✓ Marketing campaign "Q1 Product Launch" starts
  ✓ John Doe from Acme Corp sees Facebook ad
  ✓ John clicks ad → Lands on website
  ✓ John fills "Request Demo" form
  ✓ System creates Lead: "John Doe - Acme Corp"
  ✓ Lead auto-assigned to Sales Rep (Sarah)
  ✓ Sarah receives notification

Day 2 (Jan 2):
  ✓ Sarah calls John (Activity: Call logged)
  ✓ BANT qualification completed
  ✓ Lead score updated: 75 (Hot)
  ✓ Estimated value: $50k

Day 5 (Jan 5):
  ✓ Sarah converts Lead → Customer + Opportunity
  ✓ Customer: "Acme Corp" created
  ✓ Opportunity: "Acme Corp - CRM Implementation" created
  ✓ Stage: Qualification

Day 8 (Jan 8):
  ✓ Discovery call (Activity: Meeting)
  ✓ Requirements gathered
  ✓ Opportunity moved to Discovery stage

Day 12 (Jan 12):
  ✓ Sarah creates Quotation ($10,870)
  ✓ Quotation sent to John
  ✓ Opportunity moved to Proposal stage

Day 15 (Jan 15):
  ✓ John requests 15% discount
  ✓ Sarah escalates to Manager
  ✓ Manager approves
  ✓ Revised quotation sent ($9,239)
  ✓ Opportunity moved to Negotiation stage

Day 18 (Jan 18):
  ✓ John accepts quotation
  ✓ Opportunity moved to Closed Won
  ✓ Contract created
  ✓ Contract submitted for approval
  ✓ Manager approves contract

Day 20 (Jan 20):
  ✓ Order created from contract
  ✓ Invoice sent to Acme Corp

Day 25 (Jan 25):
  ✓ Payment received
  ✓ Order status: Paid
  ✓ Customer status: Active

Day 30 (Jan 30):
  ✓ Onboarding activities created
  ✓ Implementation starts

Day 45 (Feb 15):
  ✓ Training completed
  ✓ System live

Month 3 (March 15):
  ✓ First support ticket created
  ✓ Resolved in 2 hours

Month 6 (June 1):
  ✓ Quarterly Business Review
  ✓ Customer satisfaction: 9/10
  ✓ Upsell opportunity identified (expand to 50 users)

Month 12 (Dec 1):
  ✓ Renewal conversation starts
  ✓ Renewal quotation sent

Month 13 (Jan 1 next year):
  ✓ Contract renewed for another year
  ✓ Customer lifetime: 13 months
  ✓ Total revenue: $28,000
```

---

## 9. BEST PRACTICES

### Data Entry Best Practices

1. **Always capture lead source** - để track marketing ROI
2. **Use consistent naming** - "Acme Corp" không phải "ACME Corp" hoặc "Acme Corporation"
3. **Link contacts to customers** - không tạo orphan contacts
4. **Log all activities** - mọi call, email, meeting đều log lại
5. **Update opportunity value** - khi có thay đổi giá
6. **Add notes** - context cho team members khác

### Automation Guidelines

1. **Start simple** - một vài workflows cơ bản trước
2. **Test thoroughly** - test workflow trước khi activate
3. **Monitor performance** - xem workflow có chạy đúng không
4. **Avoid infinite loops** - workflow không trigger chính nó
5. **Use conditions wisely** - không spam notifications

### Performance Tips

1. **Pagination** - luôn dùng pageSize khi query list
2. **Selective fields** - chỉ lấy fields cần thiết
3. **Caching** - cache static data (pipelines, users)
4. **Batch operations** - import/update nhiều records cùng lúc
5. **Webhook over polling** - dùng webhooks thay vì poll API

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial business flow documentation |
