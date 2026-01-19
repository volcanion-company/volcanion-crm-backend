# Leads API Documentation

## Tổng quan Module

### Lead là gì?

**Lead (Khách hàng tiềm năng)** là một cá nhân hoặc tổ chức có biểu hiện quan tâm đến sản phẩm/dịch vụ của doanh nghiệp nhưng chưa trở thành khách hàng chính thức. Lead thường đến từ nhiều nguồn khác nhau như website, hội chợ, giới thiệu, quảng cáo...

### Tại sao cần quản lý Lead?

Trong quy trình bán hàng B2B/B2C hiện đại, việc quản lý lead hiệu quả là yếu tố then chốt:

| Vấn đề thực tế | Giải pháp của module Lead |
|----------------|---------------------------|
| Leads đến từ nhiều nguồn, dễ bị bỏ sót | Tập trung tất cả leads vào một nơi, tracking nguồn gốc |
| Không biết lead nào cần ưu tiên | Scoring và Rating giúp xếp hạng độ "nóng" của lead |
| Nhân viên sales không biết ai đang follow lead nào | Assignment system phân công rõ ràng |
| Khó theo dõi tiến độ chuyển đổi | Status workflow từ New → Converted |
| Mất dữ liệu khi chuyển lead thành khách hàng | Convert tự động tạo Customer + Opportunity, giữ liên kết |

### Vòng đời của Lead (Lead Lifecycle)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           LEAD LIFECYCLE                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────┐    ┌───────────┐    ┌───────────┐    ┌───────────┐                │
│  │ NEW  │───►│ CONTACTED │───►│ QUALIFIED │───►│ CONVERTED │                │
│  └──────┘    └───────────┘    └─────┬─────┘    └───────────┘                │
│      │                              │                │                      │
│      │                              │                ▼                      │
│      │                              │         ┌────────────┐                │
│      │                              │         │  CUSTOMER  │                │
│      │                              │         └────────────┘                │
│      │                              │                │                      │
│      │                              │                ▼                      │
│      │                              │         ┌─────────────┐               │
│      │                              │         │ OPPORTUNITY │               │
│      │                              │         └─────────────┘               │
│      │                              │                                       │
│      ▼                              ▼                                       │
│  ┌──────┐                    ┌─────────────┐                                │
│  │ LOST │                    │ UNQUALIFIED │                                │
│  └──────┘                    └─────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Lead (Create Lead)

**Nghiệp vụ thực tế:**
- Khi có người điền form trên website, gọi điện hỏi thăm, gặp ở hội chợ → tạo lead mới
- Ghi nhận đầy đủ thông tin liên hệ để follow-up sau này
- Tracking nguồn gốc (source) giúp đánh giá hiệu quả marketing

**Ví dụ thực tế:**
> Một khách hàng điền form "Liên hệ tư vấn" trên website. Hệ thống tự động tạo lead với source = Website, ghi nhận tên, email, số điện thoại và nhu cầu.

---

### 2. Xem danh sách Lead (Get All Leads)

**Nghiệp vụ thực tế:**
- Sales Manager cần xem tổng quan tất cả leads trong hệ thống
- Filter theo status để biết leads nào cần chú ý (New chưa ai liên hệ)
- Filter theo rating để ưu tiên leads "Hot" có khả năng mua cao
- Filter theo assignedTo để xem leads của từng nhân viên

**Ví dụ thực tế:**
> Trưởng phòng Sales mỗi sáng mở dashboard, filter leads có status = New để phân công cho team. Sau đó filter rating = Hot để nhắc team ưu tiên follow-up.

---

### 3. Xem chi tiết Lead (Get Lead by ID)

**Nghiệp vụ thực tế:**
- Trước khi gọi điện cho lead, sales cần xem đầy đủ thông tin
- Hiểu rõ công ty, ngành nghề, quy mô để chuẩn bị pitch phù hợp
- Xem lịch sử: ai đã liên hệ, khi nào, nói gì

**Ví dụ thực tế:**
> Sales sắp gọi cho lead ABC Corp. Mở chi tiết thấy: ngành Finance, 200 nhân viên, CEO quan tâm, estimated value 100K USD → chuẩn bị pitch cho doanh nghiệp lớn.

---

### 4. Cập nhật Lead (Update Lead)

**Nghiệp vụ thực tế:**
- Sau mỗi lần tương tác, cập nhật thông tin mới học được
- Thay đổi status phản ánh tiến độ sales process
- Điều chỉnh score/rating dựa trên phản hồi của lead

**Ví dụ thực tế:**
> Sau cuộc gọi, Sales cập nhật:
> - Status: Contacted → Qualified (đã xác nhận có nhu cầu thật)
> - Rating: Warm → Hot (rất quan tâm, muốn demo tuần sau)
> - Score: 50 → 85 (tăng điểm vì budget rõ ràng)

---

### 5. Assign Lead (Phân công Lead)

**Nghiệp vụ thực tế:**
- Leads mới cần được phân công cho sales phù hợp
- Re-assign khi sales nghỉ việc hoặc quá tải
- Đảm bảo mỗi lead có người chịu trách nhiệm

**Ví dụ thực tế:**
> Lead từ ngành Healthcare được assign cho Sales A (chuyên Healthcare). Lead từ công ty lớn 500+ nhân viên được assign cho Sales B (chuyên Enterprise).

---

### 6. Convert Lead (Chuyển đổi Lead)

**Nghiệp vụ thực tế:**
- Khi lead đồng ý mua hoặc tiến vào giai đoạn đàm phán chính thức
- Tạo Customer record để quản lý thông tin khách hàng
- Tạo Opportunity để tracking deal trong sales pipeline

**Ví dụ thực tế:**
> Lead ABC Corp đồng ý nhận proposal. Sales bấm Convert:
> - Tự động tạo Customer "ABC Corp" với đầy đủ thông tin
> - Tự động tạo Opportunity "ABC Corp - Enterprise Deal" với giá trị 100K USD
> - Lead status chuyển thành Converted, link đến Customer và Opportunity

**Lưu ý quan trọng:**
- Không thể convert lead đã converted
- Thông tin lead được copy sang Customer/Opportunity
- Opportunity được đưa vào stage đầu tiên của default pipeline

---

### 7. Delete Lead (Xóa Lead)

**Nghiệp vụ thực tế:**
- Xóa leads spam, duplicate, hoặc không còn relevant
- Soft delete: dữ liệu vẫn còn trong DB để audit
- Có thể restore nếu cần

**Ví dụ thực tế:**
> Phát hiện lead là spam bot điền form. Xóa để không làm ô nhiễm dữ liệu. Nếu xóa nhầm, admin có thể restore.

---

## Scoring & Rating System

### Lead Score (Điểm số)

Là điểm số từ 0-100 đánh giá khả năng chuyển đổi của lead dựa trên các tiêu chí:

| Tiêu chí | Điểm |
|----------|------|
| Có email doanh nghiệp (@company.com) | +10 |
| Có số điện thoại | +5 |
| Công ty 100+ nhân viên | +15 |
| Ngành phù hợp (target industry) | +10 |
| Đã xem pricing page | +20 |
| Đã tải whitepaper | +15 |
| Request demo | +25 |

### Lead Rating (Đánh giá)

| Rating | Điểm số tương ứng | Ý nghĩa nghiệp vụ |
|--------|-------------------|-------------------|
| Cold | 0-30 | Mới tiếp cận, chưa rõ nhu cầu |
| Warm | 31-70 | Có quan tâm, đang tìm hiểu |
| Hot | 71-100 | Nhu cầu rõ ràng, sẵn sàng mua |

---

## Tích hợp với các module khác

```
┌─────────────┐         Convert         ┌──────────────┐
│    LEAD     │────────────────────────►│   CUSTOMER   │
└─────────────┘                         └──────────────┘
       │                                       │
       │ Convert                               │ Has many
       ▼                                       ▼
┌─────────────┐                         ┌──────────────┐
│ OPPORTUNITY │◄────────────────────────│   CONTACTS   │
└─────────────┘                         └──────────────┘
       │
       │ In Pipeline
       ▼
┌─────────────┐
│  PIPELINE   │
│   STAGES    │
└─────────────┘
```

| Module | Quan hệ với Lead | Mô tả |
|--------|------------------|-------|
| **Customer** | Lead → Customer | Lead convert thành Customer khi qualified |
| **Opportunity** | Lead → Opportunity | Tạo deal để tracking trong pipeline |
| **Activity** | Lead có Activities | Ghi nhận cuộc gọi, email, meeting với lead |
| **Interaction** | Lead có Interactions | Lịch sử tương tác chi tiết |
| **Pipeline** | Opportunity trong Pipeline | Opportunity từ lead được đưa vào pipeline |

---

## Technical Overview

**Base URL:** `/api/v1/leads`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Leads

Lấy danh sách leads với phân trang và filter.

```
GET /api/v1/leads
```

**Permission Required:** `lead.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Số items mỗi trang (max: 100) |
| `sortBy` | string | No | "CreatedAt" | Field để sắp xếp |
| `sortDescending` | bool | No | false | Sắp xếp giảm dần |
| `search` | string | No | - | Tìm kiếm theo title, email, company |
| `status` | LeadStatus | No | - | Filter theo status |
| `rating` | LeadRating | No | - | Filter theo rating |
| `assignedTo` | Guid | No | - | Filter theo user được assign |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "title": "Lead from Website",
        "fullName": "John Doe",
        "email": "john@example.com",
        "phone": "+84123456789",
        "companyName": "ABC Corp",
        "status": "New",
        "rating": "Hot",
        "source": "Website",
        "score": 85,
        "estimatedValue": 50000.00,
        "assignedToUserName": "Sales Rep 1",
        "createdAt": "2026-01-18T10:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 48,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": null,
  "errors": null
}
```

---

### 2. Get Lead by ID

Lấy chi tiết một lead.

```
GET /api/v1/leads/{id}
```

**Permission Required:** `lead.view`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Lead ID |

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Lead from Website",
    "firstName": "John",
    "lastName": "Doe",
    "fullName": "John Doe",
    "email": "john@example.com",
    "phone": "+84123456789",
    "mobile": "+84987654321",
    "companyName": "ABC Corp",
    "jobTitle": "CEO",
    "industry": "Technology",
    "employeeCount": 50,
    "addressLine1": "123 Main Street",
    "city": "Ho Chi Minh",
    "state": "HCM",
    "country": "Vietnam",
    "status": "Qualified",
    "source": "Website",
    "sourceDetail": "Contact Form",
    "rating": "Hot",
    "score": 85,
    "estimatedValue": 50000.00,
    "description": "Interested in enterprise plan",
    "assignedToUserId": "user-guid-here",
    "assignedToUserName": "Sales Rep 1",
    "assignedAt": "2026-01-15T08:00:00Z",
    "convertedToCustomerId": null,
    "convertedAt": null,
    "createdAt": "2026-01-10T10:00:00Z",
    "updatedAt": "2026-01-18T10:00:00Z"
  }
}
```

#### Error Response (404)

```json
{
  "success": false,
  "data": null,
  "errors": ["Lead with id {id} not found"]
}
```

---

### 3. Create Lead

Tạo lead mới.

```
POST /api/v1/leads
```

**Permission Required:** `lead.create`

#### Request Body

```json
{
  "title": "New Lead from Conference",
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@company.com",
  "phone": "+84123456789",
  "mobile": "+84987654321",
  "companyName": "XYZ Inc",
  "jobTitle": "CTO",
  "industry": "Finance",
  "employeeCount": 200,
  "addressLine1": "456 Business Ave",
  "city": "Hanoi",
  "state": "HN",
  "country": "Vietnam",
  "source": 5,
  "sourceDetail": "Tech Conference 2026",
  "estimatedValue": 100000.00,
  "description": "Met at booth, very interested",
  "assignedToUserId": "user-guid-here"
}
```

#### Request Schema

| Field | Type | Required | Max Length | Description |
|-------|------|----------|------------|-------------|
| `title` | string | **Yes** | 200 | Tiêu đề lead |
| `firstName` | string | No | 100 | Tên |
| `lastName` | string | No | 100 | Họ |
| `email` | string | No | 100 | Email |
| `phone` | string | No | 20 | Số điện thoại |
| `mobile` | string | No | 20 | Di động |
| `companyName` | string | No | 200 | Tên công ty |
| `jobTitle` | string | No | 100 | Chức danh |
| `industry` | string | No | 100 | Ngành nghề |
| `employeeCount` | int | No | - | Số nhân viên |
| `addressLine1` | string | No | 500 | Địa chỉ |
| `city` | string | No | 100 | Thành phố |
| `state` | string | No | 100 | Tỉnh/Bang |
| `country` | string | No | 100 | Quốc gia |
| `source` | LeadSource | No | - | Nguồn lead (default: Website) |
| `sourceDetail` | string | No | 200 | Chi tiết nguồn |
| `estimatedValue` | decimal | No | - | Giá trị ước tính |
| `description` | string | No | - | Mô tả |
| `assignedToUserId` | Guid | No | - | ID user được assign |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-lead-guid",
    "title": "New Lead from Conference",
    "fullName": "Jane Smith",
    "email": "jane@company.com",
    "status": "New",
    "rating": "Cold",
    "score": 0,
    "createdAt": "2026-01-18T12:00:00Z"
  }
}
```

---

### 4. Update Lead

Cập nhật thông tin lead.

```
PUT /api/v1/leads/{id}
```

**Permission Required:** `lead.update`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Lead ID |

#### Request Body

```json
{
  "title": "Updated Lead Title",
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@company.com",
  "phone": "+84111222333",
  "companyName": "XYZ Corporation",
  "status": 2,
  "rating": 2,
  "score": 90,
  "estimatedValue": 150000.00,
  "description": "Updated description"
}
```

#### Request Schema (All fields optional)

| Field | Type | Description |
|-------|------|-------------|
| `title` | string | Tiêu đề lead |
| `firstName` | string | Tên |
| `lastName` | string | Họ |
| `email` | string | Email |
| `phone` | string | Số điện thoại |
| `companyName` | string | Tên công ty |
| `status` | LeadStatus | Trạng thái |
| `rating` | LeadRating | Đánh giá |
| `score` | int | Điểm số |
| `estimatedValue` | decimal | Giá trị ước tính |
| `description` | string | Mô tả |

#### Response

```json
{
  "success": true,
  "data": {
    "id": "lead-guid",
    "title": "Updated Lead Title",
    "fullName": "Jane Doe",
    "email": "jane.doe@company.com",
    "status": "Qualified",
    "rating": "Hot",
    "score": 90,
    "createdAt": "2026-01-10T10:00:00Z"
  }
}
```

---

### 5. Assign Lead

Gán lead cho user khác.

```
POST /api/v1/leads/{id}/assign
```

**Permission Required:** `lead.assign`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Lead ID |

#### Request Body

```json
{
  "userId": "target-user-guid"
}
```

#### Response

```json
{
  "success": true,
  "message": "Lead assigned successfully"
}
```

---

### 6. Convert Lead

Chuyển đổi lead thành Customer và tùy chọn tạo Opportunity.

```
POST /api/v1/leads/{id}/convert
```

**Permission Required:** `lead.convert`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Lead ID |

#### Request Body

```json
{
  "customerName": "ABC Corporation",
  "createOpportunity": true,
  "opportunityName": "Enterprise Deal - ABC Corp"
}
```

#### Request Schema

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `customerName` | string | No | CompanyName hoặc FullName | Tên customer mới |
| `createOpportunity` | bool | No | true | Có tạo opportunity không |
| `opportunityName` | string | No | Auto-generated | Tên opportunity |

#### Response

```json
{
  "success": true,
  "data": {
    "customerId": "new-customer-guid",
    "opportunityId": "new-opportunity-guid"
  }
}
```

#### Error Response (400 - Already Converted)

```json
{
  "success": false,
  "data": null,
  "errors": ["Lead is already converted"]
}
```

---

### 7. Delete Lead (Soft Delete)

Xóa mềm lead.

```
DELETE /api/v1/leads/{id}
```

**Permission Required:** `lead.delete`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Lead ID |

#### Response

```json
{
  "success": true,
  "message": "Lead deleted successfully"
}
```

---

## Enums

### LeadStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | New | Lead mới |
| 1 | Contacted | Đã liên hệ |
| 2 | Qualified | Đủ điều kiện |
| 3 | Unqualified | Không đủ điều kiện |
| 4 | Converted | Đã chuyển đổi |
| 5 | Lost | Đã mất |

### LeadSource

| Value | Name | Description |
|-------|------|-------------|
| 0 | Website | Từ website |
| 1 | Referral | Giới thiệu |
| 2 | SocialMedia | Mạng xã hội |
| 3 | Email | Email marketing |
| 4 | Phone | Điện thoại |
| 5 | TradeShow | Hội chợ/Triển lãm |
| 6 | Partner | Đối tác |
| 7 | Advertisement | Quảng cáo |
| 8 | ColdCall | Cold call |
| 9 | Other | Khác |

### LeadRating

| Value | Name | Description |
|-------|------|-------------|
| 0 | Cold | Lạnh - Ít quan tâm |
| 1 | Warm | Ấm - Quan tâm vừa |
| 2 | Hot | Nóng - Rất quan tâm |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `lead.view` | Xem danh sách và chi tiết lead |
| `lead.create` | Tạo lead mới |
| `lead.update` | Cập nhật thông tin lead |
| `lead.assign` | Gán lead cho user khác |
| `lead.convert` | Chuyển đổi lead thành customer |
| `lead.delete` | Xóa lead |

---

## Error Codes

| HTTP Status | Error | Description |
|-------------|-------|-------------|
| 400 | Bad Request | Dữ liệu không hợp lệ |
| 401 | Unauthorized | Chưa đăng nhập |
| 403 | Forbidden | Không có quyền |
| 404 | Not Found | Lead không tồn tại |
| 500 | Internal Server Error | Lỗi server |

---

## Example: Complete Lead Lifecycle

### 1. Tạo Lead

```bash
curl -X POST "http://localhost:5000/api/v1/leads" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Website Inquiry - Enterprise",
    "firstName": "Nguyen",
    "lastName": "Van A",
    "email": "vana@company.vn",
    "phone": "+84901234567",
    "companyName": "Tech Vietnam",
    "industry": "Technology",
    "source": 0,
    "estimatedValue": 50000
  }'
```

### 2. Assign cho Sales Rep

```bash
curl -X POST "http://localhost:5000/api/v1/leads/{leadId}/assign" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"userId": "sales-rep-user-id"}'
```

### 3. Update Status sau khi liên hệ

```bash
curl -X PUT "http://localhost:5000/api/v1/leads/{leadId}" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "status": 2,
    "rating": 2,
    "score": 85
  }'
```

### 4. Convert thành Customer và Opportunity

```bash
curl -X POST "http://localhost:5000/api/v1/leads/{leadId}/convert" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "createOpportunity": true,
    "opportunityName": "Tech Vietnam - Enterprise Deal"
  }'
```

---

## Integration Notes

### Webhook Events

Khi có thay đổi lead, hệ thống có thể gửi webhook với các events:
- `lead.created`
- `lead.updated`
- `lead.assigned`
- `lead.converted`
- `lead.deleted`

### Audit Log

Mọi thao tác CRUD đều được ghi log với các actions:
- `Create`
- `Update`
- `Assign`
- `Convert`
- `SoftDelete`

### Related Entities

- **Customer**: Lead có thể convert thành Customer
- **Opportunity**: Lead có thể convert thành Opportunity
- **Activity**: Lead có thể có nhiều Activities
- **Interaction**: Lead có thể có nhiều Interactions

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
