# Roles & Permissions API Documentation

## Tổng quan Module

### RBAC là gì?

**RBAC (Role-Based Access Control)** là hệ thống phân quyền dựa trên vai trò. Thay vì gán permissions trực tiếp cho từng user, ta gán permissions cho roles, rồi assign roles cho users.

### Tại sao cần RBAC?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              WITHOUT RBAC vs WITH RBAC                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Without RBAC (Direct Permission):   With RBAC (Role-Based):                │
│  ──────────────────────────────────  ────────────────────                  │
│                                                                             │
│  User 1: [perm1, perm2, perm3, ...]  Role: Sales                           │
│  User 2: [perm1, perm2, perm3, ...]    Permissions: [perm1, perm2, ...]    │
│  User 3: [perm1, perm2, perm3, ...]                                        │
│  ... (50 sales reps)                 Users: [User1, User2, ... User50]     │
│                                        ↑ All inherit Sales permissions      │
│  ❌ Manage 50× separately              ✅ Manage once, apply to all         │
│  ❌ Inconsistent permissions           ✅ Consistent across role             │
│  ❌ Hard to audit                      ✅ Easy to audit                      │
│  ❌ Error-prone                        ✅ Scalable & maintainable            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Base URL:** `/api/v1/roles`

**Authentication:** Bearer Token (JWT)

---

## Role Hierarchy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          ROLE ARCHITECTURE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────┐                                                               │
│  │   ROLE   │                                                               │
│  │          │                                                               │
│  │ • Name   │                                                               │
│  │ • Desc   │                                                               │
│  └────┬─────┘                                                               │
│       │                                                                     │
│       │ has many                                                            │
│       ▼                                                                     │
│  ┌──────────────┐                                                           │
│  │ PERMISSIONS  │                                                           │
│  │              │                                                           │
│  │ • customer.* │                                                           │
│  │ • lead.*     │                                                           │
│  │ • report.view│                                                           │
│  └──────────────┘                                                           │
│                                                                             │
│  Example: Sales Manager Role                                                │
│  ─────────────────────────────────────────────────────────────────────     │
│  ┌────────────────────────────────────────┐                                │
│  │ Role: Sales Manager                    │                                │
│  │ Data Scope: AllInOrganization          │                                │
│  │                                        │                                │
│  │ Permissions (45 total):                │                                │
│  │   Customer Module:                     │                                │
│  │     ✓ customer.view                    │                                │
│  │     ✓ customer.create                  │                                │
│  │     ✓ customer.update                  │                                │
│  │     ✗ customer.delete                  │                                │
│  │                                        │                                │
│  │   Lead Module:                         │                                │
│  │     ✓ lead.view                        │                                │
│  │     ✓ lead.create                      │                                │
│  │     ✓ lead.update                      │                                │
│  │     ✓ lead.assign                      │                                │
│  │     ✗ lead.delete                      │                                │
│  │                                        │                                │
│  │   Opportunity Module:                  │                                │
│  │     ✓ opportunity.view                 │                                │
│  │     ✓ opportunity.create               │                                │
│  │     ✓ opportunity.update               │                                │
│  │     ✓ opportunity.approve              │                                │
│  │                                        │                                │
│  │   Report Module:                       │                                │
│  │     ✓ report.view                      │                                │
│  │     ✓ report.export                    │                                │
│  │                                        │                                │
│  │   User Module:                         │                                │
│  │     ✗ user.create  (Admin only)        │                                │
│  │     ✗ user.delete  (Admin only)        │                                │
│  └────────────────────────────────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Scope

### Các mức độ Data Scope

| Data Scope | Ai có thể xem/edit | Use Case |
|------------|-------------------|----------|
| **AllInOrganization** | Tất cả data trong tổ chức | Admin, Manager cần xem toàn bộ |
| **Department** | Data của department mình | Department head, team lead |
| **TeamOnly** | Data của team mình | Team members |
| **OnlyOwn** | Chỉ data của chính mình | Individual contributors |

### Ví dụ Data Scope

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DATA SCOPE EXAMPLE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Organization: Acme Corp                                                    │
│  ─────────────────────────────────────────────────────────────────────     │
│  Departments:                                                               │
│    • Sales (50 people)                                                      │
│    • Marketing (30 people)                                                  │
│    • Support (20 people)                                                    │
│                                                                             │
│  Teams within Sales:                                                        │
│    • Enterprise Team (10 people)                                            │
│    • SMB Team (15 people)                                                   │
│    • Mid-Market Team (25 people)                                            │
│                                                                             │
│  ───────────────────────────────────────────────────────────────────────    │
│                                                                             │
│  User: John (Admin)                                                         │
│  Data Scope: AllInOrganization                                              │
│  → Can view/edit ALL opportunities (Sales + Marketing + Support)            │
│                                                                             │
│  User: Sarah (Sales Manager)                                                │
│  Data Scope: Department                                                     │
│  → Can view/edit Sales department opps only (all 50 people)                 │
│  → Cannot see Marketing/Support opps                                        │
│                                                                             │
│  User: Bob (Enterprise Team Lead)                                           │
│  Data Scope: TeamOnly                                                       │
│  → Can view/edit Enterprise Team opps only (10 people)                      │
│  → Cannot see SMB/Mid-Market team opps                                      │
│                                                                             │
│  User: Alice (Sales Rep)                                                    │
│  Data Scope: OnlyOwn                                                        │
│  → Can view/edit only HER OWN opportunities                                 │
│  → Cannot see other reps' opps                                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Permission Modules

### Available Permission Modules

| Module | Example Permissions | Description |
|--------|---------------------|-------------|
| **Customer** | customer.view, customer.create, customer.update, customer.delete | Manage customers |
| **Lead** | lead.view, lead.create, lead.update, lead.convert, lead.assign | Manage leads |
| **Opportunity** | opportunity.view, opportunity.create, opportunity.update, opportunity.approve | Manage opportunities |
| **Contact** | contact.view, contact.create, contact.update | Manage contacts |
| **Ticket** | ticket.view, ticket.create, ticket.assign, ticket.resolve | Support tickets |
| **Activity** | activity.view, activity.create, activity.complete | Tasks & activities |
| **Campaign** | campaign.view, campaign.create, campaign.execute | Marketing campaigns |
| **Report** | report.view, report.export | Business reports |
| **User** | user.view, user.create, user.update, user.delete | User management |
| **Role** | role.view, role.create, role.update, role.delete | Role management |
| **Workflow** | workflow.view, workflow.create, workflow.execute | Automation workflows |

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Get All Roles (Danh sách roles)

**Nghiệp vụ thực tế:**
- Admin xem tất cả roles trong system
- See permission count

**Ví dụ thực tế:**
> Admin vào Settings → Roles:
> - Admin (System Role) - 150 permissions
> - Sales Manager - 45 permissions
> - Sales Rep - 25 permissions
> - Support Agent - 30 permissions
> - Marketing - 35 permissions
> → 5 roles total

---

### 2. Get Role by ID (Chi tiết role)

**Nghiệp vụ thực tế:**
- View full role details
- See all assigned permissions

**Ví dụ thực tế:**
> Click "Sales Manager" role:
> - Name: Sales Manager
> - Description: Manages sales team and approves deals
> - Data Scope: Department
> - Permissions: 45 total
>   * Customer: view, create, update
>   * Lead: view, create, update, assign
>   * Opportunity: view, create, update, approve
>   * Report: view, export
> - Users: 8 people assigned this role

---

### 3. Get All Permissions (Danh sách permissions)

**Nghiệp vụ thực tế:**
- View all available permissions in system
- Grouped by module

**Ví dụ thực tế:**
> Admin creating new role, selects permissions:
> - Customer Module:
>   * customer.view
>   * customer.create
>   * customer.update
>   * customer.delete
> - Lead Module:
>   * lead.view
>   * lead.create
>   * ...
> → 150 permissions total across 15 modules

---

### 4. Create Role (Tạo role mới)

**Nghiệp vụ thực tế:**
- Create custom role for specific use case
- Select permissions

**Ví dụ thực tế:**
> Company needs "Sales Intern" role:
> - Name: Sales Intern
> - Description: Junior sales role with limited access
> - Data Scope: OnlyOwn (see only own data)
> - Permissions:
>   * lead.view ✓
>   * lead.create ✓
>   * opportunity.view ✓
>   * opportunity.create ✗ (not allowed)
>   * customer.view ✓
>   * customer.create ✗ (not allowed)
> → Role created, assign to interns

---

### 5. Update Role (Cập nhật role)

**Nghiệp vụ thực tế:**
- Modify role permissions
- Change data scope

**Ví dụ thực tế:**
> Sales team needs opportunity approval permission:
> - Edit "Sales Rep" role
> - Add permission: opportunity.approve
> - Data Scope: TeamOnly → Department (expanded)
> → All 15 sales reps now have approval permission

---

### 6. Update Role Permissions (Cập nhật permissions)

**Nghiệp vụ thực tế:**
- Bulk update permissions for a role
- Add/remove multiple permissions at once

**Ví dụ thực tế:**
> New module "Contracts" added, update roles:
> - Sales Manager role:
>   * Add: contract.view, contract.create, contract.approve
> - Sales Rep role:
>   * Add: contract.view, contract.create
> → All users with these roles get new permissions

---

### 7. Delete Role (Xóa role)

**Nghiệp vụ thực tế:**
- Remove unused role
- Must not have any users assigned

**Ví dụ thực tế:**
> "Temp Contractor" role no longer needed:
> - Check: 0 users assigned ✓
> - Delete role
> → Role removed from system

---

## System Roles vs Custom Roles

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SYSTEM ROLES vs CUSTOM ROLES                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  System Roles (Built-in):                                                   │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Pre-configured by system                                                 │
│  • Cannot be deleted                                                        │
│  • Can be modified (with caution)                                           │
│  • Examples:                                                                │
│    - Admin: Full system access                                              │
│    - User: Basic access                                                     │
│                                                                             │
│  Custom Roles (User-created):                                               │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Created by admins for specific needs                                     │
│  • Can be deleted (if no users assigned)                                    │
│  • Fully customizable                                                       │
│  • Examples:                                                                │
│    - Sales Manager                                                          │
│    - Support Agent                                                          │
│    - Marketing Coordinator                                                  │
│    - Sales Intern                                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Best Practices

### 1. Principle of Least Privilege

Give users minimum necessary permissions, not full admin access.

**Example:**
- Sales Rep: View customers, Create leads, Update own opportunities
- NOT: Delete anything, Manage users, System settings

### 2. Role Naming Convention

Use clear, descriptive role names:
- ✅ Sales Manager, Support Agent - L1, Marketing Coordinator
- ❌ Role123, Test, John's Role

### 3. Regular Permission Audits

Quarterly review:
- Check for unused roles (0 users assigned)
- Check for risky permission combinations
- Remove obsolete roles

### 4. Use Data Scope Appropriately

- **Sales Rep:** OnlyOwn → Sees only own opportunities
- **Team Lead:** TeamOnly → Sees team's opportunities
- **Sales Manager:** Department → Sees all sales
- **CEO:** AllInOrganization → Sees everything

---

## Endpoints

### 1. Get All Roles

Lấy danh sách tất cả roles.

```
GET /api/v1/roles
```

**Permission Required:** `role.view`

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "role-guid",
      "name": "Sales Manager",
      "description": "Manages sales team and approves deals",
      "isSystemRole": false,
      "dataScope": "Department",
      "permissionCount": 45,
      "createdAt": "2024-01-01T00:00:00Z"
    },
    {
      "id": "admin-role-guid",
      "name": "Admin",
      "description": "Full system access",
      "isSystemRole": true,
      "dataScope": "AllInOrganization",
      "permissionCount": 150,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### 2. Get Role by ID

Lấy chi tiết một role với danh sách đầy đủ permissions.

**Endpoint:** `GET /api/v1/roles/{id}`

**Permission:** `role.view`

**Response Fields:**
- Kế thừa tất cả fields từ Get All Roles
- `permissions`: Array các permission objects
  - `id`: Permission ID
  - `name`: Tên permission
  - `code`: Mã permission (vd: `customer.view`)
  - `module`: Module (Customer, Lead, Opportunity, etc.)
  - `description`: Mô tả chức năng   "module": "Customer",
      "permissions": [
        {
          "id": "perm-guid-1",
          "name": "View Customers",
          "code": "customer.view",
          "description": "Can view customer records",
          "module": "Customer"
        },
        {
          "id": "perm-guid-2",
          "name": "Create Customers",
          "code": "customer.create",
          "description": "Can create new customers",
          "module": "Customer"
        },
        {
          "id": "perm-guid-3",
          "name": "Update Customers",
          "code": "customer.update",
          "description": "Can update customer records",
          "module": "Customer"
        }
      ]
    },
    {
      "module": "Lead",
      "permissions": [
        {
          "id": "perm-guid-10",
          "name": "View Leads",
          "code": "lead.view",
          "description": "Can view leads",
          "module": "Lead"
        }
      ]
    }
  ]
}
```

### 4. Create Role

Tạo role mới.

**Endpoint:** `POST /api/v1/roles`

**Permission:** `role.create`

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Tên role (unique) |
| `description` | string | No | Mô tả role |
| `dataScope` | int | **Yes** | Phạm vi dữ liệu (0-3) |
| `permissionIds` | array | No | Danh sách Permission IDs |

**DataScope Values:**
- 0 = AllInOrganization
- 1 = Department  
- 2 = TeamOnly
- 3 = OnlyOwn

**Response:** 201 Created với role object mới tạo

### 5. Update Role

Cập nhật thông tin role (name, description, dataScope).

**Endpoint:** `PUT /api/v1/roles/{id}`

**Permission:** `role.update`

**Request Body:** (Tất cả fields đều optional)

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Tên role mới |
| `description` | string | Mô tả mới |
| `dataScope` | int | Data scope mới (0-3) |

**Lưu ý:** Không thể update system roles

### 6. Update Role Permissions

Cập nhật danh sách permissions của role (thay thế toàn bộ).

**Endpoint:** `PUT /api/v1/roles/{id}/permissions`

**Permission:** `role.update`

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `permissionIds` | array | **Yes** | Danh sách đầy đủ Permission IDs mới |

**Lưu ý:** API này THAY THẾ toàn bộ permissions. Gửi danh sách đầy đủ muốn giữ lại.

### 7. Delete Role

Xóa role (soft delete).

**Endpoint:** `DELETE /api/v1/roles/{id}`

**Permission:** `role.delete`

**Điều kiện:**
- Không thể xóa system roles
- Không thể xóa roles đang được assign cho users

**Response:** Success message khi xóa thành công

---

## Client Integration Examples

### JavaScript/TypeScript

```javascript
// 1. Get all roles
const roles = await fetch('/api/v1/roles', {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());

// 2. Get all permissions (for role creation form)
const permissions = await fetch('/api/v1/roles/permissions', {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(r => r.json());

// 3. Create new role
const newRole = await fetch('/api/v1/roles', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    name: 'Sales Intern',
    description: 'Junior sales role',
    dataScope: 3, // OnlyOwn
    permissionIds: ['perm-id-1', 'perm-id-2']
  })
}).then(r => r.json());

// 4. Update role permissions
await fetch(`/api/v1/roles/${roleId}/permissions`, {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    permissionIds: ['perm-1', 'perm-2', 'perm-3']
  })
});

// 5. Delete role
await fetch(`/api/v1/roles/${roleId}`, {
  method: 'DELETE',
  headers: { 'Authorization': `Bearer ${token}` }
});
```

### React Component Example

```jsx
function RoleManagement() {
  const [roles, setRoles] = useState([]);
  const [permissions, setPermissions] = useState([]);
  
  useEffect(() => {
    loadRoles();
    loadPermissions();
  }, []);
  
  const loadRoles = async () => {
    const response = await fetch('/api/v1/roles', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setRoles(data.data);
  };
  
  const loadPermissions = async () => {
    const response = await fetch('/api/v1/roles/permissions', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setPermissions(data.data);
  };
  
  const createRole = async (roleData) => {
    await fetch('/api/v1/roles', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(roleData)
    });
    loadRoles(); // Reload list
  };
  
  return (
    <div>
      <h1>Role Management</h1>
      {roles.map(role => (
        <RoleCard key={role.id} role={role} />
      ))}
    </div>
  );
}
```

---

## Common Use Cases

### 1. Role Management Dashboard

**Nghiệp vụ:** Admin xem và quản lý tất cả roles

**Flow:**
1. GET `/api/v1/roles` → Hiển thị danh sách roles
2. Click vào role → GET `/api/v1/roles/{id}` → Xem chi tiết
3. Edit role → PUT `/api/v1/roles/{id}` → Cập nhật
4. Delete unused role → DELETE `/api/v1/roles/{id}`

### 2. Create Custom Role

**Nghiệp vụ:** Tạo role mới cho use case đặc biệt

**Flow:**
1. GET `/api/v1/roles/permissions` → Load all available permissions
2. Admin chọn permissions cần thiết
3. POST `/api/v1/roles` với name, description, dataScope, permissionIds
4. Role mới được tạo → Có thể assign cho users

### 3. Permission Audit

**Nghiệp vụ:** Review permissions của từng role

**Flow:**
1. GET `/api/v1/roles` → List tất cả roles
2. Loop qua từng role: GET `/api/v1/roles/{id}`
3. Kiểm tra permissions:
   - Có permissions nguy hiểm? (delete, manage users)
   - Có permissions không cần thiết?
4. Update permissions nếu cần: PUT `/api/v1/roles/{id}/permissions`

### 4. Data Scope Management

**Nghiệp vụ:** Điều chỉnh phạm vi dữ liệu user được xem

**Scenarios:**
- Sales Rep promoted to Team Lead:
  - Update role: dataScope = 3 (OnlyOwn) → 2 (TeamOnly)
  
- Department Head needs wider access:
  - Update role: dataScope = 2 (TeamOnly) → 1 (Department)

---

## Permissions Required

| Permission Code | Description |
|-----------------|-------------|
| `role.view` | View roles & permissions |
| `role.create` | Create new roles |
| `role.update` | Update roles & permissions |
| `role.delete` | Delete roles |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
