using CrmSaas.Api.Authorization;
using CrmSaas.Api.Common;
using CrmSaas.Api.Data;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSaas.Api.Controllers;

[Authorize]
public class OrdersController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public OrdersController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.OrderView)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] Guid? customerId = null)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .WhereIf(status.HasValue, o => o.Status == status!.Value)
            .WhereIf(customerId.HasValue, o => o.CustomerId == customerId)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), o =>
                o.OrderNumber.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(o => new OrderResponse
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer != null ? o.Customer.Name : null,
                Status = o.Status.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TaxAmount = o.TaxAmount,
                ShippingAmount = o.ShippingAmount,
                TotalAmount = o.TotalAmount,
                PaidAmount = o.PaidAmount,
                BalanceAmount = o.BalanceAmount,
                Currency = o.Currency,
                OrderDate = o.OrderDate,
                DeliveryDate = o.DeliveryDate,
                CreatedAt = o.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.OrderView)]
    public async Task<ActionResult<ApiResponse<OrderDetailResponse>>> GetById(Guid id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Contact)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFoundResponse<OrderDetailResponse>($"Order with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(order));
    }

    [HttpPost]
    [RequirePermission(Permissions.OrderCreate)]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Create([FromBody] CreateOrderRequest request)
    {
        var orderNumber = await GenerateOrderNumber();

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = request.CustomerId,
            ContactId = request.ContactId,
            QuotationId = request.QuotationId,
            Currency = request.Currency ?? "USD",
            DiscountAmount = request.DiscountAmount,
            DiscountPercent = request.DiscountPercent,
            ShippingAmount = request.ShippingAmount,
            Notes = request.Notes,
            Terms = request.Terms,
            OrderDate = request.OrderDate ?? DateTime.UtcNow,
            DeliveryDate = request.DeliveryDate,
            BillingAddress = request.BillingAddress,
            ShippingAddress = request.ShippingAddress,
            PaymentMethod = request.PaymentMethod,
            CreatedBy = _currentUser.UserId
        };

        // Add items and calculate totals
        decimal subTotal = 0;
        foreach (var item in request.Items)
        {
            var lineTotal = item.Quantity * item.UnitPrice;
            var lineDiscount = lineTotal * item.DiscountPercent / 100;
            var lineTax = (lineTotal - lineDiscount) * item.TaxPercent / 100;
            var itemTotal = lineTotal - lineDiscount + lineTax;

            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                DiscountAmount = lineDiscount,
                TaxPercent = item.TaxPercent,
                TaxAmount = lineTax,
                TotalAmount = itemTotal,
                SortOrder = item.SortOrder
            });

            subTotal += lineTotal;
        }

        order.SubTotal = subTotal;
        order.TaxAmount = order.Items.Sum(i => i.TaxAmount);
        order.TotalAmount = order.SubTotal - order.DiscountAmount + order.TaxAmount + order.ShippingAmount;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Order), order.Id, order.OrderNumber);

        return CreatedResponse(new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.OrderUpdate)]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Update(Guid id, [FromBody] UpdateOrderRequest request)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFoundResponse<OrderResponse>($"Order with id {id} not found");
        }

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
        {
            return BadRequestResponse<OrderResponse>("Cannot update completed or cancelled orders");
        }

        order.Notes = request.Notes ?? order.Notes;
        order.DeliveryDate = request.DeliveryDate ?? order.DeliveryDate;
        order.ShippingAddress = request.ShippingAddress ?? order.ShippingAddress;
        order.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Order), order.Id, order.OrderNumber);

        return OkResponse(new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        });
    }

    [HttpPost("{id:guid}/confirm")]
    [RequirePermission(Permissions.OrderUpdate)]
    public async Task<ActionResult<ApiResponse>> Confirm(Guid id)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFoundResponse($"Order with id {id} not found");
        }

        if (order.Status != OrderStatus.Draft)
        {
            return BadRequestResponse("Only draft orders can be confirmed");
        }

        order.Status = OrderStatus.Confirmed;
        order.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Order), order.Id, order.OrderNumber);

        return OkResponse("Order confirmed successfully");
    }

    [HttpPost("{id:guid}/process")]
    [RequirePermission(Permissions.OrderUpdate)]
    public async Task<ActionResult<ApiResponse>> Process(Guid id)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFoundResponse($"Order with id {id} not found");
        }

        order.Status = OrderStatus.Processing;
        order.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Order), order.Id, order.OrderNumber);

        return OkResponse("Order is now processing");
    }

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(Permissions.OrderUpdate)]
    public async Task<ActionResult<ApiResponse>> Complete(Guid id)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFoundResponse($"Order with id {id} not found");
        }

        order.Status = OrderStatus.Completed;
        order.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Order), order.Id, order.OrderNumber);

        return OkResponse("Order completed");
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(Permissions.OrderUpdate)]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFoundResponse($"Order with id {id} not found");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Order), order.Id, order.OrderNumber);

        return OkResponse("Order cancelled");
    }

    [HttpPost("{id:guid}/record-payment")]
    [RequirePermission(Permissions.OrderUpdate)]
    public async Task<ActionResult<ApiResponse>> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFoundResponse($"Order with id {id} not found");
        }

        order.PaidAmount += request.Amount;
        order.PaymentReference = request.Reference;
        order.PaymentMethod = request.PaymentMethod ?? order.PaymentMethod;

        if (order.PaidAmount >= order.TotalAmount)
        {
            order.PaymentStatus = PaymentStatus.Paid;
        }
        else if (order.PaidAmount > 0)
        {
            order.PaymentStatus = PaymentStatus.PartiallyPaid;
        }

        order.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Order), order.Id, order.OrderNumber);

        return OkResponse("Payment recorded");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.OrderDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFoundResponse($"Order with id {id} not found");
        }

        order.IsDeleted = true;
        order.DeletedAt = DateTime.UtcNow;
        order.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Order), order.Id, order.OrderNumber);

        return OkResponse("Order deleted");
    }

    private async Task<string> GenerateOrderNumber()
    {
        var count = await _db.Orders.CountAsync() + 1;
        return $"ORD-{count:D6}";
    }

    private static OrderDetailResponse MapToDetailResponse(Order order)
    {
        return new OrderDetailResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.Customer?.Name,
            ContactName = order.Contact != null ? $"{order.Contact.FirstName} {order.Contact.LastName}" : null,
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            DiscountPercent = order.DiscountPercent,
            TaxAmount = order.TaxAmount,
            ShippingAmount = order.ShippingAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.PaidAmount,
            BalanceAmount = order.BalanceAmount,
            Currency = order.Currency,
            BillingAddress = order.BillingAddress,
            ShippingAddress = order.ShippingAddress,
            PaymentMethod = order.PaymentMethod,
            PaymentReference = order.PaymentReference,
            Notes = order.Notes,
            Terms = order.Terms,
            Items = order.Items.Select(i => new OrderItemResponse
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountPercent = i.DiscountPercent,
                TaxPercent = i.TaxPercent,
                TotalAmount = i.TotalAmount
            }).ToList(),
            CreatedAt = order.CreatedAt
        };
    }
}

// Request/Response DTOs
public class CreateOrderRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? QuotationId { get; set; }
    public string? Currency { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal ShippingAmount { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string? PaymentMethod { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public class CreateOrderItemRequest
{
    public Guid? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateOrderRequest
{
    public string? Notes { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? ShippingAddress { get; set; }
}

public class RecordPaymentRequest
{
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? PaymentMethod { get; set; }
}

public class OrderResponse
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime OrderDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDetailResponse : OrderResponse
{
    public string? ContactName { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];
}

public class OrderItemResponse
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TotalAmount { get; set; }
}
