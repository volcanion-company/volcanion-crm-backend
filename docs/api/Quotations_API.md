# Quotations API Documentation

## Tổng quan Module

### Quotation là gì?

**Quotation (Báo giá)** là đề xuất chính thức về giá cả, điều khoản và sản phẩm/dịch vụ gửi cho khách hàng. Đây là bước quan trọng trước khi close deal - customer review báo giá và quyết định accept hay reject.

### Vị trí của Quotation trong Sales Process

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        QUOTATION IN SALES FLOW                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐    ┌─────────────┐    ┌───────┐    ┌──────────┐         │
│  │ OPPORTUNITY  │───►│  QUOTATION  │───►│ ORDER │───►│ CONTRACT │         │
│  │              │    │             │    │       │    │          │         │
│  │ Đang đàm     │    │  Gửi báo   │    │ Chốt  │    │ Thực     │         │
│  │ phán         │    │  giá       │    │ deal  │    │ hiện     │         │
│  └──────────────┘    └──────┬──────┘    └───────┘    └──────────┘         │
│                             │                                               │
│                             │                                               │
│                      ┌──────┴──────┐                                        │
│                      │             │                                        │
│                 ┌────▼─────┐  ┌───▼─────┐                                  │
│                 │ ACCEPTED │  │REJECTED │                                  │
│                 │          │  │         │                                  │
│                 │ → Order  │  │ → Lost  │                                  │
│                 └──────────┘  └─────────┘                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Quotation vs Proposal vs Invoice

| Khái niệm | Mục đích | Legally Binding | Ví dụ |
|-----------|----------|-----------------|-------|
| **Quotation** | Đề xuất giá chính thức | ✅ Yes (có expiry date) | "Báo giá $50K cho ABC Corp, valid 30 days" |
| **Proposal** | Đề xuất giải pháp | ❌ No | "Đề xuất CRM implementation plan" |
| **Invoice** | Yêu cầu thanh toán | ✅ Yes | "Invoice #INV-123 due in 30 days" |
| **Order** | Xác nhận mua | ✅ Yes | "Order confirmed, proceed delivery" |

---

## Tại sao cần Quotation Management?

### Vấn đề thực tế

| Vấn đề | Giải pháp của Quotation Module |
|--------|-------------------------------|
| Gửi quote qua email, không track được | Central quote repository với status tracking |
| Quote hết hạn nhưng không biết | Auto expiry date, expiry notifications |
| Không biết quote nào được accept | Status: Accepted/Rejected/Expired |
| Tính giá thủ công, dễ sai | Auto-calculation: items, discount, tax, total |
| Quote và Order không match | Convert Quotation → Order với 1 click |
| Không có revision history | Version control, track changes |

---

## Quotation Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     QUOTATION STATUS FLOW                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────┐    ┌──────┐    ┌──────────┐                                     │
│  │ DRAFT │───►│ SENT │───►│ ACCEPTED │──────┐                              │
│  └───┬───┘    └───┬──┘    └──────────┘      │                              │
│      │            │                          │                              │
│      │            │                          ▼                              │
│      │            │                    ┌───────────┐                        │
│      │            │                    │ CONVERTED │                        │
│      │            │                    │           │                        │
│      │            │                    │ to Order  │                        │
│      │            │                    └───────────┘                        │
│      │            │                                                         │
│      │            ▼                                                         │
│      │        ┌──────────┐                                                  │
│      │        │ REJECTED │                                                  │
│      │        └──────────┘                                                  │
│      │            │                                                         │
│      │            ▼                                                         │
│      │        (Update Opportunity to Lost)                                 │
│      │                                                                      │
│      ▼            ▼                                                         │
│  ┌─────────┐  ┌─────────┐                                                  │
│  │ EXPIRED │  │ EXPIRED │                                                  │
│  │(if > 30)│  │ (auto)  │                                                  │
│  └─────────┘  └─────────┘                                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Chi tiết từng Status

| Status | Ý nghĩa | Action tiếp theo | Expiry |
|--------|---------|------------------|--------|
| **Draft** | Đang soạn, chưa gửi | Send hoặc Delete | - |
| **Sent** | Đã gửi cho customer | Wait for Accept/Reject | ✅ Có (30 days) |
| **Accepted** | Customer chấp thuận | Convert to Order | - |
| **Rejected** | Customer từ chối | Review, revise quote | - |
| **Expired** | Quá hạn, chưa response | Follow up hoặc close | - |
| **Converted** | Đã chuyển thành Order | - | - |

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Quotation (Create Quotation)

**Nghiệp vụ thực tế:**
- Sales Rep đang negotiate với customer về giá
- Opportunity ở stage "Proposal/Quote"
- Customer request formal quote

**Ví dụ thực tế:**
> Sales Rep đang close deal với ABC Corp - Opportunity $50K:
> - Customer yêu cầu: "Gửi báo giá chính thức"
> - Sales tạo Quotation:
>   * Link to Opportunity "ABC Corp - CRM Implementation"
>   * Add items: 50 user licenses @ $100 = $5,000
>   * Add services: Training 20h @ $150 = $3,000
>   * Apply discount: 10% for volume
>   * Set expiry: 30 days from today
>   * Status: Draft (Sales Manager chưa review)

---

### 2. Xem danh sách Quotations (Get All Quotations)

**Nghiệp vụ thực tế:**
- Sales Manager review tất cả quotes pending
- Finance check quotes đã accept để prepare orders
- Sales Rep track quotes đã gửi

**Ví dụ thực tế:**
> Sales Manager mỗi tuần:
> - Filter Status = Sent → 12 quotes đang chờ customer response
> - Filter ExpiryDate < 7 days → 3 quotes sắp hết hạn → Follow up urgent
> - Filter Status = Accepted → 5 quotes cần convert to Order

---

### 3. Xem chi tiết Quotation (Get Quotation by ID)

**Nghiệp vụ thực tế:**
- Customer service trả lời câu hỏi về quote
- Sales Rep review trước khi gửi
- Customer xem online quote portal

**Ví dụ thực tế:**
> Customer gọi: "Tôi muốn xem lại báo giá QUO-000123"
> - Support agent xem chi tiết:
>   * Items: 50 licenses + training
>   * Total: $7,650 (after discount)
>   * Expiry: January 25, 2026 (còn 7 ngày)
>   * Terms: Payment within 30 days
> → Reply: "Báo giá gồm... valid đến 25/1, sau đó cần renew"

---

### 4. Cập nhật Quotation (Update Quotation)

**Nghiệp vụ thực tế:**
- Customer negotiate discount
- Thay đổi terms, expiry date
- Update notes

**Lưu ý:** Chỉ update được quotes ở Draft status. Quotes đã Sent thì lock, tránh thay đổi sau khi customer đã review.

**Ví dụ thực tế:**
> Customer: "Discount có thể lên 15% không?"
> - Sales check: Quote status = Draft (OK to update)
> - Update DiscountPercent: 10% → 15%
> - Recalculate total: $8,000 → $7,225
> - Update Notes: "Discount increased per customer request"
> → Quote updated, ready to send

---

### 5. Gửi Quotation (Send Quotation)

**Nghiệp vụ thực tế:**
- Sales Manager approve quote → Send to customer
- Lock quote, không thể thay đổi
- Set expiry date countdown

**Ví dụ thực tế:**
> Sales Manager review Quote Draft $50K:
> - Check items: OK
> - Check pricing: Competitive
> - Check discount: 10% reasonable
> - Click "Send Quotation"
> - Status: Draft → Sent
> - Email auto-send to customer contact
> - Expiry countdown starts: 30 days
> - Calendar reminder: Follow up in 7 days

---

### 6. Accept Quotation (Accept Quotation)

**Nghiệp vụ thực tế:**
- Customer call/email: "We accept the quote"
- Sales Rep update status to Accepted
- Trigger: Ready to convert to Order

**Ví dụ thực tế:**
> Customer email: "Báo giá QUO-000123, chúng tôi chấp thuận"
> - Sales Rep update: Status → Accepted
> - System auto:
>   * Notify Sales Manager
>   * Update Opportunity: stage → Closed-Won
>   * Trigger: Ready for order conversion
> → Next step: Convert to Order

---

### 7. Reject Quotation (Reject Quotation)

**Nghiệp vụ thực tế:**
- Customer decline: "Giá cao quá", "Không phù hợp nhu cầu"
- Sales Rep ghi nhận reject reason
- Update Opportunity to Lost

**Ví dụ thực tế:**
> Customer: "Cảm ơn báo giá, nhưng budget chỉ $40K, không phù hợp"
> - Sales Rep update: Status → Rejected
> - Add Notes: "Rejected - Price too high, budget $40K"
> - System auto:
>   * Update Opportunity: stage → Closed-Lost
>   * Lost Reason: Price
> → Sales có thể revise quote với giá thấp hơn (create new quote)

---

### 8. Convert to Order (Convert Quotation to Order)

**Nghiệp vụ thực tế:**
- Quote Accepted → Create Order tự động
- Copy tất cả items, pricing, terms
- Link Order back to Quotation

**Ví dụ thực tế:**
> Quote #QUO-000123 Accepted:
> - Sales click "Convert to Order"
> - System auto-create Order #ORD-000456:
>   * Copy all items from quote
>   * Copy customer, contact info
>   * Copy pricing, discount, terms
>   * Set QuotationId = QUO-000123
> - Quote status → Converted
> - Order status = Draft
> → Sales Manager review Order, then Confirm

**Lưu ý quan trọng:**
- Chỉ quote Accepted mới convert được
- Mỗi quote chỉ convert 1 lần (prevent duplicate orders)

---

### 9. Xóa Quotation (Delete Quotation)

**Nghiệp vụ thực tế:**
- Xóa quotes nhầm, duplicate
- Soft delete để giữ lịch sử

---

## Quotation Calculation (Tính toán báo giá)

### Formula (giống Order)

```
SubTotal = Σ (Quantity × UnitPrice) for all items

DiscountAmount = SubTotal × DiscountPercent / 100

TaxAmount = Σ (LineItemTotal × TaxPercent / 100)

TotalAmount = SubTotal - DiscountAmount + TaxAmount
```

### Ví dụ chi tiết

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    QUOTATION #QUO-000123                                    │
│                    ABC Corporation                                          │
│                    Valid until: January 25, 2026                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Items:                                                                     │
│  ───────────────────────────────────────────────────────────────────────    │
│  1. CRM Software License                                                    │
│     50 users × $100/user                               = $5,000             │
│     Discount 10%:                                        -$500              │
│     Tax 8%:                                              +$360              │
│     ─────────────────────────────────────────────────────────────           │
│     Line Total:                                          $4,860             │
│                                                                             │
│  2. Implementation Service                                                  │
│     20 hours × $150/hour                               = $3,000             │
│     Discount 5%:                                         -$150              │
│     Tax 8%:                                              +$228              │
│     ─────────────────────────────────────────────────────────────           │
│     Line Total:                                          $3,078             │
│                                                                             │
│  ═══════════════════════════════════════════════════════════════════════    │
│  SubTotal:                                               $8,000             │
│  Order-level Discount (5%):                               -$400             │
│  Tax Total:                                                +$588             │
│  ═══════════════════════════════════════════════════════════════════════    │
│  TOTAL AMOUNT:                                           $8,188             │
│                                                                             │
│  Payment Terms: Net 30 days                                                │
│  Delivery: 10 business days after order confirmation                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Expiry Management

### Auto Expiry

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         QUOTATION EXPIRY                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Quotation #QUO-000123                                                      │
│  Created: January 1, 2026                                                   │
│  Sent: January 5, 2026                                                      │
│  Expiry Date: February 5, 2026 (30 days)                                   │
│                                                                             │
│  Timeline:                                                                  │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Day 0  (Jan 5):  Quote sent                                               │
│  Day 7  (Jan 12): Auto reminder to customer                                │
│  Day 21 (Jan 26): Alert to sales rep "Expiring soon"                       │
│  Day 28 (Feb 2):  Urgent alert "3 days left"                               │
│  Day 30 (Feb 5):  Auto status → Expired                                    │
│                                                                             │
│  Actions after Expiry:                                                      │
│  - Customer cannot accept expired quote                                     │
│  - Sales can revise and resend (create new quote)                          │
│  - Opportunity status unchanged (still in pipeline)                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Best Practices

| Scenario | Expiry Duration |
|----------|-----------------|
| Standard quote | 30 days |
| Simple products (off-the-shelf) | 15 days |
| Custom solutions | 60 days |
| Government/Enterprise | 90 days |
| Hot lead (urgent need) | 7 days |

---

## Quotation Revisions

### Khi nào cần revision?

| Situation | Action |
|-----------|--------|
| Customer request discount | Create new quote (revision) |
| Pricing changed | Revise and resend |
| Scope changed | New quote với items mới |
| Quote expired | Revise và extend expiry |

### Revision Flow

```
Original Quote: QUO-000123 v1.0 ($10,000)
    ↓
Customer: "Discount 15% được không?"
    ↓
Revised Quote: QUO-000124 v2.0 ($8,500)
    ↓
Customer: "OK, accept v2"
    ↓
Convert to Order
```

---

## Tích hợp với các module khác

```
                        ┌─────────────┐
                        │  QUOTATION  │
                        │  (Báo giá)  │
                        └──────┬──────┘
                               │
      ┌────────────────────────┼────────────────────────┐
      │                │                 │              │
      ▼                ▼                 ▼              ▼
┌────────────┐  ┌──────────┐     ┌──────────┐   ┌──────────┐
│OPPORTUNITY │  │ CUSTOMER │     │  ORDER   │   │ PRODUCT  │
│            │  │          │     │          │   │          │
│ Source     │  │ Buyer    │     │ Output   │   │ Items    │
└────────────┘  └──────────┘     └──────────┘   └──────────┘
      │                                │
      ▼                                ▼
┌────────────┐                  ┌──────────┐
│  PIPELINE  │                  │ CONTRACT │
│            │                  │          │
│ Stage      │                  │ Next     │
└────────────┘                  └──────────┘
```

| Module | Quan hệ | Mô tả |
|--------|---------|-------|
| **Opportunity** | Opportunity → Quotation | Tạo quote từ opportunity |
| **Customer** | Quotation của Customer | Quote gửi cho customer nào |
| **Order** | Quotation → Order | Accepted quote convert to order |
| **Product** | Quotation có Items | Items từ product catalog |
| **Contact** | Quotation gửi cho Contact | Người nhận quote |
| **Interaction** | Quotation là Interaction | Ghi nhận vào lịch sử |

---

## Best Practices

### 1. Luôn link Quotation với Opportunity

- Đừng tạo quote orphan (không có opportunity)
- Track: Opportunity → Quote → Order
- Sales pipeline visibility

### 2. Set realistic Expiry Date

- Standard: 30 days
- Update expiry nếu customer request thêm thời gian
- Auto-remind trước khi expiry

### 3. Lock Quote sau khi Send

- Không update quote đã sent
- Create revision nếu cần thay đổi
- Maintain audit trail

### 4. Clear Terms & Conditions

- Payment terms: Net 30, 50% upfront
- Delivery timeline
- Warranty, support terms
- → Avoid disputes sau này

### 5. Professional Quote Format

- Company branding, logo
- Clear itemization
- Breakdown: subtotal, discount, tax, total
- Contact info for questions
- Digital signature (optional)

---

## Technical Overview

**Base URL:** `/api/v1/quotations`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Quotations

Lấy danh sách quotations với phân trang và filter.

```
GET /api/v1/quotations
```

**Permission Required:** `quotation.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Số items mỗi trang (max: 100) |
| `sortBy` | string | No | "CreatedAt" | Field để sắp xếp |
| `sortDescending` | bool | No | false | Sắp xếp giảm dần |
| `search` | string | No | - | Tìm kiếm theo quotationNumber |
| `status` | QuotationStatus | No | - | Filter theo status |
| `customerId` | Guid | No | - | Filter theo customer |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "quotationNumber": "QUO-000123",
        "customerName": "ABC Corporation",
        "status": "Sent",
        "subTotal": 8000.00,
        "discountAmount": 400.00,
        "taxAmount": 588.00,
        "totalAmount": 8188.00,
        "currency": "USD",
        "quotationDate": "2026-01-15T00:00:00Z",
        "expiryDate": "2026-02-15T00:00:00Z",
        "createdAt": "2026-01-15T10:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 2,
    "totalCount": 18,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

---

### 2. Get Quotation by ID

Lấy chi tiết một quotation với items.

```
GET /api/v1/quotations/{id}
```

**Permission Required:** `quotation.view`

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "quotationNumber": "QUO-000123",
    "customerName": "ABC Corporation",
    "opportunityName": "ABC Corp - CRM Implementation",
    "status": "Sent",
    "quotationDate": "2026-01-15T00:00:00Z",
    "expiryDate": "2026-02-15T00:00:00Z",
    "subTotal": 8000.00,
    "discountAmount": 400.00,
    "discountPercent": 5.0,
    "taxAmount": 588.00,
    "totalAmount": 8188.00,
    "currency": "USD",
    "billingAddress": "123 Main St, City, Country",
    "shippingAddress": "456 Delivery St, City, Country",
    "notes": "Volume discount applied",
    "terms": "Payment due within 30 days of order confirmation",
    "items": [
      {
        "id": "item-guid-1",
        "productId": "product-guid",
        "description": "CRM Software License - 50 users",
        "quantity": 50,
        "unitPrice": 100.00,
        "discountPercent": 10.0,
        "taxPercent": 8.0,
        "totalAmount": 4860.00
      },
      {
        "id": "item-guid-2",
        "productId": "product-guid-2",
        "description": "Implementation Service - 20 hours",
        "quantity": 20,
        "unitPrice": 150.00,
        "discountPercent": 5.0,
        "taxPercent": 8.0,
        "totalAmount": 3078.00
      }
    ],
    "createdAt": "2026-01-15T10:00:00Z"
  }
}
```

---

### 3. Create Quotation

Tạo quotation mới với items.

```
POST /api/v1/quotations
```

**Permission Required:** `quotation.create`

#### Request Body

```json
{
  "customerId": "customer-guid",
  "contactId": "contact-guid",
  "opportunityId": "opportunity-guid",
  "currency": "USD",
  "discountAmount": 400,
  "discountPercent": 5,
  "notes": "Volume discount applied",
  "terms": "Payment due within 30 days of order confirmation",
  "quotationDate": "2026-01-15T00:00:00Z",
  "expiryDate": "2026-02-15T00:00:00Z",
  "billingAddress": "123 Main St, City, Country",
  "shippingAddress": "456 Delivery St, City, Country",
  "items": [
    {
      "productId": "product-guid",
      "description": "CRM Software License - 50 users",
      "quantity": 50,
      "unitPrice": 100.00,
      "discountPercent": 10.0,
      "taxPercent": 8.0,
      "sortOrder": 1
    },
    {
      "productId": "product-guid-2",
      "description": "Implementation Service - 20 hours",
      "quantity": 20,
      "unitPrice": 150.00,
      "discountPercent": 5.0,
      "taxPercent": 8.0,
      "sortOrder": 2
    }
  ]
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `customerId` | Guid | No | ID customer |
| `contactId` | Guid | No | ID contact nhận quote |
| `opportunityId` | Guid | No | ID opportunity (recommend) |
| `currency` | string | No | Mã tiền tệ (default: USD) |
| `discountAmount` | decimal | No | Số tiền discount order-level |
| `discountPercent` | decimal | No | Phần trăm discount order-level |
| `notes` | string | No | Ghi chú |
| `terms` | string | No | Điều khoản thanh toán |
| `quotationDate` | DateTime | No | Ngày báo giá (default: now) |
| `expiryDate` | DateTime | No | Ngày hết hạn (default: +30 days) |
| `billingAddress` | string | No | Địa chỉ billing |
| `shippingAddress` | string | No | Địa chỉ giao hàng |
| `items` | array | **Yes** | Danh sách items |

#### Item Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `productId` | Guid | No | ID product |
| `description` | string | No | Mô tả item |
| `quantity` | decimal | **Yes** | Số lượng |
| `unitPrice` | decimal | **Yes** | Đơn giá |
| `discountPercent` | decimal | No | Phần trăm discount item-level |
| `taxPercent` | decimal | No | Phần trăm thuế |
| `sortOrder` | int | No | Thứ tự hiển thị |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-quotation-guid",
    "quotationNumber": "QUO-000124",
    "status": "Draft",
    "totalAmount": 8188.00,
    "createdAt": "2026-01-18T12:00:00Z"
  }
}
```

---

### 4. Update Quotation

Cập nhật thông tin quotation (chỉ Draft).

```
PUT /api/v1/quotations/{id}
```

**Permission Required:** `quotation.update`

#### Request Body (All fields optional)

```json
{
  "notes": "Discount increased per customer request",
  "expiryDate": "2026-02-20T00:00:00Z",
  "discountAmount": 600,
  "discountPercent": 7.5
}
```

---

### 5. Send Quotation

Gửi quotation cho customer (Draft → Sent).

```
POST /api/v1/quotations/{id}/send
```

**Permission Required:** `quotation.update`

#### Response

```json
{
  "success": true,
  "message": "Quotation sent successfully"
}
```

**Lưu ý:** 
- Chỉ quote Draft mới send được
- Sau khi send, quote lock (không update được)
- Auto-send email cho customer contact

---

### 6. Accept Quotation

Customer chấp thuận quotation.

```
POST /api/v1/quotations/{id}/accept
```

**Permission Required:** `quotation.update`

---

### 7. Reject Quotation

Customer từ chối quotation.

```
POST /api/v1/quotations/{id}/reject
```

**Permission Required:** `quotation.update`

---

### 8. Convert to Order

Chuyển quotation thành order (chỉ Accepted).

```
POST /api/v1/quotations/{id}/convert-to-order
```

**Permission Required:** `order.create`

#### Response

```json
{
  "success": true,
  "data": {
    "orderId": "new-order-guid",
    "orderNumber": "ORD-000456"
  }
}
```

**Lưu ý:**
- Chỉ quote Accepted mới convert được
- Tự động copy tất cả items, pricing, terms
- Quotation status → Converted
- Mỗi quote chỉ convert 1 lần

---

### 9. Delete Quotation

Xóa mềm quotation.

```
DELETE /api/v1/quotations/{id}
```

**Permission Required:** `quotation.delete`

---

## Enums

### QuotationStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Draft | Nháp |
| 1 | Sent | Đã gửi |
| 2 | Accepted | Đã chấp thuận |
| 3 | Rejected | Đã từ chối |
| 4 | Expired | Hết hạn |
| 5 | Converted | Đã chuyển thành order |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `quotation.view` | Xem danh sách và chi tiết quotation |
| `quotation.create` | Tạo quotation mới |
| `quotation.update` | Cập nhật, send, accept, reject |
| `quotation.delete` | Xóa quotation |
| `order.create` | Convert quotation to order |

---

## Example: Complete Quotation Flow

### Scenario: Close a $50K deal with quotation

#### 1. Create Quotation from Opportunity

```bash
curl -X POST "http://localhost:5000/api/v1/quotations" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-guid",
    "contactId": "contact-guid",
    "opportunityId": "opportunity-guid",
    "currency": "USD",
    "expiryDate": "2026-02-18T00:00:00Z",
    "items": [
      {
        "productId": "product-1",
        "description": "CRM License - 50 users",
        "quantity": 50,
        "unitPrice": 100,
        "discountPercent": 10,
        "taxPercent": 8
      }
    ]
  }'
```

#### 2. Manager review và Send

```bash
curl -X POST "http://localhost:5000/api/v1/quotations/{quotation-id}/send" \
  -H "Authorization: Bearer {token}"
```

#### 3. Customer Accept

```bash
curl -X POST "http://localhost:5000/api/v1/quotations/{quotation-id}/accept" \
  -H "Authorization: Bearer {token}"
```

#### 4. Convert to Order

```bash
curl -X POST "http://localhost:5000/api/v1/quotations/{quotation-id}/convert-to-order" \
  -H "Authorization: Bearer {token}"
```

---

## Integration Examples

### Create Quotation from Opportunity

```csharp
// When Opportunity reaches "Proposal/Quote" stage
if (opportunity.Stage == "Proposal")
{
    var quotation = new Quotation
    {
        OpportunityId = opportunity.Id,
        CustomerId = opportunity.CustomerId,
        ContactId = opportunity.ContactId,
        QuotationDate = DateTime.UtcNow,
        ExpiryDate = DateTime.UtcNow.AddDays(30),
        Notes = $"Quote for {opportunity.Name}"
    };
    
    // Add items from opportunity
    // ...
    
    await _db.Quotations.AddAsync(quotation);
    await _db.SaveChangesAsync();
}
```

### Auto-update Opportunity when Quote Accepted

```csharp
// When quotation is accepted
if (quotation.Status == QuotationStatus.Accepted)
{
    var opportunity = await _db.Opportunities.FindAsync(quotation.OpportunityId);
    if (opportunity != null)
    {
        opportunity.Stage = "Closed-Won";
        await _db.SaveChangesAsync();
    }
}
```

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
