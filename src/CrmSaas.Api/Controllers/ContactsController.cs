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
public class ContactsController : BaseController
{
    private readonly TenantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ContactsController(
        TenantDbContext db,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission(Permissions.ContactView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ContactResponse>>>> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] Guid? customerId = null)
    {
        var query = _db.Contacts
            .AsNoTracking()
            .WhereIf(customerId.HasValue, c => c.CustomerId == customerId)
            .WhereIf(!string.IsNullOrEmpty(pagination.Search), c =>
                c.FirstName.Contains(pagination.Search!) ||
                c.LastName.Contains(pagination.Search!) ||
                c.Email!.Contains(pagination.Search!))
            .ApplySorting(pagination.SortBy ?? "CreatedAt", pagination.SortDescending);

        var result = await query
            .Select(c => new ContactResponse
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                JobTitle = c.JobTitle,
                IsPrimary = c.IsPrimary
            })
            .ToPagedResultAsync(pagination.PageNumber, pagination.PageSize);

        return OkResponse(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.ContactView)]
    public async Task<ActionResult<ApiResponse<ContactDetailResponse>>> GetById(Guid id)
    {
        var contact = await _db.Contacts
            .AsNoTracking()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact == null)
        {
            return NotFoundResponse<ContactDetailResponse>($"Contact with id {id} not found");
        }

        return OkResponse(new ContactDetailResponse
        {
            Id = contact.Id,
            CustomerId = contact.CustomerId,
            CustomerName = contact.Customer?.Name,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email,
            Phone = contact.Phone,
            Mobile = contact.Mobile,
            JobTitle = contact.JobTitle,
            Department = contact.Department,
            IsPrimary = contact.IsPrimary,
            Status = contact.Status.ToString(),
            AddressLine1 = contact.AddressLine1,
            City = contact.City,
            State = contact.State,
            Country = contact.Country,
            LinkedInUrl = contact.LinkedInUrl,
            Notes = contact.Notes,
            CreatedAt = contact.CreatedAt
        });
    }

    [HttpPost]
    [RequirePermission(Permissions.ContactCreate)]
    public async Task<ActionResult<ApiResponse<ContactResponse>>> Create([FromBody] CreateContactRequest request)
    {
        var contact = new Contact
        {
            CustomerId = request.CustomerId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Mobile = request.Mobile,
            JobTitle = request.JobTitle,
            Department = request.Department,
            IsPrimary = request.IsPrimary,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            LinkedInUrl = request.LinkedInUrl,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId
        };

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Create, nameof(Contact), contact.Id, contact.FullName);

        return CreatedResponse(new ContactResponse
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email,
            Phone = contact.Phone,
            JobTitle = contact.JobTitle,
            IsPrimary = contact.IsPrimary
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ContactUpdate)]
    public async Task<ActionResult<ApiResponse<ContactResponse>>> Update(Guid id, [FromBody] UpdateContactRequest request)
    {
        var contact = await _db.Contacts.FindAsync(id);

        if (contact == null)
        {
            return NotFoundResponse<ContactResponse>($"Contact with id {id} not found");
        }

        contact.FirstName = request.FirstName ?? contact.FirstName;
        contact.LastName = request.LastName ?? contact.LastName;
        contact.Email = request.Email ?? contact.Email;
        contact.Phone = request.Phone ?? contact.Phone;
        contact.Mobile = request.Mobile ?? contact.Mobile;
        contact.JobTitle = request.JobTitle ?? contact.JobTitle;
        contact.Department = request.Department ?? contact.Department;
        contact.IsPrimary = request.IsPrimary ?? contact.IsPrimary;
        contact.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.Update, nameof(Contact), contact.Id, contact.FullName);

        return OkResponse(new ContactResponse
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email,
            Phone = contact.Phone,
            JobTitle = contact.JobTitle,
            IsPrimary = contact.IsPrimary
        });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ContactDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var contact = await _db.Contacts.FindAsync(id);

        if (contact == null)
        {
            return NotFoundResponse($"Contact with id {id} not found");
        }

        contact.IsDeleted = true;
        contact.DeletedAt = DateTime.UtcNow;
        contact.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(AuditActions.SoftDelete, nameof(Contact), contact.Id, contact.FullName);

        return OkResponse("Contact deleted successfully");
    }
}

public class CreateContactRequest
{
    public Guid? CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public bool IsPrimary { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Notes { get; set; }
}

public class UpdateContactRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public bool? IsPrimary { get; set; }
}

public class ContactDetailResponse : ContactResponse
{
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? Mobile { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
