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
public class QuotationsController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public QuotationsController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.QuotationView)]
    public async Task<ActionResult<ApiResponse<PagedResult<QuotationResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] QuotationStatus? status = null,
        [FromQuery] Guid? customerId = null)
    {
        var query = _db.Quotations
            .AsNoTracking()
            .Include(q => q.Customer)
            .WhereIf(status.HasValue, q => q.Status == status!.Value)
            .WhereIf(customerId.HasValue, q => q.CustomerId == customerId)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), q =>
                q.QuotationNumber.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(q => new QuotationResponse
            {
                Id = q.Id,
                QuotationNumber = q.QuotationNumber,
                CustomerName = q.Customer != null ? q.Customer.Name : null,
                Status = q.Status.ToString(),
                SubTotal = q.SubTotal,
                DiscountAmount = q.DiscountAmount,
                TaxAmount = q.TaxAmount,
                TotalAmount = q.TotalAmount,
                Currency = q.Currency,
                QuotationDate = q.QuotationDate,
                ExpiryDate = q.ExpiryDate,
                CreatedAt = q.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.QuotationView)]
    public async Task<ActionResult<ApiResponse<QuotationDetailResponse>>> GetById(Guid id)
    {
        var quotation = await _db.Quotations
            .AsNoTracking()
            .Include(q => q.Customer)
            .Include(q => q.Opportunity)
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null)
        {
            return NotFoundResponse<QuotationDetailResponse>($"Quotation with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(quotation));
    }

    [HttpPost]
    [RequirePermission(Permissions.QuotationCreate)]
    public async Task<ActionResult<ApiResponse<QuotationResponse>>> Create([FromBody] CreateQuotationRequest request)
    {
        var quotationNumber = await GenerateQuotationNumber();

        var quotation = new Quotation
        {
            QuotationNumber = quotationNumber,
            CustomerId = request.CustomerId,
            ContactId = request.ContactId,
            OpportunityId = request.OpportunityId,
            Currency = request.Currency ?? "USD",
            DiscountAmount = request.DiscountAmount,
            DiscountPercent = request.DiscountPercent,
            Notes = request.Notes,
            Terms = request.Terms,
            QuotationDate = request.QuotationDate ?? DateTime.UtcNow,
            ExpiryDate = request.ExpiryDate ?? DateTime.UtcNow.AddDays(30),
            BillingAddress = request.BillingAddress,
            ShippingAddress = request.ShippingAddress,
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

            quotation.Items.Add(new QuotationItem
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

        quotation.SubTotal = subTotal;
        quotation.TaxAmount = quotation.Items.Sum(i => i.TaxAmount);
        quotation.TotalAmount = quotation.SubTotal - quotation.DiscountAmount + quotation.TaxAmount;

        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Quotation), quotation.Id, quotation.QuotationNumber);

        return CreatedResponse(new QuotationResponse
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            Status = quotation.Status.ToString(),
            TotalAmount = quotation.TotalAmount,
            CreatedAt = quotation.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.QuotationUpdate)]
    public async Task<ActionResult<ApiResponse<QuotationResponse>>> Update(Guid id, [FromBody] UpdateQuotationRequest request)
    {
        var quotation = await _db.Quotations
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null)
        {
            return NotFoundResponse<QuotationResponse>($"Quotation with id {id} not found");
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return BadRequestResponse<QuotationResponse>("Only draft quotations can be updated");
        }

        quotation.Notes = request.Notes ?? quotation.Notes;
        quotation.ExpiryDate = request.ExpiryDate ?? quotation.ExpiryDate;
        quotation.DiscountAmount = request.DiscountAmount ?? quotation.DiscountAmount;
        quotation.DiscountPercent = request.DiscountPercent ?? quotation.DiscountPercent;
        quotation.UpdatedBy = _currentUser.UserId;

        // Recalculate totals
        quotation.TotalAmount = quotation.SubTotal - quotation.DiscountAmount + quotation.TaxAmount;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Quotation), quotation.Id, quotation.QuotationNumber);

        return OkResponse(new QuotationResponse
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            Status = quotation.Status.ToString(),
            TotalAmount = quotation.TotalAmount,
            CreatedAt = quotation.CreatedAt
        });
    }

    [HttpPost("{id:guid}/send")]
    [RequirePermission(Permissions.QuotationUpdate)]
    public async Task<ActionResult<ApiResponse>> Send(Guid id)
    {
        var quotation = await _db.Quotations.FindAsync(id);

        if (quotation == null)
        {
            return NotFoundResponse($"Quotation with id {id} not found");
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return BadRequestResponse("Only draft quotations can be sent");
        }

        quotation.Status = QuotationStatus.Sent;
        quotation.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Quotation), quotation.Id, quotation.QuotationNumber);

        return OkResponse("Quotation sent successfully");
    }

    [HttpPost("{id:guid}/accept")]
    [RequirePermission(Permissions.QuotationUpdate)]
    public async Task<ActionResult<ApiResponse>> Accept(Guid id)
    {
        var quotation = await _db.Quotations.FindAsync(id);

        if (quotation == null)
        {
            return NotFoundResponse($"Quotation with id {id} not found");
        }

        quotation.Status = QuotationStatus.Accepted;
        quotation.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Quotation), quotation.Id, quotation.QuotationNumber);

        return OkResponse("Quotation accepted");
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(Permissions.QuotationUpdate)]
    public async Task<ActionResult<ApiResponse>> Reject(Guid id)
    {
        var quotation = await _db.Quotations.FindAsync(id);

        if (quotation == null)
        {
            return NotFoundResponse($"Quotation with id {id} not found");
        }

        quotation.Status = QuotationStatus.Rejected;
        quotation.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Quotation), quotation.Id, quotation.QuotationNumber);

        return OkResponse("Quotation rejected");
    }

    [HttpPost("{id:guid}/convert-to-order")]
    [RequirePermission(Permissions.OrderCreate)]
    public async Task<ActionResult<ApiResponse<OrderConversionResponse>>> ConvertToOrder(Guid id)
    {
        var quotation = await _db.Quotations
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null)
        {
            return NotFoundResponse<OrderConversionResponse>($"Quotation with id {id} not found");
        }

        if (quotation.Status != QuotationStatus.Accepted)
        {
            return BadRequestResponse<OrderConversionResponse>("Only accepted quotations can be converted to orders");
        }

        if (quotation.ConvertedToOrderId.HasValue)
        {
            return BadRequestResponse<OrderConversionResponse>("Quotation is already converted to an order");
        }

        var orderNumber = await GenerateOrderNumber();

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = quotation.CustomerId,
            ContactId = quotation.ContactId,
            QuotationId = quotation.Id,
            Currency = quotation.Currency,
            SubTotal = quotation.SubTotal,
            DiscountAmount = quotation.DiscountAmount,
            DiscountPercent = quotation.DiscountPercent,
            TaxAmount = quotation.TaxAmount,
            TotalAmount = quotation.TotalAmount,
            Notes = quotation.Notes,
            Terms = quotation.Terms,
            BillingAddress = quotation.BillingAddress,
            ShippingAddress = quotation.ShippingAddress,
            CreatedBy = _currentUser.UserId
        };

        foreach (var item in quotation.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                DiscountAmount = item.DiscountAmount,
                TaxPercent = item.TaxPercent,
                TaxAmount = item.TaxAmount,
                TotalAmount = item.TotalAmount,
                SortOrder = item.SortOrder
            });
        }

        quotation.ConvertedToOrderId = order.Id;
        quotation.UpdatedBy = _currentUser.UserId;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Convert, nameof(Quotation), quotation.Id, quotation.QuotationNumber);

        return OkResponse(new OrderConversionResponse
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber
        });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.QuotationDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var quotation = await _db.Quotations.FindAsync(id);

        if (quotation == null)
        {
            return NotFoundResponse($"Quotation with id {id} not found");
        }

        quotation.IsDeleted = true;
        quotation.DeletedAt = DateTime.UtcNow;
        quotation.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Quotation), quotation.Id, quotation.QuotationNumber);

        return OkResponse("Quotation deleted");
    }

    private async Task<string> GenerateQuotationNumber()
    {
        var count = await _db.Quotations.CountAsync() + 1;
        return $"QUO-{count:D6}";
    }

    private async Task<string> GenerateOrderNumber()
    {
        var count = await _db.Orders.CountAsync() + 1;
        return $"ORD-{count:D6}";
    }

    private static QuotationDetailResponse MapToDetailResponse(Quotation quotation)
    {
        return new QuotationDetailResponse
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            CustomerName = quotation.Customer?.Name,
            OpportunityName = quotation.Opportunity?.Name,
            Status = quotation.Status.ToString(),
            QuotationDate = quotation.QuotationDate,
            ExpiryDate = quotation.ExpiryDate,
            SubTotal = quotation.SubTotal,
            DiscountAmount = quotation.DiscountAmount,
            DiscountPercent = quotation.DiscountPercent,
            TaxAmount = quotation.TaxAmount,
            TotalAmount = quotation.TotalAmount,
            Currency = quotation.Currency,
            BillingAddress = quotation.BillingAddress,
            ShippingAddress = quotation.ShippingAddress,
            Notes = quotation.Notes,
            Terms = quotation.Terms,
            Items = quotation.Items.Select(i => new QuotationItemResponse
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
            CreatedAt = quotation.CreatedAt
        };
    }
}

// Request/Response DTOs
public class CreateQuotationRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? OpportunityId { get; set; }
    public string? Currency { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public DateTime? QuotationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public List<CreateQuotationItemRequest> Items { get; set; } = [];
}

public class CreateQuotationItemRequest
{
    public Guid? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateQuotationRequest
{
    public string? Notes { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
}

public class QuotationResponse
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime QuotationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QuotationDetailResponse : QuotationResponse
{
    public string? OpportunityName { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public List<QuotationItemResponse> Items { get; set; } = [];
}

public class QuotationItemResponse
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

public class OrderConversionResponse
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
}
