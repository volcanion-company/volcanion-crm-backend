# Contacts API Documentation

## Tổng quan Module

### Contact là gì?

**Contact (Người liên hệ)** là cá nhân cụ thể thuộc về một tổ chức/công ty khách hàng mà doanh nghiệp tương tác trong quá trình bán hàng và chăm sóc khách hàng. Một Customer (công ty) có thể có nhiều Contacts (nhân viên/đại diện).

### Sự khác biệt giữa Lead, Contact và Customer

| Khái niệm | Định nghĩa | Ví dụ |
|-----------|------------|-------|
| **Lead** | Người/tổ chức quan tâm nhưng chưa trở thành khách hàng | Ai đó điền form trên website |
| **Customer** | Tổ chức/công ty đã trở thành khách hàng chính thức | Công ty ABC Corp |
| **Contact** | Cá nhân cụ thể trong tổ chức khách hàng | Anh Nguyễn Văn A - CEO của ABC Corp |

### Tại sao cần quản lý Contact riêng biệt?

Trong bán hàng B2B, một công ty khách hàng thường có nhiều người tham gia vào quá trình mua hàng:

| Vấn đề thực tế | Giải pháp của module Contact |
|----------------|------------------------------|
| Một công ty có nhiều người liên hệ | Quản lý tất cả contacts của mỗi customer |
| Không biết ai là người quyết định chính | Đánh dấu Primary Contact |
| Mỗi người có vai trò khác nhau | Lưu JobTitle, Department |
| Cần liên hệ đúng người đúng việc | Filter theo customer, role, department |
| Theo dõi lịch sử tương tác với từng người | Liên kết với Interactions |

### Mô hình quan hệ Customer - Contact

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CUSTOMER - CONTACT MODEL                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                         CUSTOMER                                    │    │
│  │                     (ABC Corporation)                               │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                │                                            │
│                                │ Has Many                                   │
│                                ▼                                            │
│  ┌────────────────────────────────────────────────────────────────────┐     │
│  │                          CONTACTS                                  │     │
│  ├────────────────────────────────────────────────────────────────────┤     │
│  │                                                                    │     │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │     │
│  │  │ Nguyễn Văn A │  │ Trần Thị B   │  │ Lê Văn C     │              │     │
│  │  │ CEO          │  │ CFO          │  │ IT Manager   │              │     │
│  │  │ (*) PRIMARY  │  │              │  │              │              │     │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │     │
│  │                                                                    │     │
│  └────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│  Vai trò trong quy trình mua hàng:                                          │
│  • CEO (Primary) - Người phê duyệt cuối cùng                                │
│  • CFO - Người duyệt ngân sách                                              │
│  • IT Manager - Người đánh giá kỹ thuật                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Contact (Create Contact)

**Nghiệp vụ thực tế:**
- Khi gặp người mới trong quá trình làm việc với khách hàng → thêm contact
- Ghi nhận đầy đủ thông tin: họ tên, chức vụ, phòng ban, thông tin liên hệ
- Đánh dấu Primary Contact nếu là người liên hệ chính

**Ví dụ thực tế:**
> Sales đi meeting với ABC Corp, gặp thêm CFO Trần Thị B. Sau meeting, tạo contact mới:
> - FirstName: Trần, LastName: Thị B
> - JobTitle: CFO, Department: Finance
> - Email, Phone để follow-up
> - IsPrimary: false (CEO vẫn là primary)

---

### 2. Xem danh sách Contacts (Get All Contacts)

**Nghiệp vụ thực tế:**
- Xem tất cả contacts trong hệ thống hoặc của một customer cụ thể
- Tìm kiếm theo tên, email để liên hệ
- Filter theo customerId khi cần xem team của một khách hàng

**Ví dụ thực tế:**
> Sales chuẩn bị meeting với ABC Corp, filter contacts theo customerId để xem danh sách tất cả người đã từng liên hệ, biết ai cần mời họp.

---

### 3. Xem chi tiết Contact (Get Contact by ID)

**Nghiệp vụ thực tế:**
- Xem đầy đủ thông tin của một contact trước khi liên hệ
- Biết contact thuộc customer nào, chức vụ gì
- Xem ghi chú, LinkedIn để hiểu thêm về người này

**Ví dụ thực tế:**
> Trước khi gọi CFO Trần Thị B, Sales mở chi tiết contact:
> - Xem thuộc công ty ABC Corp
> - Đọc notes: "Quan tâm đến ROI, cần số liệu cụ thể"
> - Mở LinkedIn xem background để chuẩn bị câu chuyện phù hợp

---

### 4. Cập nhật Contact (Update Contact)

**Nghiệp vụ thực tế:**
- Cập nhật khi contact đổi chức vụ, số điện thoại, email
- Thay đổi Primary Contact khi có người mới phụ trách
- Cập nhật notes sau mỗi lần tương tác

**Ví dụ thực tế:**
> Anh A được thăng chức từ Sales Manager lên Sales Director:
> - Update JobTitle: "Sales Director"
> - Có thể update IsPrimary: true nếu giờ anh ta là người quyết định chính

---

### 5. Xóa Contact (Delete Contact)

**Nghiệp vụ thực tế:**
- Xóa contact khi người đó nghỉ việc, không còn liên hệ được
- Xóa contact duplicate
- Soft delete để giữ lại lịch sử cho audit

**Ví dụ thực tế:**
> Contact Lê Văn C đã nghỉ việc ở ABC Corp. Sales xóa contact để không gọi nhầm, nhưng lịch sử tương tác trước đó vẫn được giữ.

---

## Primary Contact

### Primary Contact là gì?

**Primary Contact** là người liên hệ chính của một customer - người mà bạn sẽ liên hệ đầu tiên khi cần trao đổi với công ty đó.

### Ý nghĩa nghiệp vụ

| Tình huống | Primary Contact |
|------------|-----------------|
| Gửi invoice | Gửi cho Primary Contact |
| Thông báo bảo trì | Email Primary Contact |
| Renewal reminder | Liên hệ Primary Contact trước |
| Escalation | Primary Contact là điểm leo thang |

### Best Practices

1. **Mỗi customer nên có 1 Primary Contact**
2. **Primary Contact nên là người có thẩm quyền** (Manager trở lên)
3. **Cập nhật Primary Contact khi có thay đổi nhân sự**
4. **Lưu backup contact** trong notes phòng khi Primary không liên hệ được

---

## Contact Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        CONTACT STATUS FLOW                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌────────────┐                                                             │
│  │   ACTIVE   │ ◄─── Default khi tạo mới                                    │
│  └─────┬──────┘                                                             │
│        │                                                                    │
│        ├─────────────────────────────┐                                      │
│        │                             │                                      │
│        ▼                             ▼                                      │
│  ┌────────────┐               ┌──────────────┐                              │
│  │  INACTIVE  │               │ UNSUBSCRIBED │                              │
│  └────────────┘               └──────────────┘                              │
│        │                             │                                      │
│        │ Người đó nghỉ việc          │ Không muốn nhận email marketing      │
│        │ hoặc đổi vị trí             │ (GDPR compliance)                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

| Status | Ý nghĩa | Khi nào sử dụng |
|--------|---------|-----------------|
| **Active** | Đang hoạt động, có thể liên hệ | Contact còn làm việc và chấp nhận liên hệ |
| **Inactive** | Không hoạt động | Nghỉ việc, chuyển công ty, không liên hệ được |
| **Unsubscribed** | Từ chối nhận thông tin | Đã unsubscribe khỏi email marketing |

---

## Tích hợp với các module khác

```
                    ┌──────────────┐
                    │   CUSTOMER   │
                    │  (Công ty)   │
                    └──────┬───────┘
                           │
                           │ Has Many
                           ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│    LEAD      │───►│   CONTACT    │◄───│ OPPORTUNITY  │
│  (Tiềm năng) │    │ (Người liên  │    │   (Deal)     │
└──────────────┘    │    hệ)       │    └──────────────┘
                    └──────┬───────┘
                           │
                           │ Has Many
                           ▼
                    ┌──────────────┐
                    │ INTERACTION  │
                    │ (Lịch sử     │
                    │  tương tác)  │
                    └──────────────┘
```

| Module | Quan hệ với Contact | Mô tả |
|--------|---------------------|-------|
| **Customer** | Contact thuộc Customer | Mỗi contact thuộc về một customer |
| **Interaction** | Contact có Interactions | Lịch sử email, cuộc gọi, meeting với contact |
| **Opportunity** | Contact tham gia Opportunity | Contact có thể là stakeholder trong deal |
| **Activity** | Contact trong Activity | Cuộc họp, task liên quan đến contact |

---

## Technical Overview

**Base URL:** `/api/v1/contacts`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Contacts

Lấy danh sách contacts với phân trang và filter.

```
GET /api/v1/contacts
```

**Permission Required:** `contact.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Số items mỗi trang (max: 100) |
| `sortBy` | string | No | "CreatedAt" | Field để sắp xếp |
| `sortDescending` | bool | No | false | Sắp xếp giảm dần |
| `search` | string | No | - | Tìm kiếm theo firstName, lastName, email |
| `customerId` | Guid | No | - | Filter theo customer |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "firstName": "Nguyen",
        "lastName": "Van A",
        "email": "vana@abccorp.com",
        "phone": "+84901234567",
        "jobTitle": "CEO",
        "isPrimary": true
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3,
    "totalCount": 25,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": null,
  "errors": null
}
```

---

### 2. Get Contact by ID

Lấy chi tiết một contact.

```
GET /api/v1/contacts/{id}
```

**Permission Required:** `contact.view`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Contact ID |

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerId": "customer-guid-here",
    "customerName": "ABC Corporation",
    "firstName": "Nguyen",
    "lastName": "Van A",
    "email": "vana@abccorp.com",
    "phone": "+84901234567",
    "mobile": "+84987654321",
    "jobTitle": "CEO",
    "department": "Executive",
    "isPrimary": true,
    "status": "Active",
    "addressLine1": "123 Business Street",
    "city": "Ho Chi Minh",
    "state": "HCM",
    "country": "Vietnam",
    "linkedInUrl": "https://linkedin.com/in/nguyenvana",
    "notes": "Quan tâm đến giải pháp enterprise, cần demo chi tiết",
    "createdAt": "2026-01-10T10:00:00Z"
  }
}
```

#### Error Response (404)

```json
{
  "success": false,
  "data": null,
  "errors": ["Contact with id {id} not found"]
}
```

---

### 3. Create Contact

Tạo contact mới.

```
POST /api/v1/contacts
```

**Permission Required:** `contact.create`

#### Request Body

```json
{
  "customerId": "customer-guid-here",
  "firstName": "Tran",
  "lastName": "Thi B",
  "email": "tranb@abccorp.com",
  "phone": "+84901111222",
  "mobile": "+84987111222",
  "jobTitle": "CFO",
  "department": "Finance",
  "isPrimary": false,
  "addressLine1": "123 Business Street",
  "addressLine2": "Floor 5",
  "city": "Ho Chi Minh",
  "state": "HCM",
  "postalCode": "700000",
  "country": "Vietnam",
  "linkedInUrl": "https://linkedin.com/in/tranthib",
  "notes": "Người duyệt ngân sách, cần số liệu ROI"
}
```

#### Request Schema

| Field | Type | Required | Max Length | Description |
|-------|------|----------|------------|-------------|
| `customerId` | Guid | No | - | ID của customer (có thể null nếu contact độc lập) |
| `firstName` | string | **Yes** | 100 | Tên |
| `lastName` | string | **Yes** | 100 | Họ |
| `email` | string | No | 100 | Email |
| `phone` | string | No | 20 | Số điện thoại |
| `mobile` | string | No | 20 | Di động |
| `jobTitle` | string | No | 100 | Chức danh |
| `department` | string | No | 100 | Phòng ban |
| `isPrimary` | bool | No | - | Có phải contact chính không |
| `addressLine1` | string | No | 500 | Địa chỉ dòng 1 |
| `addressLine2` | string | No | 500 | Địa chỉ dòng 2 |
| `city` | string | No | 100 | Thành phố |
| `state` | string | No | 100 | Tỉnh/Bang |
| `postalCode` | string | No | 20 | Mã bưu điện |
| `country` | string | No | 100 | Quốc gia |
| `linkedInUrl` | string | No | 200 | LinkedIn URL |
| `notes` | string | No | - | Ghi chú |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-contact-guid",
    "firstName": "Tran",
    "lastName": "Thi B",
    "email": "tranb@abccorp.com",
    "phone": "+84901111222",
    "jobTitle": "CFO",
    "isPrimary": false
  }
}
```

---

### 4. Update Contact

Cập nhật thông tin contact.

```
PUT /api/v1/contacts/{id}
```

**Permission Required:** `contact.update`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Contact ID |

#### Request Body

```json
{
  "firstName": "Tran",
  "lastName": "Thi B",
  "email": "tranb.new@abccorp.com",
  "phone": "+84901111333",
  "mobile": "+84987111333",
  "jobTitle": "CFO & COO",
  "department": "Executive",
  "isPrimary": true
}
```

#### Request Schema (All fields optional)

| Field | Type | Description |
|-------|------|-------------|
| `firstName` | string | Tên |
| `lastName` | string | Họ |
| `email` | string | Email |
| `phone` | string | Số điện thoại |
| `mobile` | string | Di động |
| `jobTitle` | string | Chức danh |
| `department` | string | Phòng ban |
| `isPrimary` | bool | Có phải contact chính không |

#### Response

```json
{
  "success": true,
  "data": {
    "id": "contact-guid",
    "firstName": "Tran",
    "lastName": "Thi B",
    "email": "tranb.new@abccorp.com",
    "phone": "+84901111333",
    "jobTitle": "CFO & COO",
    "isPrimary": true
  }
}
```

---

### 5. Delete Contact (Soft Delete)

Xóa mềm contact.

```
DELETE /api/v1/contacts/{id}
```

**Permission Required:** `contact.delete`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Contact ID |

#### Response

```json
{
  "success": true,
  "message": "Contact deleted successfully"
}
```

---

## Enums

### ContactStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | Đang hoạt động |
| 1 | Inactive | Không hoạt động |
| 2 | Unsubscribed | Đã hủy đăng ký nhận thông tin |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `contact.view` | Xem danh sách và chi tiết contact |
| `contact.create` | Tạo contact mới |
| `contact.update` | Cập nhật thông tin contact |
| `contact.delete` | Xóa contact |

---

## Error Codes

| HTTP Status | Error | Description |
|-------------|-------|-------------|
| 400 | Bad Request | Dữ liệu không hợp lệ |
| 401 | Unauthorized | Chưa đăng nhập |
| 403 | Forbidden | Không có quyền |
| 404 | Not Found | Contact không tồn tại |
| 500 | Internal Server Error | Lỗi server |

---

## Example: Complete Contact Management Flow

### 1. Tạo Contact cho Customer mới

```bash
curl -X POST "http://localhost:5000/api/v1/contacts" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-guid",
    "firstName": "Nguyen",
    "lastName": "Van A",
    "email": "vana@company.vn",
    "phone": "+84901234567",
    "jobTitle": "CEO",
    "department": "Executive",
    "isPrimary": true,
    "notes": "Người quyết định chính, thích demo trực tiếp"
  }'
```

### 2. Thêm Contact thứ hai

```bash
curl -X POST "http://localhost:5000/api/v1/contacts" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-guid",
    "firstName": "Tran",
    "lastName": "Thi B",
    "email": "tranb@company.vn",
    "phone": "+84901111222",
    "jobTitle": "IT Manager",
    "department": "IT",
    "isPrimary": false,
    "notes": "Người đánh giá kỹ thuật"
  }'
```

### 3. Lấy tất cả Contacts của Customer

```bash
curl -X GET "http://localhost:5000/api/v1/contacts?customerId={customer-guid}" \
  -H "Authorization: Bearer {token}"
```

### 4. Cập nhật Primary Contact

```bash
curl -X PUT "http://localhost:5000/api/v1/contacts/{contact-id}" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "jobTitle": "CTO",
    "isPrimary": true
  }'
```

---

## Integration Notes

### Webhook Events

Khi có thay đổi contact, hệ thống có thể gửi webhook với các events:
- `contact.created`
- `contact.updated`
- `contact.deleted`

### Audit Log

Mọi thao tác CRUD đều được ghi log với các actions:
- `Create`
- `Update`
- `SoftDelete`

### Related Entities

- **Customer**: Contact thuộc về Customer
- **Interaction**: Contact có nhiều Interactions (lịch sử tương tác)
- **Opportunity**: Contact có thể là stakeholder trong Opportunity
- **Activity**: Contact có thể tham gia Activities (meeting, task)

---

## Best Practices

### 1. Luôn liên kết Contact với Customer

Mặc dù `customerId` có thể null, trong thực tế B2B nên luôn liên kết contact với customer để:
- Dễ quản lý và tìm kiếm
- Biết contact thuộc công ty nào
- Khi xóa customer, biết cần xử lý contacts nào

### 2. Đánh dấu Primary Contact rõ ràng

- Mỗi customer chỉ nên có 1 primary contact
- Primary contact nên là người có thẩm quyền quyết định
- Cập nhật khi có thay đổi nhân sự

### 3. Ghi Notes chi tiết

Notes nên bao gồm:
- Sở thích, phong cách giao tiếp
- Những điều quan tâm (ROI, kỹ thuật, giá cả...)
- Lịch sử quan trọng (đã từng dùng sản phẩm đối thủ...)

### 4. Cập nhật Status kịp thời

- Đánh dấu Inactive khi biết người đó nghỉ việc
- Đánh dấu Unsubscribed khi họ yêu cầu không nhận email

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
