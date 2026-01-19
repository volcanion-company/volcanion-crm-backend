# Orders API Documentation

## Tổng quan Module

### Order là gì?

**Order (Đơn hàng)** là xác nhận chính thức từ khách hàng muốn mua sản phẩm/dịch vụ. Order được tạo sau khi Opportunity won hoặc từ Quotation được khách chấp thuận.

### Vị trí của Order trong Sales Cycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        COMPLETE SALES CYCLE                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────┐    ┌────────┐    ┌─────────────┐    ┌───────┐    ┌──────────┐   │
│  │ LEAD │───►│CUSTOMER│───►│ OPPORTUNITY │───►│ ORDER │───►│ CONTRACT │   │
│  └──────┘    └────────┘    └─────────────┘    └───────┘    └──────────┘   │
│  Potential    Convert       Đang chốt deal    Đã chốt     Thực hiện      │
│  Customer     to Customer   (In Pipeline)       Deal        dịch vụ       │
│                                   │                                         │
│                                   ▼                                         │
│                            ┌─────────────┐                                 │
│                            │  QUOTATION  │                                 │
│                            │   (Báo giá) │                                 │
│                            └──────┬──────┘                                 │
│                                   │                                         │
│                                   │ Customer Accept                         │
│                                   ▼                                         │
│                            ┌──────────┐                                    │
│                            │  ORDER   │                                    │
│                            └──────────┘                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Order vs Quotation vs Opportunity

| Khái niệm | Giai đoạn | Commitment | Ví dụ |
|-----------|-----------|------------|-------|
| **Opportunity** | Đang thương lượng | Chưa chắc chắn | "ABC Corp có thể mua CRM package $50K" |
| **Quotation** | Gửi báo giá | Proposal, chờ approval | "Gửi báo giá $50K cho ABC Corp" |
| **Order** | Đã chốt deal | Commitment chính thức | "ABC Corp confirm mua, tạo Order #ORD-000123" |
| **Contract** | Thực hiện | Legal document | "Ký hợp đồng, bắt đầu triển khai" |

---

## Tại sao cần Order Management?

### Vấn đề thực tế

| Vấn đề | Giải pháp của Order Module |
|--------|---------------------------|
| Track đơn hàng thủ công bằng Excel | Central order repository với số thứ tự tự động |
| Không biết đơn nào đã thanh toán | Payment status tracking (Paid, Partial, Unpaid) |
| Lộn xộn giữa quote và order thật | Clear separation: Quote → Order workflow |
| Không biết đơn nào đang ship | Order status lifecycle |
| Tính toán tax, discount thủ công | Auto-calculation: subtotal, discount, tax, total |
| Không có lịch sử đơn hàng của khách | Order history per customer |

---

## Order Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ORDER STATUS FLOW                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────┐    ┌───────────┐    ┌────────────┐    ┌─────────┐              │
│  │ DRAFT │───►│ CONFIRMED │───►│ PROCESSING │───►│ SHIPPED │──┐           │
│  └───────┘    └───────────┘    └────────────┘    └─────────┘  │           │
│      │              │                                           │           │
│      │              │                                           ▼           │
│      │              │                                    ┌───────────┐      │
│      │              │                                    │ DELIVERED │      │
│      │              │                                    └─────┬─────┘      │
│      │              │                                          │            │
│      │              │                                          ▼            │
│      │              │                                    ┌───────────┐      │
│      │              │                                    │ COMPLETED │      │
│      │              │                                    └───────────┘      │
│      │              │                                                       │
│      ▼              ▼                                                       │
│  ┌───────────┐  ┌───────────┐                                              │
│  │ CANCELLED │  │ CANCELLED │                                              │
│  └───────────┘  └───────────┘                                              │
│                       │                                                     │
│                       ▼ (If paid)                                           │
│                  ┌──────────┐                                               │
│                  │ REFUNDED │                                               │
│                  └──────────┘                                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Chi tiết từng Status

| Status | Ý nghĩa | Action tiếp theo | Có thể cancel? |
|--------|---------|------------------|----------------|
| **Draft** | Đơn nháp, chưa chính thức | Confirm hoặc Cancel | ✅ Yes |
| **Confirmed** | Khách đã xác nhận mua | Process order | ✅ Yes (refund nếu đã trả) |
| **Processing** | Đang chuẩn bị hàng | Ship goods | ⚠️ Có (phức tạp hơn) |
| **Shipped** | Đã giao cho đơn vị vận chuyển | Confirm delivered | ❌ No (contact support) |
| **Delivered** | Khách đã nhận hàng | Complete order | ❌ No |
| **Completed** | Hoàn thành, close deal | - | ❌ No |
| **Cancelled** | Đã hủy | - | - |
| **Refunded** | Đã hoàn tiền | - | - |

---

## Payment Status Tracking

### Payment Workflow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     PAYMENT STATUS FLOW                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Order Total: $10,000                                                       │
│                                                                             │
│  ┌─────────┐                                                                │
│  │ UNPAID  │  Paid Amount: $0                                               │
│  │  $0/$10K│                                                                │
│  └────┬────┘                                                                │
│       │                                                                     │
│       │ Record Payment: $3,000                                              │
│       ▼                                                                     │
│  ┌───────────────┐                                                          │
│  │ PARTIALLY PAID│  Paid Amount: $3,000                                     │
│  │  $3K/$10K     │  Balance: $7,000                                         │
│  └───────┬───────┘                                                          │
│          │                                                                  │
│          │ Record Payment: $7,000                                           │
│          ▼                                                                  │
│  ┌─────────────┐                                                            │
│  │    PAID     │  Paid Amount: $10,000                                      │
│  │  $10K/$10K  │  Balance: $0 ✓                                             │
│  └──────┬──────┘                                                            │
│         │                                                                   │
│         │ (If order cancelled)                                              │
│         ▼                                                                   │
│  ┌──────────┐                                                               │
│  │ REFUNDED │  Refund Amount: $10,000                                       │
│  └──────────┘                                                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Payment Status

| Status | Điều kiện | Ví dụ |
|--------|-----------|-------|
| **Unpaid** | PaidAmount = $0 | Order $10K, chưa trả gì |
| **Partially Paid** | $0 < PaidAmount < TotalAmount | Order $10K, trả $3K, còn nợ $7K |
| **Paid** | PaidAmount >= TotalAmount | Order $10K, đã trả đủ $10K |
| **Refunded** | Cancelled và đã hoàn tiền | Order cancelled, refund $10K |

---

## Order Calculation (Tính toán đơn hàng)

### Formula

```
SubTotal = Σ (Quantity × UnitPrice) for all items

DiscountAmount = SubTotal × DiscountPercent / 100

TaxAmount = Σ (LineItemTotal × TaxPercent / 100)

ShippingAmount = Fixed amount

TotalAmount = SubTotal - DiscountAmount + TaxAmount + ShippingAmount

BalanceAmount = TotalAmount - PaidAmount
```

### Ví dụ tính toán

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ORDER #ORD-000123                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Items:                                                                     │
│  1. CRM Software License × 10 users @ $100/user     = $1,000               │
│     - Discount 10%:                                   -$100                 │
│     - Tax 8%:                                          +$72                 │
│     Line Total:                                        $972                 │
│                                                                             │
│  2. Implementation Service × 20 hours @ $150/hour   = $3,000               │
│     - Discount 5%:                                    -$150                 │
│     - Tax 8%:                                         +$228                 │
│     Line Total:                                      $3,078                 │
│                                                                             │
│  ───────────────────────────────────────────────────────────────────────    │
│  SubTotal:                                           $4,000                 │
│  Order-level Discount (5%):                           -$200                 │
│  Tax:                                                  +$300                 │
│  Shipping:                                             +$100                 │
│  ───────────────────────────────────────────────────────────────────────    │
│  Total Amount:                                       $4,200                 │
│  Paid Amount:                                        $2,000                 │
│  Balance:                                            $2,200                 │
│                                                                             │
│  Payment Status: Partially Paid                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Multi-Currency Support

### Exchange Rate

```
Order in USD (Base Currency): $10,000

Customer in VND:
- Exchange Rate: 1 USD = 24,000 VND
- Total in VND: 240,000,000 VND
```

### Currency Fields

| Field | Description |
|-------|-------------|
| `Currency` | Mã tiền tệ (USD, VND, EUR) |
| `ExchangeRate` | Tỷ giá so với base currency |
| `TotalAmount` | Giá trị theo currency của order |

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Tạo Order (Create Order)

**Nghiệp vụ thực tế:**
- Opportunity won → Sales tạo order
- Quotation được accept → Convert to order
- Customer trực tiếp đặt hàng (web, phone)

**Ví dụ thực tế:**
> Sales Rep đã close deal với ABC Corp:
> - Opportunity value: $50,000 → Won
> - Tạo Order mới từ Quotation #QUO-000456
> - Add items: 50 user licenses, training service
> - Apply discount: 10% cho số lượng lớn
> - Set payment terms: 30 days
> - Status: Draft → Sales Manager review trước khi Confirm

---

### 2. Xem danh sách Orders (Get All Orders)

**Nghiệp vụ thực tế:**
- Sales Manager review tất cả orders
- Finance team check orders cần thu tiền
- Operations team xem orders cần ship

**Ví dụ thực tế:**
> Finance Manager mỗi tuần:
> - Filter PaymentStatus = Unpaid → 15 orders cần thu tiền
> - Filter Status = Confirmed → 8 orders cần process
> - Sort by TotalAmount desc → Focus vào orders lớn trước

---

### 3. Xem chi tiết Order (Get Order by ID)

**Nghiệp vụ thực tế:**
- Customer service trả lời câu hỏi của khách về order
- Warehouse check chi tiết items cần ship
- Accounting verify thông tin billing

**Ví dụ thực tế:**
> Customer gọi hotline: "Order ORD-000123 của tôi đến đâu rồi?"
> - Support agent xem chi tiết: Status = Shipped, delivery date = tomorrow
> - Check items: 10 licenses + training
> - Check payment: Paid $4,200/$4,200 ✓
> → Reply: "Order đã ship, dự kiến giao ngày mai"

---

### 4. Cập nhật Order (Update Order)

**Nghiệp vụ thực tế:**
- Customer yêu cầu đổi địa chỉ giao hàng
- Thêm notes về yêu cầu đặc biệt
- Update delivery date

**Lưu ý:** Chỉ update được orders ở Draft/Confirmed. Orders đã Complete/Cancel không update được.

**Ví dụ thực tế:**
> Customer: "Tôi muốn đổi địa chỉ giao hàng"
> - Check Order status: Confirmed (OK, chưa ship)
> - Update ShippingAddress: "New address..."
> - Update Notes: "Customer requested address change"
> → Order updated successfully

---

### 5. Confirm Order (Confirm Order)

**Nghiệp vụ thực tế:**
- Sales Manager duyệt order từ Draft → Confirmed
- Lock order, không thể thay đổi items

**Ví dụ thực tế:**
> Sales Rep tạo Order Draft $50K:
> - Manager review: price OK, discount reasonable
> - Manager click "Confirm"
> - Status: Draft → Confirmed
> - Email auto-send cho customer: "Order confirmed, preparing for delivery"

---

### 6. Process Order (Process Order)

**Nghiệp vụ thực tế:**
- Warehouse/Operations bắt đầu chuẩn bị hàng
- Pick items, pack, ready to ship

**Ví dụ thực tế:**
> Warehouse nhận Order #ORD-000123 (Confirmed):
> - Pick items from inventory
> - Pack goods
> - Update status: Processing
> - Notify shipping team

---

### 7. Complete Order (Complete Order)

**Nghiệp vụ thực tế:**
- Đơn hàng đã hoàn tất toàn bộ
- Customer satisfied, close order

**Ví dụ thực tế:**
> Order đã Delivered + Payment Paid:
> - Customer confirm: "Đã nhận hàng, OK"
> - Update status: Completed
> - Trigger: Create contract (nếu subscription)
> - Trigger: Update opportunity to Closed-Won

---

### 8. Cancel Order (Cancel Order)

**Nghiệp vụ thực tế:**
- Customer hủy order trước khi ship
- Internal issue: out of stock

**Ví dụ thực tế:**
> Customer: "Tôi muốn hủy order"
> - Check status: Confirmed (chưa ship, OK để cancel)
> - Check payment: $2,000 paid
> - Cancel order
> - Initiate refund process
> - Status: Cancelled → PaymentStatus: Refunded

---

### 9. Record Payment (Record Payment)

**Nghiệp vụ thực tế:**
- Finance nhận payment từ customer
- Update payment status, track balance

**Ví dụ thực tế:**
> Order #ORD-000123, Total: $10,000, Paid: $0
> - Customer chuyển khoản $3,000
> - Finance record payment:
>   * Amount: $3,000
>   * Method: Bank Transfer
>   * Reference: TXN-456789
> - System update:
>   * PaidAmount: $3,000
>   * BalanceAmount: $7,000
>   * PaymentStatus: Unpaid → Partially Paid

> Sau 1 tuần, customer trả tiếp $7,000:
> - Record payment $7,000
> - PaidAmount: $10,000
> - PaymentStatus: Partially Paid → Paid ✓

---

### 10. Xóa Order (Delete Order)

**Nghiệp vụ thực tế:**
- Xóa orders nhầm, duplicate
- Soft delete để giữ lịch sử

---

## Tích hợp với các module khác

```
                        ┌───────────────┐
                        │     ORDER     │
                        │   (Đơn hàng)  │
                        └───────┬───────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        │               │               │               │
        ▼               ▼               ▼               ▼
  ┌──────────┐   ┌────────────┐  ┌──────────┐   ┌──────────┐
  │ CUSTOMER │   │ OPPORTUNITY│  │QUOTATION │   │ CONTRACT │
  │          │   │            │  │          │   │          │
  │ Buyer    │   │ Source     │  │ Source   │   │ Output   │
  └──────────┘   └────────────┘  └──────────┘   └──────────┘
        │               │
        │               ▼
        │        ┌────────────┐
        │        │  PIPELINE  │
        │        │            │
        │        │ Stage Won  │
        │        └────────────┘
        ▼
  ┌──────────┐
  │  ITEMS   │
  │          │
  │ Products │
  └──────────┘
```

| Module | Quan hệ | Mô tả |
|--------|---------|-------|
| **Customer** | Order của Customer | Xem tất cả orders của khách hàng |
| **Opportunity** | Opportunity → Order | Won opportunity tạo order |
| **Quotation** | Quotation → Order | Quotation accepted tạo order |
| **Contract** | Order → Contract | Order completed tạo contract (service) |
| **Product** | Order có Items | Items trong order link đến products |
| **Payment** | Order có Payments | Track payment history |
| **Interaction** | Order là Interaction | Ghi nhận vào lịch sử |

---

## Best Practices

### 1. Luôn tạo Order từ Opportunity hoặc Quotation

- Đừng tạo Order orphan (không có nguồn gốc)
- Link Order với Opportunity để track sales performance
- Link Order với Quotation để có audit trail

### 2. Confirm Order trước khi Process

- Draft → Confirm (Manager approval)
- Confirm → Process (Warehouse execution)
- Tránh process orders chưa được duyệt

### 3. Track Payment realtime

- Record payment ngay khi nhận
- Không để orders Unpaid quá lâu
- Set up payment reminders

### 4. Clear Terms & Conditions

- Payment terms: 30 days, 50% upfront
- Delivery terms: FOB, CIF
- Refund policy
- → Avoid disputes

### 5. Separate Billing & Shipping Address

- Billing address: cho invoice
- Shipping address: cho delivery
- Rất quan trọng cho B2B

---

## Technical Overview

**Base URL:** `/api/v1/orders`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Orders

Lấy danh sách orders với phân trang và filter.

```
GET /api/v1/orders
```

**Permission Required:** `order.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Số items mỗi trang (max: 100) |
| `sortBy` | string | No | "CreatedAt" | Field để sắp xếp |
| `sortDescending` | bool | No | false | Sắp xếp giảm dần |
| `search` | string | No | - | Tìm kiếm theo orderNumber |
| `status` | OrderStatus | No | - | Filter theo status |
| `customerId` | Guid | No | - | Filter theo customer |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "orderNumber": "ORD-000123",
        "customerName": "ABC Corporation",
        "status": "Confirmed",
        "paymentStatus": "PartiallyPaid",
        "subTotal": 10000.00,
        "discountAmount": 500.00,
        "taxAmount": 760.00,
        "shippingAmount": 100.00,
        "totalAmount": 10360.00,
        "paidAmount": 5000.00,
        "balanceAmount": 5360.00,
        "currency": "USD",
        "orderDate": "2026-01-15T00:00:00Z",
        "deliveryDate": "2026-01-25T00:00:00Z",
        "createdAt": "2026-01-15T10:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3,
    "totalCount": 28,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

---

### 2. Get Order by ID

Lấy chi tiết một order với items.

```
GET /api/v1/orders/{id}
```

**Permission Required:** `order.view`

#### Response

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "orderNumber": "ORD-000123",
    "customerName": "ABC Corporation",
    "contactName": "Nguyen Van A",
    "status": "Confirmed",
    "paymentStatus": "PartiallyPaid",
    "orderDate": "2026-01-15T00:00:00Z",
    "deliveryDate": "2026-01-25T00:00:00Z",
    "subTotal": 10000.00,
    "discountAmount": 500.00,
    "discountPercent": 5.0,
    "taxAmount": 760.00,
    "shippingAmount": 100.00,
    "totalAmount": 10360.00,
    "paidAmount": 5000.00,
    "balanceAmount": 5360.00,
    "currency": "USD",
    "billingAddress": "123 Main St, City, Country",
    "shippingAddress": "456 Delivery St, City, Country",
    "paymentMethod": "Bank Transfer",
    "paymentReference": "TXN-789456",
    "notes": "Customer requested expedited shipping",
    "terms": "Payment due within 30 days",
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

### 3. Create Order

Tạo order mới với items.

```
POST /api/v1/orders
```

**Permission Required:** `order.create`

#### Request Body

```json
{
  "customerId": "customer-guid",
  "contactId": "contact-guid",
  "quotationId": "quotation-guid",
  "currency": "USD",
  "discountAmount": 500,
  "discountPercent": 5,
  "shippingAmount": 100,
  "notes": "Customer requested expedited shipping",
  "terms": "Payment due within 30 days",
  "orderDate": "2026-01-15T00:00:00Z",
  "deliveryDate": "2026-01-25T00:00:00Z",
  "billingAddress": "123 Main St, City, Country",
  "shippingAddress": "456 Delivery St, City, Country",
  "paymentMethod": "Bank Transfer",
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
| `contactId` | Guid | No | ID contact |
| `quotationId` | Guid | No | ID quotation (nếu convert từ quote) |
| `currency` | string | No | Mã tiền tệ (default: USD) |
| `discountAmount` | decimal | No | Số tiền discount order-level |
| `discountPercent` | decimal | No | Phần trăm discount order-level |
| `shippingAmount` | decimal | No | Phí ship |
| `notes` | string | No | Ghi chú |
| `terms` | string | No | Điều khoản thanh toán |
| `orderDate` | DateTime | No | Ngày đặt hàng (default: now) |
| `deliveryDate` | DateTime | No | Ngày giao hàng dự kiến |
| `billingAddress` | string | No | Địa chỉ billing |
| `shippingAddress` | string | No | Địa chỉ giao hàng |
| `paymentMethod` | string | No | Phương thức thanh toán |
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
    "id": "new-order-guid",
    "orderNumber": "ORD-000124",
    "status": "Draft",
    "totalAmount": 10360.00,
    "createdAt": "2026-01-18T12:00:00Z"
  }
}
```

---

### 4. Update Order

Cập nhật thông tin order (chỉ Draft/Confirmed).

```
PUT /api/v1/orders/{id}
```

**Permission Required:** `order.update`

#### Request Body (All fields optional)

```json
{
  "notes": "Customer requested address change",
  "deliveryDate": "2026-01-26T00:00:00Z",
  "shippingAddress": "New address..."
}
```

---

### 5. Confirm Order

Chuyển order từ Draft sang Confirmed.

```
POST /api/v1/orders/{id}/confirm
```

**Permission Required:** `order.update`

#### Response

```json
{
  "success": true,
  "message": "Order confirmed successfully"
}
```

---

### 6. Process Order

Chuyển order sang Processing (đang chuẩn bị hàng).

```
POST /api/v1/orders/{id}/process
```

**Permission Required:** `order.update`

---

### 7. Complete Order

Hoàn thành order.

```
POST /api/v1/orders/{id}/complete
```

**Permission Required:** `order.update`

---

### 8. Cancel Order

Hủy order.

```
POST /api/v1/orders/{id}/cancel
```

**Permission Required:** `order.update`

---

### 9. Record Payment

Ghi nhận thanh toán cho order.

```
POST /api/v1/orders/{id}/record-payment
```

**Permission Required:** `order.update`

#### Request Body

```json
{
  "amount": 5000.00,
  "reference": "TXN-789456",
  "paymentMethod": "Bank Transfer"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `amount` | decimal | **Yes** | Số tiền thanh toán |
| `reference` | string | No | Mã tham chiếu giao dịch |
| `paymentMethod` | string | No | Phương thức (Bank Transfer, Credit Card, Cash) |

#### Response

```json
{
  "success": true,
  "message": "Payment recorded"
}
```

**Lưu ý:** PaymentStatus tự động update:
- PaidAmount < TotalAmount → PartiallyPaid
- PaidAmount >= TotalAmount → Paid

---

### 10. Delete Order

Xóa mềm order.

```
DELETE /api/v1/orders/{id}
```

**Permission Required:** `order.delete`

---

## Enums

### OrderStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Draft | Nháp |
| 1 | Confirmed | Đã xác nhận |
| 2 | Processing | Đang xử lý |
| 3 | Shipped | Đã giao cho vận chuyển |
| 4 | Delivered | Đã giao hàng |
| 5 | Completed | Hoàn thành |
| 6 | Cancelled | Đã hủy |
| 7 | Refunded | Đã hoàn tiền |

### PaymentStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Unpaid | Chưa thanh toán |
| 1 | PartiallyPaid | Thanh toán một phần |
| 2 | Paid | Đã thanh toán đủ |
| 3 | Refunded | Đã hoàn tiền |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `order.view` | Xem danh sách và chi tiết order |
| `order.create` | Tạo order mới |
| `order.update` | Cập nhật, confirm, process, complete order |
| `order.delete` | Xóa order |

---

## Example: Complete Order Flow

### Scenario: Close a $10K deal

#### 1. Opportunity Won → Create Order

```bash
curl -X POST "http://localhost:5000/api/v1/orders" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-guid",
    "contactId": "contact-guid",
    "currency": "USD",
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

#### 2. Manager review và Confirm

```bash
curl -X POST "http://localhost:5000/api/v1/orders/{order-id}/confirm" \
  -H "Authorization: Bearer {token}"
```

#### 3. Customer trả 50% deposit

```bash
curl -X POST "http://localhost:5000/api/v1/orders/{order-id}/record-payment" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 5000,
    "reference": "TXN-001",
    "paymentMethod": "Bank Transfer"
  }'
```

#### 4. Warehouse process order

```bash
curl -X POST "http://localhost:5000/api/v1/orders/{order-id}/process" \
  -H "Authorization: Bearer {token}"
```

#### 5. Customer trả 50% còn lại

```bash
curl -X POST "http://localhost:5000/api/v1/orders/{order-id}/record-payment" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 5000,
    "reference": "TXN-002",
    "paymentMethod": "Bank Transfer"
  }'
```

#### 6. Complete order

```bash
curl -X POST "http://localhost:5000/api/v1/orders/{order-id}/complete" \
  -H "Authorization: Bearer {token}"
```

---

## Integration Examples

### Create Order from Won Opportunity

```csharp
// When Opportunity status changes to Won
if (opportunity.Status == OpportunityStatus.Won)
{
    var order = new Order
    {
        CustomerId = opportunity.CustomerId,
        ContactId = opportunity.ContactId,
        OrderDate = DateTime.UtcNow,
        Notes = $"Created from Opportunity {opportunity.OpportunityNumber}"
    };
    
    // Copy items from opportunity
    // ...
    
    await _db.Orders.AddAsync(order);
    await _db.SaveChangesAsync();
}
```

### Create Order from Accepted Quotation

```csharp
// When Quotation is accepted
if (quotation.Status == QuotationStatus.Accepted)
{
    var order = new Order
    {
        QuotationId = quotation.Id,
        CustomerId = quotation.CustomerId,
        Items = quotation.Items.Select(qi => new OrderItem
        {
            ProductId = qi.ProductId,
            Quantity = qi.Quantity,
            UnitPrice = qi.UnitPrice,
            // ...
        }).ToList()
    };
    
    await _db.Orders.AddAsync(order);
    await _db.SaveChangesAsync();
}
```

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
