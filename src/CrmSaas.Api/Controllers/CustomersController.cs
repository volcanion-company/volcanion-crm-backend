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
public class CustomersController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService,
        ILogger<CustomersController> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get all customers with pagination
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.CustomerView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerResponse>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] CustomerType? type = null,
        [FromQuery] CustomerStatus? status = null)
    {
        var query = _db.Customers
            .AsNoTracking()
            .Include(c => c.AssignedToUser)
            .WhereIf(type.HasValue, c => c.Type == type!.Value)
            .WhereIf(status.HasValue, c => c.Status == status!.Value)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), c =>
                c.Name.Contains(pagination.Search!) ||
                c.Email!.Contains(pagination.Search!) ||
                c.CustomerCode!.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(c => MapToResponse(c))
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    /// <summary>
    /// Get customer by id
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.CustomerView)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<CustomerDetailResponse>>> GetById(Guid id)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .Include(c => c.Contacts)
            .Include(c => c.AssignedToUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
        {
            return NotFoundResponse<CustomerDetailResponse>($"Customer with id {id} not found");
        }

        return OkResponse(MapToDetailResponse(customer));
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.CustomerCreate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Type = request.Type,
            Email = request.Email,
            Phone = request.Phone,
            Mobile = request.Mobile,
            Website = request.Website,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Title = request.Title,
            DateOfBirth = request.DateOfBirth,
            CompanyName = request.CompanyName,
            TaxId = request.TaxId,
            Industry = request.Industry,
            EmployeeCount = request.EmployeeCount,
            AnnualRevenue = request.AnnualRevenue,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Status = request.Status,
            Source = request.Source,
            SourceDetail = request.SourceDetail,
            AssignedToUserId = request.AssignedToUserId,
            CustomerCode = request.CustomerCode,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Customer), customer.Id, customer.Name, newValues: customer);

        _logger.LogInformation("Customer {CustomerId} created by user {UserId}", customer.Id, _currentUser.UserId);

        return CreatedResponse(MapToResponse(customer), "Customer created successfully");
    }

    /// <summary>
    /// Update customer
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.CustomerUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FindAsync(id);

        if (customer == null)
        {
            return NotFoundResponse<CustomerResponse>($"Customer with id {id} not found");
        }

        var oldValues = new { customer.Name, customer.Email, customer.Status };

        customer.Name = request.Name ?? customer.Name;
        customer.Email = request.Email ?? customer.Email;
        customer.Phone = request.Phone ?? customer.Phone;
        customer.Mobile = request.Mobile ?? customer.Mobile;
        customer.Website = request.Website ?? customer.Website;
        customer.FirstName = request.FirstName ?? customer.FirstName;
        customer.LastName = request.LastName ?? customer.LastName;
        customer.Title = request.Title ?? customer.Title;
        customer.CompanyName = request.CompanyName ?? customer.CompanyName;
        customer.TaxId = request.TaxId ?? customer.TaxId;
        customer.Industry = request.Industry ?? customer.Industry;
        customer.AddressLine1 = request.AddressLine1 ?? customer.AddressLine1;
        customer.AddressLine2 = request.AddressLine2 ?? customer.AddressLine2;
        customer.City = request.City ?? customer.City;
        customer.State = request.State ?? customer.State;
        customer.PostalCode = request.PostalCode ?? customer.PostalCode;
        customer.Country = request.Country ?? customer.Country;
        customer.Status = request.Status ?? customer.Status;
        customer.AssignedToUserId = request.AssignedToUserId ?? customer.AssignedToUserId;
        customer.Notes = request.Notes ?? customer.Notes;
        customer.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Customer), customer.Id, customer.Name,
            oldValues: oldValues, newValues: new { customer.Name, customer.Email, customer.Status });

        return OkResponse(MapToResponse(customer), "Customer updated successfully");
    }

    /// <summary>
    /// Delete customer
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.CustomerDelete)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var customer = await _db.Customers.FindAsync(id);

        if (customer == null)
        {
            return NotFoundResponse($"Customer with id {id} not found");
        }

        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        customer.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Customer), customer.Id, customer.Name);

        return OkResponse("Customer deleted successfully");
    }

    private static CustomerResponse MapToResponse(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Type = c.Type.ToString(),
        Email = c.Email,
        Phone = c.Phone,
        Status = c.Status.ToString(),
        Source = c.Source.ToString(),
        CustomerCode = c.CustomerCode,
        AssignedToUserName = c.AssignedToUser?.FullName,
        CreatedAt = c.CreatedAt
    };

    private static CustomerDetailResponse MapToDetailResponse(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Type = c.Type.ToString(),
        Email = c.Email,
        Phone = c.Phone,
        Mobile = c.Mobile,
        Website = c.Website,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Title = c.Title,
        DateOfBirth = c.DateOfBirth,
        CompanyName = c.CompanyName,
        TaxId = c.TaxId,
        Industry = c.Industry,
        EmployeeCount = c.EmployeeCount,
        AnnualRevenue = c.AnnualRevenue,
        AddressLine1 = c.AddressLine1,
        AddressLine2 = c.AddressLine2,
        City = c.City,
        State = c.State,
        PostalCode = c.PostalCode,
        Country = c.Country,
        Status = c.Status.ToString(),
        Source = c.Source.ToString(),
        SourceDetail = c.SourceDetail,
        CustomerCode = c.CustomerCode,
        LifetimeValue = c.LifetimeValue,
        Notes = c.Notes,
        AssignedToUserId = c.AssignedToUserId,
        AssignedToUserName = c.AssignedToUser?.FullName,
        Contacts = c.Contacts.Select(ct => new ContactResponse
        {
            Id = ct.Id,
            FirstName = ct.FirstName,
            LastName = ct.LastName,
            Email = ct.Email,
            Phone = ct.Phone,
            JobTitle = ct.JobTitle,
            IsPrimary = ct.IsPrimary
        }).ToList(),
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public CustomerType Type { get; set; } = CustomerType.Individual;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Website { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Title { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public CustomerSource Source { get; set; } = CustomerSource.Direct;
    public string? SourceDetail { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? CustomerCode { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCustomerRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Website { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Title { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? Industry { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public CustomerStatus? Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
}

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerDetailResponse : CustomerResponse
{
    public string? Mobile { get; set; }
    public string? Website { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Title { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? SourceDetail { get; set; }
    public decimal? LifetimeValue { get; set; }
    public string? Notes { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public List<ContactResponse> Contacts { get; set; } = [];
    public DateTime? UpdatedAt { get; set; }
}

public class ContactResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public bool IsPrimary { get; set; }
}
