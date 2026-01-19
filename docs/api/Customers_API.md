# Customers API Documentation

## Tổng quan Module

### Customer là gì?

**Customer (Khách hàng)** là cá nhân hoặc tổ chức đã chính thức trở thành khách hàng của doanh nghiệp - nghĩa là đã có giao dịch, hợp đồng hoặc cam kết sử dụng sản phẩm/dịch vụ. Customer là trung tâm của mọi hoạt động CRM.

### Hành trình từ Lead đến Customer

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CUSTOMER ACQUISITION JOURNEY                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────┐      ┌───────────┐      ┌────────────┐      ┌──────────────┐  │
│  │  LEAD    │ ───► │ QUALIFIED │ ───► │ OPPORTUNITY│ ───► │   CUSTOMER   │  │
│  │ (Tiềm    │      │   LEAD    │      │   (Deal)   │      │  (Khách hàng │  │
│  │  năng)   │      │           │      │            │      │   chính thức)│  │
│  └──────────┘      └───────────┘      └────────────┘      └──────────────┘  │
│       │                                                          │          │
│       │ Chưa xác minh                                            │ Đã mua   │
│       │ nhu cầu                                                  │ hàng     │
│       │                                                          ▼          │
│       │                                                   ┌──────────────┐  │
│       │                                                   │   ORDERS     │  │
│       │                                                   │  CONTRACTS   │  │
│       │                                                   │   TICKETS    │  │
│       │                                                   └──────────────┘  │
│       ▼                                                                     │
│  ┌──────────┐                                                               │
│  │   LOST   │ Không quan tâm / Chọn đối thủ                                 │
│  └──────────┘                                                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Tại sao Customer là trung tâm của CRM?

Customer là entity quan trọng nhất vì mọi hoạt động kinh doanh đều xoay quanh khách hàng:

| Vấn đề thực tế | Giải pháp của module Customer |
|----------------|-------------------------------|
| Không có cái nhìn 360° về khách hàng | Tập trung tất cả thông tin vào một profile |
| Khó theo dõi lịch sử giao dịch | Liên kết với Orders, Contracts, Tickets |
| Không biết giá trị khách hàng | LifetimeValue tính tổng giá trị mua hàng |
| Phân loại khách hàng không rõ ràng | Type (Individual/Business), Status, Source |
| Mất khách không biết lý do | Status tracking: Active → Inactive → Churned |

### Customer 360° View

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CUSTOMER 360° VIEW                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                        ┌─────────────────┐                                  │
│                        │    CUSTOMER     │                                  │
│                        │   ABC Corp      │                                  │
│                        │  Type: Business │                                  │
│                        │ Status: Active  │                                  │
│                        └────────┬────────┘                                  │
│                                 │                                           │
│       ┌──────────┬──────────┬──┴───┬──────────┬──────────┬──────────┐       │
│       ▼          ▼          ▼      ▼          ▼          ▼          ▼       │
│  ┌─────────┐ ┌────────┐ ┌──────┐ ┌──────┐ ┌────────┐ ┌────────┐ ┌───────┐   │
│  │CONTACTS │ │ LEADS  │ │OPPOR-│ │ORDERS│ │CONTRACT│ │TICKETS │ │INTER- │   │
│  │         │ │        │ │TUNITY│ │      │ │        │ │        │ │ACTION │   │
│  │ 5 người │ │3 leads │ │2 deal│ │15 đơn│ │2 HĐ    │ │8 ticket│ │50 lần │   │
│  └─────────┘ └────────┘ └──────┘ └──────┘ └────────┘ └────────┘ └───────┘   │
│                                                                             │
│  Thông tin tổng hợp:                                                        │
│  • Lifetime Value: 500,000 USD                                              │
│  • Đang có 2 opportunities đang chạy                                        │
│  • 8 tickets, 6 đã giải quyết                                               │
│  • Primary Contact: CEO Nguyễn Văn A                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Phân loại Customer

### 1. Theo Type (Loại khách hàng)

| Type | Mô tả | Ví dụ | Fields đặc trưng |
|------|-------|-------|------------------|
| **Individual** | Cá nhân, khách hàng lẻ | Anh Nguyễn Văn A mua laptop | FirstName, LastName, DateOfBirth |
| **Business** | Doanh nghiệp, tổ chức | Công ty ABC mua 100 licenses | CompanyName, TaxId, Industry, EmployeeCount |

### 2. Theo Status (Trạng thái)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        CUSTOMER STATUS LIFECYCLE                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────────┐        ┌──────────┐        ┌───────────┐                     │
│  │ PROSPECT  │ ─────► │  ACTIVE  │ ─────► │ INACTIVE  │                     │
│  └───────────┘        └────┬─────┘        └─────┬─────┘                     │
│       │                    │                    │                           │
│       │ Chưa mua           │ Đang mua           │ Ngừng mua                 │
│       │ chính thức         │ sử dụng            │ tạm thời                  │
│       │                    │                    │                           │
│       │                    │                    ▼                           │
│       │                    │              ┌───────────┐                     │
│       │                    │              │  CHURNED  │                     │
│       │                    │              └───────────┘                     │
│       │                    │                    │                           │
│       │                    │                    │ Rời bỏ                    │
│       │                    │                    │ hoàn toàn                 │
│       │                    │                    │                           │
│       │                    ▼                    │                           │
│       │              ┌───────────┐              │                           │
│       └─────────────►│  WINBACK  │◄─────────────┘                           │
│                      └───────────┘                                          │
│                            │                                                │
│                            │ Quay lại mua                                   │
│                            ▼                                                │
│                      ┌───────────┐                                          │
│                      │  ACTIVE   │                                          │
│                      └───────────┘                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

| Status | Ý nghĩa | Hành động phù hợp |
|--------|---------|-------------------|
| **Prospect** | Tiềm năng, chưa mua chính thức | Nurturing, gửi tài liệu, demo |
| **Active** | Đang hoạt động, mua hàng đều | Upsell, cross-sell, chăm sóc |
| **Inactive** | Ngừng mua một thời gian | Re-engagement campaign |
| **Churned** | Đã rời bỏ, không còn là khách | Win-back program, phân tích nguyên nhân |

### 3. Theo Source (Nguồn)

| Source | Mô tả | Ý nghĩa với Marketing |
|--------|-------|----------------------|
| **Direct** | Khách tìm đến trực tiếp | Brand awareness tốt |
| **Referral** | Được giới thiệu | Referral program hiệu quả |
| **Website** | Từ website | SEO/Content marketing tốt |
| **SocialMedia** | Từ mạng xã hội | Social marketing hiệu quả |
| **Advertisement** | Từ quảng cáo | Paid ads ROI tốt |
| **TradeShow** | Từ hội chợ/triển lãm | Events marketing hiệu quả |
| **Partner** | Từ đối tác | Partner program tốt |
| **Other** | Nguồn khác | Cần phân tích thêm |

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Customer (Create Customer)

**Nghiệp vụ thực tế:**
- Khi lead convert thành customer (tự động hoặc thủ công)
- Khi có khách hàng mới mua hàng trực tiếp
- Import từ hệ thống cũ khi migrate

**Ví dụ thực tế:**
> Lead ABC Corp đồng ý ký hợp đồng. Sales bấm Convert Lead → Hệ thống tự động tạo Customer với:
> - Name: ABC Corporation
> - Type: Business
> - Source: Website (từ Lead)
> - Status: Active
> - Tất cả thông tin từ Lead được copy sang

---

### 2. Xem danh sách Customers (Get All Customers)

**Nghiệp vụ thực tế:**
- Xem tổng quan tất cả khách hàng trong hệ thống
- Filter theo type để phân biệt B2B và B2C
- Filter theo status để ưu tiên khách hàng active
- Tìm kiếm nhanh theo tên, email, mã khách hàng

**Ví dụ thực tế:**
> Account Manager cần xem danh sách khách hàng Active trong ngành Technology để chuẩn bị chiến dịch upsell phần mềm mới.

---

### 3. Xem chi tiết Customer (Get Customer by ID)

**Nghiệp vụ thực tế:**
- Xem toàn bộ thông tin 360° của khách hàng
- Biết ai là contacts, lịch sử mua hàng
- Chuẩn bị trước khi meeting hoặc gọi điện

**Ví dụ thực tế:**
> Trước cuộc họp quarterly review với ABC Corp, Account Manager mở Customer detail:
> - Xem Lifetime Value: 500K USD
> - Xem danh sách 5 contacts, Primary là CEO
> - Xem 2 opportunities đang chạy
> - Đọc notes: "Quan tâm đến giải pháp AI mới"

---

### 4. Cập nhật Customer (Update Customer)

**Nghiệp vụ thực tế:**
- Cập nhật thông tin khi có thay đổi (địa chỉ, email, người phụ trách)
- Thay đổi status khi khách ngừng mua hoặc quay lại
- Cập nhật notes sau mỗi lần tương tác quan trọng

**Ví dụ thực tế:**
> Khách hàng ABC Corp thông báo chuyển văn phòng:
> - Update AddressLine1, City
> - Update notes: "Đã chuyển VP mới, liên hệ số mới"
> - Thông báo cho các bộ phận liên quan

---

### 5. Xóa Customer (Delete Customer)

**Nghiệp vụ thực tế:**
- Xóa customer duplicate hoặc tạo nhầm
- Xóa customer yêu cầu xóa dữ liệu (GDPR)
- Soft delete để giữ lịch sử cho báo cáo

**Ví dụ thực tế:**
> Phát hiện có 2 records cho cùng 1 công ty (do nhập trùng). Merge dữ liệu vào 1 record, xóa record duplicate.

---

## Customer Lifetime Value (CLV)

### CLV là gì?

**Customer Lifetime Value (Giá trị trọn đời khách hàng)** là tổng giá trị mà một khách hàng mang lại cho doanh nghiệp trong suốt thời gian họ là khách hàng.

### Công thức tính

```
CLV = Tổng giá trị đơn hàng + Giá trị hợp đồng - Chi phí phục vụ
```

### Ý nghĩa nghiệp vụ

| CLV Range | Phân loại | Chiến lược |
|-----------|-----------|------------|
| $0 - $1,000 | Standard | Self-service, automation |
| $1,000 - $10,000 | Premium | Dedicated support, upsell |
| $10,000 - $100,000 | Enterprise | Account Manager, custom solution |
| $100,000+ | Strategic | C-level engagement, partnership |

### Ví dụ thực tế

> Customer ABC Corp có:
> - 15 đơn hàng, tổng $200,000
> - 2 hợp đồng năm, $100,000/năm x 2 năm = $200,000
> - **CLV = $400,000** → Strategic customer, cần C-level engagement

---

## Customer Code

### Mục đích

**Customer Code** là mã định danh duy nhất cho khách hàng, thường dùng để:
- Tích hợp với hệ thống ERP/Accounting
- In trên hóa đơn, hợp đồng
- Tra cứu nhanh

### Format thường dùng

| Format | Ví dụ | Mô tả |
|--------|-------|-------|
| Tuần tự | C00001, C00002 | Đơn giản, dễ quản lý |
| Theo năm | C2026-0001 | Biết năm tạo customer |
| Theo type | IND-001, BUS-001 | Phân biệt Individual/Business |
| Theo khu vực | VN-HCM-001 | Phân biệt theo địa lý |

---

## Tích hợp với các module khác

```
                              ┌───────────────┐
                              │   CUSTOMER    │
                              │   (Trung tâm) │
                              └───────┬───────┘
                                      │
        ┌─────────────────────────────┼─────────────────────────────┐
        │           │           │     │     │           │           │
        ▼           ▼           ▼     │     ▼           ▼           ▼
  ┌──────────┐┌──────────┐┌──────────┐│┌──────────┐┌──────────┐┌──────────┐
  │ CONTACTS ││  LEADS   ││OPPORTUNI-│││  ORDERS  ││CONTRACTS ││ TICKETS  │
  │          ││          ││   TIES   │││          ││          ││          │
  │ Người    ││ Nguồn    ││ Deals    │││ Đơn hàng ││ Hợp đồng ││ Hỗ trợ   │
  │ liên hệ  ││ khách    ││ đang     │││          ││          ││          │
  └──────────┘└──────────┘└──────────┘│└──────────┘└──────────┘└──────────┘
                                      │
                                      ▼
                              ┌───────────────┐
                              │ INTERACTIONS  │
                              │ QUOTATIONS    │
                              │ CAMPAIGNS     │
                              └───────────────┘
```

| Module | Quan hệ | Mô tả |
|--------|---------|-------|
| **Contacts** | Customer có nhiều Contacts | Danh sách người liên hệ của customer |
| **Leads** | Lead convert thành Customer | Nguồn gốc của customer |
| **Opportunities** | Customer có nhiều Opportunities | Các deal đang chạy |
| **Orders** | Customer có nhiều Orders | Lịch sử đơn hàng |
| **Contracts** | Customer có nhiều Contracts | Các hợp đồng |
| **Tickets** | Customer có nhiều Tickets | Yêu cầu hỗ trợ |
| **Quotations** | Customer có nhiều Quotations | Báo giá đã gửi |
| **Interactions** | Customer có nhiều Interactions | Lịch sử tương tác |

---

## Technical Overview

**Base URL:** `/api/v1/customers`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Customers

Lấy danh sách customers với phân trang và filter.

```
GET /api/v1/customers
```

**Permission Required:** `customer.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Số items mỗi trang (max: 100) |
| `sortBy` | string | No | "CreatedAt" | Field để sắp xếp |
| `sortDescending` | bool | No | false | Sắp xếp giảm dần |
| `search` | string | No | - | Tìm kiếm theo name, email, customerCode |
| `type` | CustomerType | No | - | Filter theo type |
| `status` | CustomerStatus | No | - | Filter theo status |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "ABC Corporation",
        "type": "Business",
        "email": "contact@abccorp.com",
        "phone": "+84901234567",
        "status": "Active",
        "source": "Website",
        "customerCode": "C2026-0001",
        "assignedToUserName": "Account Manager 1",
        "createdAt": "2026-01-10T10:00:00Z"
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

### 2. Get Customer by ID

Lấy chi tiết một customer với đầy đủ thông tin 360°.

```
GET /api/v1/customers/{id}
```

**Permission Required:** `customer.view`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Customer ID |

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "ABC Corporation",
    "type": "Business",
    "email": "contact@abccorp.com",
    "phone": "+84901234567",
    "mobile": "+84987654321",
    "website": "https://abccorp.com",
    "firstName": null,
    "lastName": null,
    "title": null,
    "dateOfBirth": null,
    "companyName": "ABC Corporation",
    "taxId": "0123456789",
    "industry": "Technology",
    "employeeCount": 200,
    "annualRevenue": 5000000.00,
    "addressLine1": "123 Business Street",
    "addressLine2": "Floor 10",
    "city": "Ho Chi Minh",
    "state": "HCM",
    "postalCode": "700000",
    "country": "Vietnam",
    "status": "Active",
    "source": "Website",
    "sourceDetail": "Contact Form",
    "customerCode": "C2026-0001",
    "lifetimeValue": 500000.00,
    "notes": "Strategic customer, C-level engagement required",
    "assignedToUserId": "user-guid-here",
    "assignedToUserName": "Account Manager 1",
    "contacts": [
      {
        "id": "contact-guid-1",
        "firstName": "Nguyen",
        "lastName": "Van A",
        "email": "vana@abccorp.com",
        "phone": "+84901111111",
        "jobTitle": "CEO",
        "isPrimary": true
      },
      {
        "id": "contact-guid-2",
        "firstName": "Tran",
        "lastName": "Thi B",
        "email": "tranb@abccorp.com",
        "phone": "+84902222222",
        "jobTitle": "CFO",
        "isPrimary": false
      }
    ],
    "createdAt": "2026-01-10T10:00:00Z",
    "updatedAt": "2026-01-15T14:00:00Z"
  }
}
```

---

### 3. Create Customer

Tạo customer mới.

```
POST /api/v1/customers
```

**Permission Required:** `customer.create`

#### Request Body

```json
{
  "name": "XYZ Technology",
  "type": 1,
  "email": "info@xyztech.com",
  "phone": "+84901234567",
  "mobile": "+84987654321",
  "website": "https://xyztech.com",
  "companyName": "XYZ Technology JSC",
  "taxId": "0987654321",
  "industry": "Software",
  "employeeCount": 50,
  "annualRevenue": 1000000.00,
  "addressLine1": "456 Tech Park",
  "city": "Hanoi",
  "state": "HN",
  "postalCode": "100000",
  "country": "Vietnam",
  "status": 1,
  "source": 2,
  "sourceDetail": "Google Search",
  "customerCode": "C2026-0002",
  "assignedToUserId": "user-guid-here",
  "notes": "New enterprise customer, interested in full suite"
}
```

#### Request Schema

| Field | Type | Required | Max Length | Description |
|-------|------|----------|------------|-------------|
| `name` | string | **Yes** | 200 | Tên khách hàng |
| `type` | CustomerType | No | - | Loại (Individual=0, Business=1) |
| `email` | string | No | 100 | Email |
| `phone` | string | No | 20 | Số điện thoại |
| `mobile` | string | No | 20 | Di động |
| `website` | string | No | 500 | Website |
| `firstName` | string | No | 100 | Tên (Individual) |
| `lastName` | string | No | 100 | Họ (Individual) |
| `title` | string | No | 50 | Danh xưng (Mr/Mrs/Dr) |
| `dateOfBirth` | DateTime | No | - | Ngày sinh (Individual) |
| `companyName` | string | No | 200 | Tên công ty (Business) |
| `taxId` | string | No | 50 | Mã số thuế |
| `industry` | string | No | 100 | Ngành nghề |
| `employeeCount` | int | No | - | Số nhân viên |
| `annualRevenue` | decimal | No | - | Doanh thu năm |
| `addressLine1` | string | No | 500 | Địa chỉ dòng 1 |
| `addressLine2` | string | No | 500 | Địa chỉ dòng 2 |
| `city` | string | No | 100 | Thành phố |
| `state` | string | No | 100 | Tỉnh/Bang |
| `postalCode` | string | No | 20 | Mã bưu điện |
| `country` | string | No | 100 | Quốc gia |
| `status` | CustomerStatus | No | - | Trạng thái (default: Active) |
| `source` | CustomerSource | No | - | Nguồn (default: Direct) |
| `sourceDetail` | string | No | 100 | Chi tiết nguồn |
| `customerCode` | string | No | 50 | Mã khách hàng |
| `assignedToUserId` | Guid | No | - | ID người phụ trách |
| `notes` | string | No | - | Ghi chú |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-customer-guid",
    "name": "XYZ Technology",
    "type": "Business",
    "email": "info@xyztech.com",
    "phone": "+84901234567",
    "status": "Active",
    "source": "Website",
    "customerCode": "C2026-0002",
    "assignedToUserName": "Account Manager 1",
    "createdAt": "2026-01-18T12:00:00Z"
  },
  "message": "Customer created successfully"
}
```

---

### 4. Update Customer

Cập nhật thông tin customer.

```
PUT /api/v1/customers/{id}
```

**Permission Required:** `customer.update`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Customer ID |

#### Request Body (All fields optional)

```json
{
  "name": "XYZ Technology Corporation",
  "email": "contact@xyztech.com",
  "phone": "+84901111222",
  "industry": "Enterprise Software",
  "status": 1,
  "notes": "Upgraded to enterprise tier"
}
```

#### Response

```json
{
  "success": true,
  "data": {
    "id": "customer-guid",
    "name": "XYZ Technology Corporation",
    "type": "Business",
    "email": "contact@xyztech.com",
    "phone": "+84901111222",
    "status": "Active",
    "source": "Website",
    "customerCode": "C2026-0002",
    "assignedToUserName": "Account Manager 1",
    "createdAt": "2026-01-18T12:00:00Z"
  },
  "message": "Customer updated successfully"
}
```

---

### 5. Delete Customer (Soft Delete)

Xóa mềm customer.

```
DELETE /api/v1/customers/{id}
```

**Permission Required:** `customer.delete`

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Customer ID |

#### Response

```json
{
  "success": true,
  "message": "Customer deleted successfully"
}
```

---

## Enums

### CustomerType

| Value | Name | Description |
|-------|------|-------------|
| 0 | Individual | Cá nhân |
| 1 | Business | Doanh nghiệp |

### CustomerStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Prospect | Tiềm năng |
| 1 | Active | Đang hoạt động |
| 2 | Inactive | Không hoạt động |
| 3 | Churned | Đã rời bỏ |

### CustomerSource

| Value | Name | Description |
|-------|------|-------------|
| 0 | Direct | Trực tiếp |
| 1 | Referral | Giới thiệu |
| 2 | Website | Từ website |
| 3 | SocialMedia | Mạng xã hội |
| 4 | Advertisement | Quảng cáo |
| 5 | TradeShow | Hội chợ/Triển lãm |
| 6 | Partner | Đối tác |
| 7 | Other | Khác |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `customer.view` | Xem danh sách và chi tiết customer |
| `customer.create` | Tạo customer mới |
| `customer.update` | Cập nhật thông tin customer |
| `customer.delete` | Xóa customer |

---

## Error Codes

| HTTP Status | Error | Description |
|-------------|-------|-------------|
| 400 | Bad Request | Dữ liệu không hợp lệ |
| 401 | Unauthorized | Chưa đăng nhập |
| 403 | Forbidden | Không có quyền |
| 404 | Not Found | Customer không tồn tại |
| 500 | Internal Server Error | Lỗi server |

---

## Example: Complete Customer Management Flow

### 1. Tạo Business Customer

```bash
curl -X POST "http://localhost:5000/api/v1/customers" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "ABC Corporation",
    "type": 1,
    "email": "info@abccorp.com",
    "phone": "+84901234567",
    "companyName": "ABC Corporation",
    "taxId": "0123456789",
    "industry": "Technology",
    "employeeCount": 200,
    "status": 1,
    "source": 2,
    "customerCode": "C2026-0001",
    "notes": "Enterprise customer from website inquiry"
  }'
```

### 2. Tạo Individual Customer

```bash
curl -X POST "http://localhost:5000/api/v1/customers" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Nguyen Van A",
    "type": 0,
    "firstName": "Nguyen",
    "lastName": "Van A",
    "email": "vana@email.com",
    "phone": "+84987654321",
    "dateOfBirth": "1990-01-15",
    "status": 1,
    "source": 0,
    "customerCode": "C2026-0002"
  }'
```

### 3. Lấy danh sách Active Business Customers

```bash
curl -X GET "http://localhost:5000/api/v1/customers?type=1&status=1" \
  -H "Authorization: Bearer {token}"
```

### 4. Cập nhật Customer Status

```bash
curl -X PUT "http://localhost:5000/api/v1/customers/{customer-id}" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "status": 2,
    "notes": "Customer not responding, marked as Inactive"
  }'
```

---

## Integration Notes

### Webhook Events

Khi có thay đổi customer, hệ thống có thể gửi webhook với các events:
- `customer.created`
- `customer.updated`
- `customer.deleted`

### Audit Log

Mọi thao tác CRUD đều được ghi log với các actions:
- `Create` - Kèm theo newValues
- `Update` - Kèm theo oldValues và newValues
- `SoftDelete`

### Related Entities

- **Contacts**: Danh sách người liên hệ của customer
- **Leads**: Lead nguồn gốc (nếu convert từ lead)
- **Opportunities**: Các deals đang chạy
- **Orders**: Lịch sử đơn hàng
- **Contracts**: Các hợp đồng
- **Tickets**: Yêu cầu hỗ trợ
- **Quotations**: Báo giá đã gửi
- **Interactions**: Lịch sử tương tác

---

## Best Practices

### 1. Sử dụng Customer Type đúng cách

- **Individual**: Sử dụng cho B2C, khách hàng cá nhân
- **Business**: Sử dụng cho B2B, khách hàng doanh nghiệp

### 2. Theo dõi Customer Status

- Cập nhật status kịp thời khi có thay đổi
- Sử dụng Inactive trước khi Churned (cho cơ hội win-back)
- Phân tích nguyên nhân Churned để cải thiện retention

### 3. Tính Lifetime Value chính xác

- Cập nhật sau mỗi đơn hàng/hợp đồng mới
- Sử dụng để phân loại và ưu tiên customers
- Báo cáo CLV theo segment để đánh giá hiệu quả

### 4. Assign đúng người phụ trách

- Mỗi customer nên có một Account Manager
- Re-assign khi nhân viên nghỉ việc
- Cân bằng số lượng customers cho mỗi AM

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
