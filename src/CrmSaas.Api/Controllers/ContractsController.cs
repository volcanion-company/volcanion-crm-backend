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
public class ContractsController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ContractsController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.ContractView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ContractResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] ContractStatus? status = null,
        [FromQuery] Guid? customerId = null)
    {
        var query = _db.Contracts
            .AsNoTracking()
            .Include(c => c.Customer)
            .WhereIf(status.HasValue, c => c.Status == status!.Value)
            .WhereIf(customerId.HasValue, c => c.CustomerId == customerId)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), c =>
                c.ContractNumber.Contains(pagination.Search!) ||
                c.Title.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(c => new ContractResponse
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                Title = c.Title,
                CustomerName = c.Customer != null ? c.Customer.Name : null,
                Status = c.Status.ToString(),
                Type = c.Type.ToString(),
                Value = c.Value,
                Currency = c.Currency,
                BillingFrequency = c.BillingFrequency.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RenewalDate = c.RenewalDate,
                AutoRenew = c.AutoRenew,
                CreatedAt = c.CreatedAt
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("expiring-soon")]
    [RequirePermission(Permissions.ContractView)]
    public async Task<ActionResult<ApiResponse<List<ContractResponse>>>> GetExpiringSoon([FromQuery] int days = 30)
    {
        var expirationDate = DateTime.UtcNow.AddDays(days);

        var contracts = await _db.Contracts
            .AsNoTracking()
            .Include(c => c.Customer)
            .Where(c => c.Status == ContractStatus.Active)
            .Where(c => c.EndDate <= expirationDate)
            .OrderBy(c => c.EndDate)
            .Select(c => new ContractResponse
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                Title = c.Title,
                CustomerName = c.Customer != null ? c.Customer.Name : null,
                Status = c.Status.ToString(),
                Type = c.Type.ToString(),
                Value = c.Value,
                Currency = c.Currency,
                BillingFrequency = c.BillingFrequency.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RenewalDate = c.RenewalDate,
                AutoRenew = c.AutoRenew,
                CreatedAt = c.CreatedAt
            })
            .Take(50)
            .ToListAsync();

        return OkResponse(contracts);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.ContractView)]
    public async Task<ActionResult<ApiResponse<ContractDetailResponse>>> GetById(Guid id)
    {
        var contract = await _db.Contracts
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Contact)
            .Include(c => c.Order)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
        {
            return NotFoundResponse<ContractDetailResponse>($"Contract with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(contract));
    }

    [HttpPost]
    [RequirePermission(Permissions.ContractCreate)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> Create([FromBody] CreateContractRequest request)
    {
        var contractNumber = await GenerateContractNumber();

        var contract = new Contract
        {
            ContractNumber = contractNumber,
            Title = request.Title,
            CustomerId = request.CustomerId,
            ContactId = request.ContactId,
            OrderId = request.OrderId,
            Type = request.Type,
            Value = request.Value,
            Currency = request.Currency ?? "USD",
            BillingFrequency = request.BillingFrequency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SignedDate = request.SignedDate,
            AutoRenew = request.AutoRenew,
            RenewalPeriodMonths = request.RenewalPeriodMonths,
            NoticePeriodDays = request.NoticePeriodDays,
            Description = request.Description,
            Terms = request.Terms,
            DocumentUrl = request.DocumentUrl,
            CreatedBy = _currentUser.UserId
        };

        if (contract.AutoRenew && contract.NoticePeriodDays.HasValue)
        {
            contract.RenewalDate = contract.EndDate.AddDays(-contract.NoticePeriodDays.Value);
        }

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Contract), contract.Id, contract.ContractNumber);

        return CreatedResponse(new ContractResponse
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            Title = contract.Title,
            Status = contract.Status.ToString(),
            Type = contract.Type.ToString(),
            Value = contract.Value,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            CreatedAt = contract.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ContractUpdate)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> Update(Guid id, [FromBody] UpdateContractRequest request)
    {
        var contract = await _db.Contracts.FindAsync(id);

        if (contract == null)
        {
            return NotFoundResponse<ContractResponse>($"Contract with id {id} not found");
        }

        contract.Title = request.Title ?? contract.Title;
        contract.Description = request.Description ?? contract.Description;
        contract.Value = request.Value ?? contract.Value;
        contract.AutoRenew = request.AutoRenew ?? contract.AutoRenew;
        contract.RenewalPeriodMonths = request.RenewalPeriodMonths ?? contract.RenewalPeriodMonths;
        contract.NoticePeriodDays = request.NoticePeriodDays ?? contract.NoticePeriodDays;
        contract.Terms = request.Terms ?? contract.Terms;
        contract.DocumentUrl = request.DocumentUrl ?? contract.DocumentUrl;
        contract.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Contract), contract.Id, contract.ContractNumber);

        return OkResponse(new ContractResponse
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            Title = contract.Title,
            Status = contract.Status.ToString(),
            Type = contract.Type.ToString(),
            Value = contract.Value,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            CreatedAt = contract.CreatedAt
        });
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(Permissions.ContractUpdate)]
    public async Task<ActionResult<ApiResponse>> Activate(Guid id)
    {
        var contract = await _db.Contracts.FindAsync(id);

        if (contract == null)
        {
            return NotFoundResponse($"Contract with id {id} not found");
        }

        if (contract.Status != ContractStatus.Draft && contract.Status != ContractStatus.PendingApproval && contract.Status != ContractStatus.Approved)
        {
            return BadRequestResponse("Only draft, pending, or approved contracts can be activated");
        }

        contract.Status = ContractStatus.Active;
        contract.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Contract), contract.Id, contract.ContractNumber);

        return OkResponse("Contract activated successfully");
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(Permissions.ContractUpdate)]
    public async Task<ActionResult<ApiResponse>> Approve(Guid id)
    {
        var contract = await _db.Contracts.FindAsync(id);

        if (contract == null)
        {
            return NotFoundResponse($"Contract with id {id} not found");
        }

        contract.Status = ContractStatus.Approved;
        contract.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Contract), contract.Id, contract.ContractNumber);

        return OkResponse("Contract approved");
    }

    [HttpPost("{id:guid}/renew")]
    [RequirePermission(Permissions.ContractUpdate)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> Renew(Guid id)
    {
        var contract = await _db.Contracts.FindAsync(id);

        if (contract == null)
        {
            return NotFoundResponse<ContractResponse>($"Contract with id {id} not found");
        }

        if (contract.Status != ContractStatus.Active && contract.Status != ContractStatus.Expired)
        {
            return BadRequestResponse<ContractResponse>("Only active or expired contracts can be renewed");
        }

        // Create new contract based on current one
        var contractNumber = await GenerateContractNumber();
        var renewalMonths = contract.RenewalPeriodMonths ?? 12;
        var newStartDate = contract.EndDate;
        var newEndDate = newStartDate.AddMonths(renewalMonths);

        var newContract = new Contract
        {
            ContractNumber = contractNumber,
            Title = $"{contract.Title} - Renewal",
            CustomerId = contract.CustomerId,
            ContactId = contract.ContactId,
            Type = contract.Type,
            Value = contract.Value,
            Currency = contract.Currency,
            BillingFrequency = contract.BillingFrequency,
            StartDate = newStartDate,
            EndDate = newEndDate,
            AutoRenew = contract.AutoRenew,
            RenewalPeriodMonths = contract.RenewalPeriodMonths,
            NoticePeriodDays = contract.NoticePeriodDays,
            Description = contract.Description,
            Terms = contract.Terms,
            Status = ContractStatus.Draft,
            CreatedBy = _currentUser.UserId
        };

        // Update original contract status
        contract.Status = ContractStatus.Renewed;
        contract.UpdatedBy = _currentUser.UserId;

        _db.Contracts.Add(newContract);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Contract), contract.Id, contract.ContractNumber);
        await _auditService.LogAsync(AuditActions.Create, nameof(Contract), newContract.Id, newContract.ContractNumber);

        return OkResponse(new ContractResponse
        {
            Id = newContract.Id,
            ContractNumber = newContract.ContractNumber,
            Title = newContract.Title,
            Status = newContract.Status.ToString(),
            Type = newContract.Type.ToString(),
            Value = newContract.Value,
            StartDate = newContract.StartDate,
            EndDate = newContract.EndDate,
            CreatedAt = newContract.CreatedAt
        });
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(Permissions.ContractUpdate)]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id)
    {
        var contract = await _db.Contracts.FindAsync(id);

        if (contract == null)
        {
            return NotFoundResponse($"Contract with id {id} not found");
        }

        contract.Status = ContractStatus.Cancelled;
        contract.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.StatusChange, nameof(Contract), contract.Id, contract.ContractNumber);

        return OkResponse("Contract cancelled");
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ContractDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var contract = await _db.Contracts.FindAsync(id);

        if (contract == null)
        {
            return NotFoundResponse($"Contract with id {id} not found");
        }

        contract.IsDeleted = true;
        contract.DeletedAt = DateTime.UtcNow;
        contract.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Contract), contract.Id, contract.ContractNumber);

        return OkResponse("Contract deleted");
    }

    private async Task<string> GenerateContractNumber()
    {
        var count = await _db.Contracts.CountAsync() + 1;
        return $"CTR-{count:D6}";
    }

    private static ContractDetailResponse MapToDetailResponse(Contract contract)
    {
        return new ContractDetailResponse
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            Title = contract.Title,
            CustomerName = contract.Customer?.Name,
            ContactName = contract.Contact != null ? $"{contract.Contact.FirstName} {contract.Contact.LastName}" : null,
            OrderNumber = contract.Order?.OrderNumber,
            Status = contract.Status.ToString(),
            Type = contract.Type.ToString(),
            Value = contract.Value,
            Currency = contract.Currency,
            BillingFrequency = contract.BillingFrequency.ToString(),
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            SignedDate = contract.SignedDate,
            RenewalDate = contract.RenewalDate,
            AutoRenew = contract.AutoRenew,
            RenewalPeriodMonths = contract.RenewalPeriodMonths,
            NoticePeriodDays = contract.NoticePeriodDays,
            Description = contract.Description,
            Terms = contract.Terms,
            DocumentUrl = contract.DocumentUrl,
            CreatedAt = contract.CreatedAt
        };
    }
}

// Request/Response DTOs
public class CreateContractRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? OrderId { get; set; }
    public ContractType Type { get; set; } = ContractType.Service;
    public decimal Value { get; set; }
    public string? Currency { get; set; }
    public BillingFrequency BillingFrequency { get; set; } = BillingFrequency.Monthly;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? SignedDate { get; set; }
    public bool AutoRenew { get; set; }
    public int? RenewalPeriodMonths { get; set; }
    public int? NoticePeriodDays { get; set; }
    public string? Description { get; set; }
    public string? Terms { get; set; }
    public string? DocumentUrl { get; set; }
}

public class UpdateContractRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Value { get; set; }
    public bool? AutoRenew { get; set; }
    public int? RenewalPeriodMonths { get; set; }
    public int? NoticePeriodDays { get; set; }
    public string? Terms { get; set; }
    public string? DocumentUrl { get; set; }
}

public class ContractResponse
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingFrequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? RenewalDate { get; set; }
    public bool AutoRenew { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ContractDetailResponse : ContractResponse
{
    public string? ContactName { get; set; }
    public string? OrderNumber { get; set; }
    public DateTime? SignedDate { get; set; }
    public int? RenewalPeriodMonths { get; set; }
    public int? NoticePeriodDays { get; set; }
    public string? Description { get; set; }
    public string? Terms { get; set; }
    public string? DocumentUrl { get; set; }
}
