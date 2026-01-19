using CrmSaas.Api.Authorization;
using CrmSaas.Api.Common;
using CrmSaas.Api.Entities;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSaas.Api.Controllers;

[Authorize]
public class TenantsController : BaseController
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ITenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get all tenants
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.TenantView)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantResponse>>), 200)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TenantResponse>>>> GetAll()
    {
        var tenants = await _tenantService.GetAllAsync();
        var response = tenants.Select(MapToResponse);
        return OkResponse(response);
    }

    /// <summary>
    /// Get tenant by id
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.TenantView)]
    [ProducesResponseType(typeof(ApiResponse<TenantResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> GetById(Guid id)
    {
        var tenant = await _tenantService.GetByIdAsync(id);
        
        if (tenant == null)
        {
            return NotFoundResponse<TenantResponse>($"Tenant with id {id} not found");
        }

        return OkResponse(MapToResponse(tenant));
    }

    /// <summary>
    /// Register a new tenant
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TenantResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> Register([FromBody] CreateTenantRequest request)
    {
        if (await _tenantService.ExistsAsync(request.Identifier))
        {
            return Conflict(ApiResponse<TenantResponse>.Fail($"Tenant with identifier '{request.Identifier}' already exists"));
        }

        var tenant = await _tenantService.CreateAsync(request);
        return CreatedResponse(MapToResponse(tenant), "Tenant created successfully");
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.TenantCreate)]
    [ProducesResponseType(typeof(ApiResponse<TenantResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> Create([FromBody] CreateTenantRequest request)
    {
        if (await _tenantService.ExistsAsync(request.Identifier))
        {
            return Conflict(ApiResponse<TenantResponse>.Fail($"Tenant with identifier '{request.Identifier}' already exists"));
        }

        var tenant = await _tenantService.CreateAsync(request);
        return CreatedResponse(MapToResponse(tenant), "Tenant created successfully");
    }

    /// <summary>
    /// Update tenant
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.TenantUpdate)]
    [ProducesResponseType(typeof(ApiResponse<TenantResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> Update(Guid id, [FromBody] UpdateTenantRequest request)
    {
        try
        {
            var tenant = await _tenantService.UpdateAsync(id, request);
            return OkResponse(MapToResponse(tenant), "Tenant updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse<TenantResponse>($"Tenant with id {id} not found");
        }
    }

    /// <summary>
    /// Delete tenant
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.TenantDelete)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var result = await _tenantService.DeleteAsync(id);
        
        if (!result)
        {
            return NotFoundResponse($"Tenant with id {id} not found");
        }

        return OkResponse("Tenant deleted successfully");
    }

    private static TenantResponse MapToResponse(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Identifier = tenant.Identifier,
        Subdomain = tenant.Subdomain,
        Status = tenant.Status.ToString(),
        Plan = tenant.Plan.ToString(),
        MaxUsers = tenant.MaxUsers,
        MaxStorageBytes = tenant.MaxStorageBytes,
        LogoUrl = tenant.LogoUrl,
        PrimaryColor = tenant.PrimaryColor,
        TimeZone = tenant.TimeZone,
        Culture = tenant.Culture,
        CreatedAt = tenant.CreatedAt,
        UpdatedAt = tenant.UpdatedAt
    };
}

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public long MaxStorageBytes { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
