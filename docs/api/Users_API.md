# Users API Documentation

## Tổng quan Module

### User Management là gì?

**User Management (Quản lý người dùng)** là module quản lý tài khoản users trong CRM, bao gồm create, update, activate/deactivate, reset password, và assign roles.

### User Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          USER LIFECYCLE                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────┐    ┌────────┐    ┌──────────┐    ┌──────────┐               │
│  │  CREATE  │───►│ ACTIVE │◄──►│ INACTIVE │───►│ DELETED  │               │
│  └──────────┘    └───┬────┘    └──────────┘    └──────────┘               │
│       │              │                                                      │
│       │              │                                                      │
│       ▼              ▼                                                      │
│  ┌─────────────────────────────────────────┐                               │
│  │ • Admin creates user                    │                               │
│  │ • Set email, name, password             │                               │
│  │ • Assign roles                          │                               │
│  │ • Send welcome email                    │                               │
│  └─────────────────────────────────────────┘                               │
│                                                                             │
│  ┌─────────────────────────────────────────┐                               │
│  │ ACTIVE USER                             │                               │
│  │ • Can login                             │                               │
│  │ • Access CRM based on permissions       │                               │
│  │ • Can change own password               │                               │
│  │ • Can update profile                    │                               │
│  └─────────────────────────────────────────┘                               │
│                                                                             │
│  ┌─────────────────────────────────────────┐                               │
│  │ INACTIVE USER (Deactivated)             │                               │
│  │ • Cannot login                          │                               │
│  │ • Existing sessions terminated          │                               │
│  │ • Data preserved                        │                               │
│  │ • Can be reactivated                    │                               │
│  └─────────────────────────────────────────┘                               │
│                                                                             │
│  ┌─────────────────────────────────────────┐                               │
│  │ DELETED USER (Soft delete)              │                               │
│  │ • Cannot login                          │                               │
│  │ • Not visible in UI                     │                               │
│  │ • Data retained for audit trail         │                               │
│  │ • Email freed for new user              │                               │
│  └─────────────────────────────────────────┘                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## User Roles & Permissions

### Role Hierarchy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ROLE HIERARCHY                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌────────────────────────────────────┐                                    │
│  │          ADMIN                     │                                    │
│  │  (Full access to everything)       │                                    │
│  │  • Manage users                    │                                    │
│  │  • Manage roles                    │                                    │
│  │  • System settings                 │                                    │
│  │  • View all data across teams      │                                    │
│  └──────────────┬─────────────────────┘                                    │
│                 │                                                           │
│                 ▼                                                           │
│  ┌────────────────────────────────────┐                                    │
│  │          MANAGER                   │                                    │
│  │  (Team management)                 │                                    │
│  │  • View team data                  │                                    │
│  │  • Approve deals                   │                                    │
│  │  • Assign leads/opps               │                                    │
│  │  • Reports & analytics             │                                    │
│  └──────────────┬─────────────────────┘                                    │
│                 │                                                           │
│                 ▼                                                           │
│  ┌────────────────────────────────────┐                                    │
│  │          SALES REP                 │                                    │
│  │  (Own data + assigned)             │                                    │
│  │  • Manage own leads/opps           │                                    │
│  │  • View assigned customers         │                                    │
│  │  • Create activities               │                                    │
│  │  • Update pipeline                 │                                    │
│  └──────────────┬─────────────────────┘                                    │
│                 │                                                           │
│                 ▼                                                           │
│  ┌────────────────────────────────────┐                                    │
│  │       SUPPORT AGENT                │                                    │
│  │  (Customer support)                │                                    │
│  │  • View customers                  │                                    │
│  │  • Manage tickets                  │                                    │
│  │  • Create activities               │                                    │
│  │  • Read-only on sales data         │                                    │
│  └────────────────────────────────────┘                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Get All Users (Danh sách users)

**Nghiệp vụ thực tế:**
- Admin xem tất cả users trong organization
- Filter by status (active/inactive)
- Search by name/email

**Ví dụ thực tế:**
> HR Manager xem users:
> - Total: 45 users
> - Active: 42
> - Inactive: 3 (left company)
> - Search "john" → 3 results
> - Filter Sales role → 15 users

---

### 2. Get User by ID (Chi tiết user)

**Nghiệp vụ thực tế:**
- View detailed user profile
- See roles, permissions, activity

**Ví dụ thực tế:**
> Admin clicks on "John Smith":
> - Email: john@company.com
> - Status: Active
> - Roles: Sales, Manager
> - Last login: 2 hours ago
> - Created: 2024-06-15
> - Total opportunities: 45
> - Total revenue: $1.2M

---

### 3. Create User (Tạo user mới)

**Nghiệp vụ thực tế:**
- Onboard new employee
- Set initial credentials
- Assign roles

**Ví dụ thực tế:**
> New sales rep hired:
> - Admin creates user:
>   * Email: alice@company.com
>   * Name: Alice Johnson
>   * Password: TempPass123! (temporary)
>   * Roles: [Sales]
> - Send welcome email
> - User receives login credentials
> → Alice logs in, changes password

---

### 4. Update User (Cập nhật thông tin)

**Nghiệp vụ thực tế:**
- Update user profile
- Change roles
- Modify settings

**Ví dụ thực tế:**
> John promoted to Manager:
> - Admin updates user:
>   * Add role: Manager
>   * Update title: "Sales Manager"
> → John now has manager permissions

---

### 5. Activate User (Kích hoạt)

**Nghiệp vụ thực tế:**
- Re-enable inactive user
- Employee returns from leave

**Ví dụ thực tế:**
> Bob returned from 3-month sabbatical:
> - Status: Inactive (deactivated before leave)
> - Admin clicks "Activate"
> - Status: Active
> → Bob can login again

---

### 6. Deactivate User (Vô hiệu hóa)

**Nghiệp vụ thực tế:**
- Temporarily disable access
- Employee on leave
- Security concern

**Ví dụ thực tế:**
> Sarah on maternity leave (6 months):
> - Admin clicks "Deactivate"
> - Status: Inactive
> - Existing sessions terminated
> - Data preserved
> → Sarah cannot login, will reactivate later

---

### 7. Reset Password (Reset mật khẩu)

**Nghiệp vụ thực tế:**
- User forgot password
- Security requirement (force reset)
- Admin generates temporary password

**Ví dụ thực tế:**
> User calls helpdesk: "I forgot my password"
> - Admin clicks "Reset Password"
> - System generates: TempPass789!
> - Admin tells user the temp password
> - User logs in, forced to change password
> → User sets new password

---

### 8. Change Password (Đổi mật khẩu)

**Nghiệp vụ thực tế:**
- User changes own password
- Security best practice (every 90 days)

**Ví dụ thực tế:**
> User wants to change password:
> - Go to Profile → Security
> - Current password: OldPass123!
> - New password: NewSecure456!
> - Confirm password: NewSecure456!
> → Password changed successfully

---

### 9. Delete User (Xóa user)

**Nghiệp vụ thực tế:**
- Employee left company
- Soft delete (data preserved)

**Ví dụ thực tế:**
> David left company:
> - Admin clicks "Delete"
> - Confirmation: "Are you sure? This will remove access."
> - Status: Deleted (soft delete)
> - Email freed: david@company.com can be reused
> - Historical data preserved (audit trail)
> → David cannot login, data intact

---

## User Status Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        USER STATUS TRANSITIONS                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  New Employee                                                               │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Create User → Status: Active → Can login immediately                       │
│                                                                             │
│  Employee on Leave                                                          │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Active → Deactivate → Status: Inactive → Cannot login                      │
│  Inactive → Activate → Status: Active → Can login again                     │
│                                                                             │
│  Employee Left Company                                                      │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Active → Delete → Status: Deleted → Cannot login, data preserved           │
│                                                                             │
│  Security Issue                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│  Active → Deactivate → Reset Password → Activate                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Best Practices

### 1. Password Policy

```csharp
// Enforce strong passwords
public bool ValidatePassword(string password)
{
    // Minimum 8 characters
    if (password.Length < 8) return false;
    
    // At least one uppercase
    if (!password.Any(char.IsUpper)) return false;
    
    // At least one lowercase
    if (!password.Any(char.IsLower)) return false;
    
    // At least one digit
    if (!password.Any(char.IsDigit)) return false;
    
    // At least one special character
    if (!password.Any(c => "!@#$%^&*".Contains(c))) return false;
    
    return true;
}
```

### 2. Audit User Actions

```csharp
// Log all user management actions
await auditService.LogAsync(
    action: "User.Created",
    entityType: "User",
    entityId: newUser.Id,
    details: $"User {newUser.Email} created by {currentUser.Email}",
    ipAddress: GetClientIp()
);
```

### 3. Prevent Self-Modification

```csharp
// Users cannot deactivate/delete themselves
if (userId == currentUser.Id)
{
    throw new BusinessException("Cannot deactivate yourself");
}
```

---

## Technical Overview

**Base URL:** `/api/v1/users`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get All Users

Lấy danh sách users với pagination.

```
GET /api/v1/users?pageNumber=1&pageSize=10&status=Active&search=john
```

**Permission Required:** `user.view`

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Số trang |
| `pageSize` | int | No | 10 | Items per page |
| `status` | UserStatus | No | - | Filter by status |
| `search` | string | No | - | Search by name/email |
| `sortBy` | string | No | "CreatedAt" | Sort field |
| `sortDescending` | bool | No | false | Sort order |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "user-guid",
        "email": "john@company.com",
        "firstName": "John",
        "lastName": "Smith",
        "fullName": "John Smith",
        "status": "Active",
        "roles": ["Sales", "Manager"],
        "lastLoginAt": "2026-01-18T08:00:00Z",
        "createdAt": "2024-06-15T00:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 45
  }
}
```

---

### 2. Get User by ID

Lấy chi tiết một user.

```
GET /api/v1/users/{id}
```

**Permission Required:** `user.view`

#### Response

```json
{
  "success": true,
  "data": {
    "id": "user-guid",
    "email": "john@company.com",
    "firstName": "John",
    "lastName": "Smith",
    "fullName": "John Smith",
    "phone": "+1-555-1234",
    "status": "Active",
    "roles": ["Sales", "Manager"],
    "timeZone": "America/New_York",
    "culture": "en-US",
    "lastLoginAt": "2026-01-18T08:00:00Z",
    "createdAt": "2024-06-15T00:00:00Z",
    "updatedAt": "2026-01-10T00:00:00Z"
  }
}
```

---

### 3. Create User

Tạo user mới.

```
POST /api/v1/users
```

**Permission Required:** `user.create`

#### Request Body

```json
{
  "email": "alice@company.com",
  "password": "TempPass123!",
  "firstName": "Alice",
  "lastName": "Johnson",
  "phone": "+1-555-5678",
  "timeZone": "America/New_York",
  "culture": "en-US",
  "roleIds": ["role-guid-1", "role-guid-2"]
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | **Yes** | User email (unique) |
| `password` | string | **Yes** | Initial password (min 8 chars) |
| `firstName` | string | **Yes** | First name |
| `lastName` | string | **Yes** | Last name |
| `phone` | string | No | Phone number |
| `timeZone` | string | No | Time zone (default: UTC) |
| `culture` | string | No | Culture (default: en-US) |
| `roleIds` | array | No | Role IDs to assign |

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-user-guid",
    "email": "alice@company.com",
    "fullName": "Alice Johnson",
    "status": "Active",
    "createdAt": "2026-01-18T10:00:00Z"
  }
}
```

---

### 4. Update User

Cập nhật user info.

```
PUT /api/v1/users/{id}
```

**Permission Required:** `user.update`

#### Request Body (All fields optional)

```json
{
  "firstName": "Alice",
  "lastName": "Johnson-Smith",
  "phone": "+1-555-9999",
  "timeZone": "America/Los_Angeles",
  "culture": "en-US",
  "roleIds": ["role-guid-1", "role-guid-3"]
}
```

---

### 5. Activate User

Kích hoạt inactive user.

```
POST /api/v1/users/{id}/activate
```

**Permission Required:** `user.update`

#### Response

```json
{
  "success": true,
  "message": "User activated"
}
```

---

### 6. Deactivate User

Vô hiệu hóa user.

```
POST /api/v1/users/{id}/deactivate
```

**Permission Required:** `user.update`

#### Response

```json
{
  "success": true,
  "message": "User deactivated"
}
```

**Note:** Cannot deactivate yourself.

---

### 7. Reset Password (Admin)

Admin reset password cho user.

```
POST /api/v1/users/{id}/reset-password
```

**Permission Required:** `user.update`

#### Response

```json
{
  "success": true,
  "data": "TempPass789!",
  "message": "Password reset successful. New password generated."
}
```

**Security:** Admin receives temporary password to give to user. User must change on next login.

---

### 8. Change Password (Self)

User đổi password của chính mình.

```
POST /api/v1/users/change-password
```

**Permission Required:** Authenticated user

#### Request Body

```json
{
  "currentPassword": "OldPass123!",
  "newPassword": "NewSecure456!",
  "confirmPassword": "NewSecure456!"
}
```

#### Response

```json
{
  "success": true,
  "message": "Password changed successfully"
}
```

---

### 9. Delete User

Xóa user (soft delete).

```
DELETE /api/v1/users/{id}
```

**Permission Required:** `user.delete`

#### Response

```json
{
  "success": true,
  "message": "User deleted successfully"
}
```

**Note:** Cannot delete yourself.

---

## Enums

### UserStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | User can login |
| 1 | Inactive | User deactivated |
| 2 | Deleted | User soft deleted |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| `user.view` | View users |
| `user.create` | Create new users |
| `user.update` | Update users, reset passwords |
| `user.delete` | Delete users |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
