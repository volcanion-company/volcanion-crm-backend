# Tenants API Documentation

## Tổng quan Module

### Multi-Tenancy là gì?

**Multi-Tenancy** là kiến trúc cho phép một ứng dụng phục vụ nhiều tổ chức (tenants) độc lập trên cùng một hệ thống, với dữ liệu và cấu hình riêng biệt.

### Tại sao cần Multi-Tenancy?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SINGLE vs MULTI-TENANT SaaS                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Single-Tenant (Traditional):                                               │
│  ─────────────────────────────────────────────────────────────────────     │
│  Company A → App Instance 1 → Database 1 → Server 1                         │
│  Company B → App Instance 2 → Database 2 → Server 2                         │
│  Company C → App Instance 3 → Database 3 → Server 3                         │
│                                                                             │
│  ❌ High infrastructure cost (3× servers)                                   │
│  ❌ Hard to maintain (update 3× apps)                                       │
│  ❌ Difficult to scale                                                      │
│  ❌ Isolated improvements (A gets feature, B/C wait)                        │
│                                                                             │
│  Multi-Tenant (SaaS):                                                       │
│  ─────────────────────────────────────────────────────────────────────     │
│  Company A ┐                                                                │
│  Company B ├─→ Single App Instance → Shared Database → Single Server       │
│  Company C ┘     ↑ Tenant Isolation                                         │
│                                                                             │
│  ✅ Low infrastructure cost (1 server for all)                              │
│  ✅ Easy maintenance (update once, all benefit)                             │
│  ✅ Efficient scaling (shared resources)                                    │
│  ✅ Rapid feature delivery (deploy once)                                    │
│  ✅ Cost-effective for customers                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Tenant Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       TENANT ISOLATION ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Request Flow:                                                              │
│  ─────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  1. User Request                                                            │
│     https://acme.yourcrm.com/api/customers                                  │
│              ↓                                                              │
│  2. Subdomain Detection                                                     │
│     Middleware extracts: "acme"                                             │
│              ↓                                                              │
│  3. Tenant Resolution                                                       │
│     SELECT * FROM Tenants WHERE Subdomain = 'acme'                          │
│     → TenantId: "acme-tenant-guid"                                          │
│              ↓                                                              │
│  4. Set Tenant Context                                                      │
│     HttpContext.Items["TenantId"] = "acme-tenant-guid"                      │
│              ↓                                                              │
│  5. Data Access with Filter                                                 │
│     SELECT * FROM Customers                                                 │
│     WHERE TenantId = 'acme-tenant-guid'                                     │
│              ↓                                                              │
│  6. Response (Only Acme's data)                                             │
│                                                                             │
│  ───────────────────────────────────────────────────────────────────────    │
│                                                                             │
│  Data Isolation:                                                            │
│  ─────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  Database: CrmSaaS                                                          │
│  ┌─────────────────────────────────────────────────┐                       │
│  │ Customers Table                                 │                       │
│  ├──────────┬────────────────┬──────────────────┤                          │
│  │ TenantId │ Name           │ Email            │                          │
│  ├──────────┼────────────────┼──────────────────┤                          │
│  │ acme-id  │ Acme Corp      │ acme@example.com │  ← Acme's data          │
│  │ acme-id  │ Contoso Ltd    │ con@example.com  │                         │
│  │ beta-id  │ Beta Inc       │ beta@example.com │  ← Beta's data          │
│  │ beta-id  │ Gamma Corp     │ gamma@example.com│                         │
│  └──────────┴────────────────┴──────────────────┘                          │
│                                                                             │
│  ✅ Acme queries: WHERE TenantId = 'acme-id'                                │
│     → Only sees Acme Corp, Contoso Ltd                                      │
│  ✅ Beta queries: WHERE TenantId = 'beta-id'                                │
│     → Only sees Beta Inc, Gamma Corp                                        │
│  ✅ Data never leaks between tenants                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Tenant Plans & Limits

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SAAS PRICING TIERS                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐               │
│  │  FREE TRIAL    │  │  PROFESSIONAL  │  │   ENTERPRISE   │               │
│  ├────────────────┤  ├────────────────┤  ├────────────────┤               │
│  │ $0/month       │  │ $49/month      │  │ $299/month     │               │
│  │                │  │                │  │                │               │
│  │ • 2 Users      │  │ • 10 Users     │  │ • Unlimited    │               │
│  │ • 100 MB       │  │ • 10 GB        │  │ • 500 GB       │               │
│  │ • 100 Leads    │  │ • 5,000 Leads  │  │ • Unlimited    │               │
│  │ • 14 Days      │  │ • Core CRM     │  │ • Advanced     │               │
│  │ • Basic        │  │ • Reports      │  │ • Custom       │               │
│  │                │  │ • Email        │  │ • Dedicated    │               │
│  │                │  │                │  │ • SLA          │               │
│  └────────────────┘  └────────────────┘  └────────────────┘               │
│                                                                             │
│  Plan Enforcement:                                                          │
│  ─────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  Before creating user:                                                      │
│    if (tenant.UserCount >= tenant.MaxUsers)                                 │
│      throw "User limit reached. Upgrade plan."                              │
│                                                                             │
│  Before file upload:                                                        │
│    if (tenant.StorageUsed + fileSize > tenant.MaxStorageBytes)             │
│      throw "Storage limit reached. Upgrade plan."                           │
│                                                                             │
│  Before creating lead:                                                      │
│    if (tenant.LeadCount >= tenant.MaxLeads)                                 │
│      throw "Lead limit reached. Upgrade plan."                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Get All Tenants (Danh sách tenants)

**Nghiệp vụ thực tế:**
- Super admin xem tất cả tenants trong system
- Monitor active/inactive tenants

**Ví dụ thực tế:**
> Super admin vào System Admin panel:
> - Acme Corp (acme.yourcrm.com) - Active - Professional - 8 users
> - Beta Inc (beta.yourcrm.com) - Active - Enterprise - 50 users
> - Gamma LLC (gamma.yourcrm.com) - Suspended - Trial - 2 users (expired)
> - Delta Systems (delta.yourcrm.com) - Active - Free - 1 user
> → 4 tenants total, 3 active

---

### 2. Get Tenant by ID (Chi tiết tenant)

**Nghiệp vụ thực tế:**
- View full tenant configuration
- Check resource usage vs limits

**Ví dụ thực tế:**
> Click "Acme Corp" tenant:
> - Name: Acme Corp
> - Subdomain: acme
> - Status: Active
> - Plan: Professional ($49/month)
> - Max Users: 10 (currently 8)
> - Max Storage: 10 GB (used 6.2 GB)
> - Created: Jan 1, 2024
> - Last Login: 2 hours ago
> - Primary Color: #0066CC
> - Logo: acme-logo.png

---

### 3. Register Tenant (Public signup - khách hàng tự đăng ký)

**Nghiệp vụ thực tế:**
- New company signs up for CRM
- Creates tenant + admin user automatically

**Ví dụ thực tế:**
> Startup "NewCo" founder visits yourcrm.com:
> 1. Clicks "Start Free Trial"
> 2. Fills form:
>    - Company Name: NewCo
>    - Subdomain: newco (→ newco.yourcrm.com)
>    - Admin Email: founder@newco.com
>    - Admin Name: John Doe
>    - Password: SecurePass123!
> 3. System creates:
>    - Tenant "NewCo" with Free Trial plan
>    - Admin user "John Doe"
>    - Sample data (optional)
> 4. John receives welcome email
> 5. Can now login at newco.yourcrm.com

---

### 4. Create Tenant (Admin creates tenant)

**Nghiệp vụ thực tế:**
- Super admin manually creates tenant
- For enterprise customers or special cases

**Ví dụ thực tế:**
> Enterprise sales team closes deal with "MegaCorp":
> - Super admin creates tenant:
>   * Name: MegaCorp International
>   * Subdomain: megacorp
>   * Plan: Enterprise
>   * MaxUsers: Unlimited
>   * MaxStorage: 500 GB
> - Assigns dedicated account manager
> - Configures custom branding
> - Sets up SSO integration (optional)
> → MegaCorp ready to onboard 200 employees

---

### 5. Update Tenant (Cập nhật tenant)

**Nghiệp vụ thực tế:**
- Update tenant settings
- Change plan, limits, branding

**Ví dụ thực tế:**
> "Acme Corp" grows from 8 to 15 employees:
> - Current: Professional plan (max 10 users)
> - Update:
>   * Plan: Enterprise
>   * MaxUsers: 50
>   * MaxStorage: 100 GB (from 10 GB)
>   * Status: Active (unchanged)
> - Billing automatically adjusted
> → Acme can now add 42 more users

---

### 6. Delete Tenant (Xóa tenant)

**Nghiệp vụ thực tế:**
- Customer cancels subscription
- Remove all tenant data

**Ví dụ thực tế:**
> "Gamma LLC" goes out of business:
> - Mark tenant Status: Deleted
> - Schedule data deletion (30 day grace period)
> - Send final invoice
> - Archive data for compliance
> - After 30 days: Permanently delete
>   * All users deleted
>   * All customers, leads, opps deleted
>   * Subdomain freed up
> → gamma.yourcrm.com available for new tenant

---

## Tenant Status Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        TENANT STATUS FLOW                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────┐                                                               │
│  │  TRIAL   │  (14 days free)                                               │
│  └────┬─────┘                                                               │
│       │                                                                     │
│       │ Convert to paid                                                     │
│       ▼                                                                     │
│  ┌──────────┐                                                               │
│  │  ACTIVE  │  (Paying customer)                                            │
│  └────┬─────┘                                                               │
│       │                                                                     │
│       │ Payment failed                                                      │
│       ▼                                                                     │
│  ┌────────────┐                                                             │
│  │ SUSPENDED  │  (Grace period: 7 days)                                     │
│  └────┬───────┘                                                             │
│       │                                                                     │
│       ├─────► Payment received → Back to ACTIVE                             │
│       │                                                                     │
│       └─────► No payment → INACTIVE                                         │
│                     │                                                       │
│                     │ Delete request                                        │
│                     ▼                                                       │
│                ┌──────────┐                                                 │
│                │ DELETED  │  (30 day grace, then purged)                    │
│                └──────────┘                                                 │
│                                                                             │
│  Status Access Rules:                                                       │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Trial: Full access, limited time                                         │
│  • Active: Full access                                                      │
│  • Suspended: Read-only access (can export data)                            │
│  • Inactive: No access (show upgrade prompt)                                │
│  • Deleted: No access (data queued for deletion)                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Subdomain Routing

### How Subdomain Works

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SUBDOMAIN ROUTING                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Setup:                                                                     │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Domain: yourcrm.com                                                      │
│  • Wildcard DNS: *.yourcrm.com → Server IP                                  │
│  • Application: ASP.NET Core + Tenant Middleware                            │
│                                                                             │
│  Examples:                                                                  │
│  ─────────────────────────────────────────────────────────────────────     │
│                                                                             │
│  Request 1: https://acme.yourcrm.com/api/customers                          │
│    ├─ Extract subdomain: "acme"                                             │
│    ├─ Lookup tenant: WHERE Subdomain = 'acme'                               │
│    ├─ Set context: TenantId = acme-guid                                     │
│    └─ Query: SELECT * FROM Customers WHERE TenantId = acme-guid             │
│                                                                             │
│  Request 2: https://beta.yourcrm.com/api/leads                              │
│    ├─ Extract subdomain: "beta"                                             │
│    ├─ Lookup tenant: WHERE Subdomain = 'beta'                               │
│    ├─ Set context: TenantId = beta-guid                                     │
│    └─ Query: SELECT * FROM Leads WHERE TenantId = beta-guid                 │
│                                                                             │
│  Request 3: https://yourcrm.com (no subdomain)                              │
│    └─ Show marketing site / login page                                      │
│                                                                             │
│  Invalid subdomain: https://notexist.yourcrm.com                            │
│    └─ Return 404: Tenant not found                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Best Practices

### 1. Subdomain Validation

```csharp
public class SubdomainValidator
{
    // ✅ Valid subdomains
    var valid = new[] { "acme", "beta-corp", "test123", "my-company" };
    
    // ❌ Invalid subdomains
    var invalid = new[] 
    { 
        "www",          // Reserved
        "api",          // Reserved
        "admin",        // Reserved
        "-acme",        // Can't start with hyphen
        "acme-",        // Can't end with hyphen
        "ac",           // Too short (min 3 chars)
        "very-long-subdomain-exceeds-63-chars..." // Too long
    };
    
    // Regex validation
    private static readonly Regex SubdomainRegex = 
        new(@"^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])?$", RegexOptions.Compiled);
        
    public static bool IsValid(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return false;
        if (ReservedSubdomains.Contains(subdomain)) return false;
        return SubdomainRegex.IsMatch(subdomain);
    }
}
```

### 2. Tenant Context Middleware

```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context, MasterDbContext db)
    {
        // Extract subdomain from host
        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);
        
        if (!string.IsNullOrEmpty(subdomain))
        {
            // Lookup tenant
            var tenant = await db.Tenants
                .FirstOrDefaultAsync(t => 
                    t.Subdomain == subdomain && 
                    t.Status != TenantStatus.Deleted);
            
            if (tenant == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Tenant not found");
                return;
            }
            
            // Check tenant status
            if (tenant.Status == TenantStatus.Suspended || 
                tenant.Status == TenantStatus.Inactive)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Tenant suspended. Please contact support.");
                return;
            }
            
            // Set tenant context
            context.Items["TenantId"] = tenant.Id;
            context.Items["Tenant"] = tenant;
        }
        
        await _next(context);
    }
    
    private string? ExtractSubdomain(string host)
    {
        // yourcrm.com → null (no subdomain)
        // acme.yourcrm.com → acme
        // api.acme.yourcrm.com → api.acme (multi-level)
        
        var parts = host.Split('.');
        if (parts.Length <= 2) return null; // No subdomain
        
        return parts[0]; // Return first part
    }
}
```

### 3. Limit Enforcement

```csharp
public class TenantLimitService
{
    public async Task<bool> CanCreateUser(Guid tenantId)
    {
        var tenant = await GetTenantAsync(tenantId);
        var currentCount = await GetUserCountAsync(tenantId);
        
        if (tenant.MaxUsers > 0 && currentCount >= tenant.MaxUsers)
        {
            throw new TenantLimitExceededException(
                $"User limit reached ({tenant.MaxUsers}). Please upgrade your plan.");
        }
        
        return true;
    }
    
    public async Task<bool> CanUploadFile(Guid tenantId, long fileSize)
    {
        var tenant = await GetTenantAsync(tenantId);
        var currentUsage = await GetStorageUsageAsync(tenantId);
        
        if (currentUsage + fileSize > tenant.MaxStorageBytes)
        {
            var available = tenant.MaxStorageBytes - currentUsage;
            throw new TenantLimitExceededException(
                $"Storage limit reached. Available: {FormatBytes(available)}");
        }
        
        return true;
    }
}
```

### 4. Tenant Onboarding Flow

```csharp
public async Task<TenantRegistrationResult> RegisterTenantAsync(TenantRegistrationRequest request)
{
    // 1. Validate subdomain
    if (!SubdomainValidator.IsValid(request.Subdomain))
        throw new ValidationException("Invalid subdomain format");
        
    if (await SubdomainExistsAsync(request.Subdomain))
        throw new ValidationException("Subdomain already taken");
    
    // 2. Create tenant
    var tenant = new Tenant
    {
        Name = request.CompanyName,
        Subdomain = request.Subdomain.ToLower(),
        Status = TenantStatus.Trial,
        Plan = TenantPlan.Trial,
        MaxUsers = 2,
        MaxStorageBytes = 100 * 1024 * 1024, // 100 MB
        TrialEndsAt = DateTime.UtcNow.AddDays(14)
    };
    
    await _db.Tenants.AddAsync(tenant);
    await _db.SaveChangesAsync();
    
    // 3. Create admin user
    var adminUser = new User
    {
        TenantId = tenant.Id,
        Email = request.AdminEmail,
        FirstName = request.AdminFirstName,
        LastName = request.AdminLastName,
        Status = UserStatus.Active,
        RoleId = adminRoleId
    };
    
    await _userService.CreateUserAsync(adminUser, request.Password);
    
    // 4. Create sample data (optional)
    await SeedSampleDataAsync(tenant.Id);
    
    // 5. Send welcome email
    await _emailService.SendWelcomeEmailAsync(adminUser.Email, tenant.Subdomain);
    
    // 6. Log audit
    await _auditService.LogAsync(AuditActions.Create, nameof(Tenant), tenant.Id, 
        $"Tenant '{tenant.Name}' registered with subdomain '{tenant.Subdomain}'");
    
    return new TenantRegistrationResult
    {
        TenantId = tenant.Id,
        Subdomain = tenant.Subdomain,
        Url = $"https://{tenant.Subdomain}.yourcrm.com",
        AdminUserId = adminUser.Id
    };
}
```

---

## Technical Overview

**Base URL:** `/api/v1/tenants`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Tenants

Lấy danh sách tất cả tenants (Super Admin only).

```
GET /api/v1/tenants
```

**Permission Required:** Super Admin

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "tenant-guid",
      "name": "Acme Corp",
      "subdomain": "acme",
      "status": "Active",
      "plan": "Professional",
      "maxUsers": 10,
      "currentUsers": 8,
      "maxStorageBytes": 10737418240,
      "storageUsed": 6666666666,
      "createdAt": "2024-01-01T00:00:00Z",
      "trialEndsAt": null
    }
  ]
}
```

---

### 2. Get Tenant by ID

Lấy chi tiết một tenant.

```
GET /api/v1/tenants/{id}
```

**Permission Required:** Super Admin or own tenant

#### Response

```json
{
  "success": true,
  "data": {
    "id": "tenant-guid",
    "name": "Acme Corp",
    "subdomain": "acme",
    "status": "Active",
    "plan": "Professional",
    "maxUsers": 10,
    "maxStorageBytes": 10737418240,
    "logoUrl": "https://cdn.yourcrm.com/tenants/acme/logo.png",
    "primaryColor": "#0066CC",
    "timeZone": "America/New_York",
    "culture": "en-US",
    "createdAt": "2024-01-01T00:00:00Z",
    "trialEndsAt": null
  }
}
```

---

### 3. Register Tenant (Public)

Đăng ký tenant mới (public endpoint).

```
POST /api/v1/tenants/register
```

**Authentication:** Not required (AllowAnonymous)

#### Request Body

```json
{
  "companyName": "NewCo",
  "subdomain": "newco",
  "adminEmail": "founder@newco.com",
  "adminFirstName": "John",
  "adminLastName": "Doe",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!",
  "phoneNumber": "+1-555-0123",
  "industry": "Technology"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `companyName` | string | **Yes** | Company name (tenant name) |
| `subdomain` | string | **Yes** | Unique subdomain (3-63 chars, lowercase, alphanumeric + hyphens) |
| `adminEmail` | string | **Yes** | Admin user email |
| `adminFirstName` | string | **Yes** | Admin first name |
| `adminLastName` | string | **Yes** | Admin last name |
| `password` | string | **Yes** | Admin password (min 8 chars) |
| `confirmPassword` | string | **Yes** | Must match password |
| `phoneNumber` | string | No | Contact phone |
| `industry` | string | No | Business industry |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "tenantId": "new-tenant-guid",
    "subdomain": "newco",
    "url": "https://newco.yourcrm.com",
    "adminUserId": "admin-user-guid",
    "trialEndsAt": "2026-02-01T00:00:00Z"
  },
  "message": "Welcome to YourCRM! Your 14-day free trial has started."
}
```

---

### 4. Create Tenant (Admin)

Tạo tenant mới (Admin manually creates).

```
POST /api/v1/tenants
```

**Permission Required:** Super Admin

#### Request Body

```json
{
  "name": "MegaCorp International",
  "subdomain": "megacorp",
  "plan": "Enterprise",
  "maxUsers": 0,
  "maxStorageBytes": 536870912000,
  "primaryColor": "#FF5500",
  "logoUrl": "https://cdn.example.com/logo.png"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Tenant name |
| `subdomain` | string | **Yes** | Unique subdomain |
| `plan` | TenantPlan | **Yes** | Plan type (Trial/Free/Professional/Enterprise) |
| `maxUsers` | int | **Yes** | Max users (0 = unlimited) |
| `maxStorageBytes` | long | **Yes** | Max storage in bytes |
| `primaryColor` | string | No | Brand primary color (hex) |
| `logoUrl` | string | No | Logo URL |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-tenant-guid",
    "name": "MegaCorp International",
    "subdomain": "megacorp",
    "status": "Active",
    "plan": "Enterprise",
    "maxUsers": 0,
    "createdAt": "2026-01-18T10:00:00Z"
  }
}
```

---

### 5. Update Tenant

Cập nhật tenant info.

```
PUT /api/v1/tenants/{id}
```

**Permission Required:** Super Admin

#### Request Body (All fields optional)

```json
{
  "name": "Acme Corporation",
  "plan": "Enterprise",
  "maxUsers": 50,
  "maxStorageBytes": 107374182400,
  "status": "Active",
  "primaryColor": "#0066CC",
  "logoUrl": "https://cdn.example.com/acme-logo.png"
}
```

---

### 6. Delete Tenant

Xóa tenant (soft delete).

```
DELETE /api/v1/tenants/{id}
```

**Permission Required:** Super Admin

#### Response

```json
{
  "success": true,
  "message": "Tenant marked for deletion. Data will be purged after 30 days."
}
```

**Note:** This is a soft delete. Tenant status set to "Deleted", data purged after grace period.

---

## Enums

### TenantStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Trial | Free trial period |
| 1 | Active | Active paid customer |
| 2 | Suspended | Payment failed, grace period |
| 3 | Inactive | No payment, no access |
| 4 | Deleted | Marked for deletion |

### TenantPlan

| Value | Name | Description |
|-------|------|-------------|
| 0 | Trial | 14-day free trial |
| 1 | Free | Free plan (limited) |
| 2 | Professional | Mid-tier paid plan |
| 3 | Enterprise | Full-featured plan |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `tenant.view` | View tenant info |
| `tenant.create` | Create new tenants |
| `tenant.update` | Update tenant settings |
| `tenant.delete` | Delete tenants |

**Note:** Most tenant management requires Super Admin role.

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
