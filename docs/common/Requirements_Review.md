# Đối chiếu Requirements vs Source Code (CRM SaaS Backend)

Ngày đánh giá: 2026-01-17

## 1. Tổng quan mức độ đáp ứng
- ✅ Đã có nền tảng CRM backend đầy đủ CRUD cho các module lõi (Customer, Lead, Opportunity, Order, Contract, Ticket, Campaign, Activity, Reports).
- ✅ Có Multi‑tenancy, JWT + Refresh Token, RBAC theo permission, Audit log, Import/Export cơ bản.
- ⚠️ Nhiều yêu cầu nghiệp vụ nâng cao còn thiếu hoặc mới dừng ở mức dữ liệu/CRUD, chưa có workflow, automation, rule engine, hoặc tích hợp.
- ⚠️ Một số vấn đề kiến trúc/triển khai: shared database với 2 DbContext đang gây xung đột migration và duplicate tables.

## 2. Ma trận đối chiếu theo module

### 2.1 Customer Management
**Yêu cầu**
- Tạo/sửa/xoá/merge khách hàng
- Lưu dữ liệu cá nhân/doanh nghiệp, liên hệ, nguồn, tags
- Lịch sử tương tác
- Duplicate detection
- Audit log, phân quyền

**Hiện trạng code**
- ✅ CRUD Customers/Contacts/Interactions
- ✅ Tags, Source, CustomFields, AuditLog
- ⚠️ Chưa có chức năng merge
- ⚠️ Chưa có duplicate detection (theo email/phone/identity)
- ⚠️ Chưa có logic 360° profile tổng hợp (chỉ dữ liệu rời)

**Cải tiến đề xuất**
- Thêm API merge customer + lưu lịch sử merge
- Rule-based duplicate detection (email/phone/taxId + fuzzy matching)
- Endpoint trả về “Customer 360” (timeline + deals + tickets + activities)

---

### 2.2 Lead Management
**Yêu cầu**
- Thu lead từ website/Facebook/Google/import/API
- Phân công lead
- Chuyển lead → customer/opportunity
- Validate và chống trùng

**Hiện trạng code**
- ✅ Lead entity + CRUD + import/export
- ✅ Trạng thái pipeline (New → Lost)
- ✅ Có trường ConvertedToCustomer/Opportunity
- ⚠️ Chưa có workflow ingest (webhook, form capture)
- ⚠️ Chưa có duplicate detection, auto-assign rules
- ⚠️ Chưa có lead scoring rule engine

**Cải tiến đề xuất**
- Module Lead ingestion (webhook endpoints + mapping)
- Duplicate detection + auto-merge/suppress
- Auto assignment rules (round-robin/territory)
- Lead scoring rules + audit

---

### 2.3 Opportunity / Deal
**Yêu cầu**
- Pipeline bán hàng + forecast
- Tracking thời gian bán hàng
- Tính xác suất thắng, tổng hợp giá trị deal

**Hiện trạng code**
- ✅ Pipeline + Stage + Opportunity
- ✅ Probability + WeightedAmount
- ✅ Report pipeline & sales performance
- ⚠️ Chưa có SLA/aging time (time in stage)
- ⚠️ Chưa có forecasting engine theo kỳ

**Cải tiến đề xuất**
- Lưu lịch sử stage change (OpportunityStageHistory)
- KPI: average cycle time, win rate by stage

---

### 2.4 Orders & Contracts
**Yêu cầu**
- Quotation → Order, Contract, Renewal
- Kiểm soát trạng thái Draft → Approved → Signed → Completed

**Hiện trạng code**
- ✅ Entities Quotation/Order/Contract
- ✅ Relationship ConvertedToOrder
- ⚠️ Chưa có workflow status transition rule
- ⚠️ Chưa có contract renewal automation

**Cải tiến đề xuất**
- State machine cho Quotation/Order/Contract
- Auto‑renewal scheduler + reminder

---

### 2.5 Marketing Automation
**Yêu cầu**
- Campaign + phân khúc + gửi Email/SMS/Push
- Tracking open/click/conversion, A/B testing

**Hiện trạng code**
- ✅ Campaign entity có metrics
- ✅ CommunicationTemplate & CampaignMember
- ⚠️ Chưa có segmentation engine
- ⚠️ Chưa có sending engine (SMTP/SMS/Push)
- ⚠️ Chưa có A/B testing logic

**Cải tiến đề xuất**
- Segmentation module (filters + saved segments)
- Messaging pipeline (queue + provider adapters)
- A/B testing với campaign variants

---

### 2.6 Customer Support / Ticket
**Yêu cầu**
- Ticket từ email/hotline/chat
- SLA, ưu tiên xử lý, lifecycle
- Đánh giá hài lòng

**Hiện trạng code**
- ✅ Ticket entity + CRUD + SLA model
- ✅ Status, Priority, SatisfactionRating
- ⚠️ Chưa có inbound channels (email/chat integration)
- ⚠️ Chưa có SLA enforcement engine
- ⚠️ Chưa có satisfaction survey automation

**Cải tiến đề xuất**
- SLA job: auto‑breach, escalation
- Email‑to‑ticket pipeline
- CSAT survey trigger sau khi đóng ticket

---

### 2.7 Task / Activity
**Yêu cầu**
- Lịch hẹn, nhắc việc, calendar sync, realtime reminder

**Hiện trạng code**
- ✅ Activity + Reminder entities
- ⚠️ Chưa có calendar sync (Google/Outlook)
- ⚠️ Chưa có realtime notification (SignalR/WebSocket)
- ⚠️ Chưa có scheduler để gửi reminder

**Cải tiến đề xuất**
- Background job scheduler (Hangfire/Quartz)
- Calendar integration adapters
- Notification hub

---

### 2.8 Reports & Analytics
**Yêu cầu**
- Dashboard realtime, KPI sales, phân tích khách hàng
- Export Excel/PDF
- Phân quyền xem báo cáo

**Hiện trạng code**
- ✅ ReportsController có dashboard + pipeline + conversion
- ✅ Permission ReportView
- ⚠️ Chưa có KPI sâu (segment, cohort, churn)
- ⚠️ Chưa có export PDF
- ⚠️ Dashboard chưa realtime

**Cải tiến đề xuất**
- Materialized views / snapshot tables
- Export PDF + scheduled reports
- Realtime dashboard (SignalR)

---

### 2.9 Security & RBAC
**Yêu cầu**
- RBAC theo module/field/data scope
- Audit log, OAuth/SSO

**Hiện trạng code**
- ✅ Permission‑based RBAC + AuditLog
- ✅ JWT + Refresh Token
- ⚠️ DataScope (Role.DataScope) chưa được enforced
- ⚠️ Field‑level permission chưa có
- ⚠️ OAuth/SSO chưa có

**Cải tiến đề xuất**
- Middleware/filter enforce DataScope
- Field-level masking policies
- OAuth2/OIDC integration

---

### 2.10 Integration
**Yêu cầu**
- ERP/Email/SMS/Payment/Social integrations
- Webhook, Retry/Queue, Sync

**Hiện trạng code**
- ⚠️ Chưa có integration module
- ⚠️ Không có webhook handler, queue, retry

**Cải tiến đề xuất**
- Integration module (webhook + outbound connector)
- Queue + retry (RabbitMQ/ServiceBus)

---

## 3. Non‑functional requirements
- **Performance/Realtimes**: chưa có caching hoặc realtime hub.
- **Scalability/Microservices**: hiện là monolith; cần tách bounded contexts.
- **Availability 99.9%**: chưa có health checks, failover, monitoring.
- **Security**: chưa có encryption at rest, secrets management.
- **Backup**: chưa có backup/restore strategy.

## 4. Vấn đề kỹ thuật quan trọng
- **DbContext trùng bảng trong shared DB**: MasterDbContext và TenantDbContext đang tạo trùng tables (Permissions, Roles, Users…). Cần:
  - Tách database cho Master và Tenant, hoặc
  - Dùng schema riêng (master/tenant) và map table names, hoặc
  - Loại bỏ entity trùng ra khỏi TenantDbContext.

## 5. Ưu tiên cải tiến (Roadmap đề xuất)
1. Fix kiến trúc DB multi‑tenant (tách schema/db).
2. Duplicate detection + merge Customer/Lead.
3. Workflow engine cho Lead/Opportunity/Order/Contract.
4. SLA + automation cho Ticket.
5. Segmentation + campaign automation + messaging engine.
6. DataScope enforcement + field‑level security.
7. Integration module + webhook/queue.

---

## 6. Kết luận
Backend hiện đáp ứng tốt phần **core CRUD + RBAC + multi‑tenant + audit + reporting cơ bản**, nhưng còn thiếu nhiều workflow và automation như trong requirements. Các cải tiến nêu trên là cần thiết để đạt mức “CRM SaaS hoàn chỉnh theo yêu cầu nghiệp vụ”.
