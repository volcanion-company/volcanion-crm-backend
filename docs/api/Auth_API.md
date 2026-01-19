# Authentication API Documentation

## Tổng quan Module

### Authentication là gì?

**Authentication (Xác thực)** là quá trình xác minh identity của user thông qua credentials (email/password), và cấp access tokens để truy cập vào hệ thống CRM.

### JWT Authentication Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        JWT AUTHENTICATION FLOW                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. LOGIN                                                                   │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ POST /api/v1/auth/login                                  │           │
│     │ {                                                         │           │
│     │   "email": "user@company.com",                           │           │
│     │   "password": "SecurePass123!"                           │           │
│     │ }                                                         │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│     Server validates credentials                                            │
│                        │                                                    │
│                        ▼                                                    │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ Response: 200 OK                                          │           │
│     │ {                                                         │           │
│     │   "accessToken": "eyJhbGc...",    (expires in 1 hour)    │           │
│     │   "refreshToken": "dGhpc2lz...",  (expires in 7 days)    │           │
│     │   "expiresIn": 3600,                                      │           │
│     │   "user": {                                               │           │
│     │     "id": "user-guid",                                    │           │
│     │     "email": "user@company.com",                          │           │
│     │     "name": "John Smith",                                 │           │
│     │     "roles": ["Sales"]                                    │           │
│     │   }                                                        │           │
│     │ }                                                         │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
│  2. ACCESS PROTECTED RESOURCES                                              │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ GET /api/v1/customers                                     │           │
│     │ Headers:                                                  │           │
│     │   Authorization: Bearer eyJhbGc...                        │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│     Server validates JWT                                                    │
│       • Signature valid? ✓                                                  │
│       • Not expired? ✓                                                      │
│       • User still active? ✓                                                │
│                        │                                                    │
│                        ▼                                                    │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ Response: 200 OK                                          │           │
│     │ { customers data }                                        │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
│  3. TOKEN EXPIRES (After 1 hour)                                            │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ GET /api/v1/customers                                     │           │
│     │ Authorization: Bearer eyJhbGc... (expired)                │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ Response: 401 Unauthorized                                │           │
│     │ { "error": "Token expired" }                              │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
│  4. REFRESH TOKEN                                                           │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ POST /api/v1/auth/refresh                                 │           │
│     │ {                                                         │           │
│     │   "refreshToken": "dGhpc2lz..."                          │           │
│     │ }                                                         │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│     Server validates refresh token                                          │
│       • Token valid? ✓                                                      │
│       • Not revoked? ✓                                                      │
│       • User active? ✓                                                      │
│                        │                                                    │
│                        ▼                                                    │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ Response: 200 OK                                          │           │
│     │ {                                                         │           │
│     │   "accessToken": "eyJNEW...",     (new token)            │           │
│     │   "refreshToken": "NEW123...",    (rotated)              │           │
│     │   "expiresIn": 3600                                       │           │
│     │ }                                                         │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
│  5. LOGOUT                                                                  │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ POST /api/v1/auth/logout                                  │           │
│     │ { "refreshToken": "NEW123..." }                          │           │
│     └──────────────────┬───────────────────────────────────────┘           │
│                        │                                                    │
│                        ▼                                                    │
│     Refresh token revoked (blacklisted)                                     │
│                        │                                                    │
│                        ▼                                                    │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ Response: 200 OK                                          │           │
│     │ { "message": "Logged out successfully" }                  │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Access Token vs Refresh Token

| Aspect | Access Token | Refresh Token |
|--------|--------------|---------------|
| **Purpose** | Access protected resources | Get new access token |
| **Lifespan** | Short (1 hour) | Long (7 days) |
| **Storage** | Memory (never localStorage!) | HttpOnly cookie or secure storage |
| **Revocable** | No (JWT stateless) | Yes (stored in DB) |
| **Sent with** | Every API request | Only to /refresh endpoint |
| **Contains** | User ID, roles, permissions | User ID, device ID |

---

## Security Best Practices

### 1. Password Security

```csharp
// ✅ GOOD: Hash passwords with bcrypt/argon2
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, 12); // Cost factor 12
}

// Verify password
public bool VerifyPassword(string password, string hash)
{
    return BCrypt.Net.BCrypt.Verify(password, hash);
}

// ❌ NEVER store plain text passwords!
// ❌ NEVER use MD5/SHA1 for passwords!
```

### 2. Token Security

```typescript
// ✅ GOOD: Store tokens securely
// Access token in memory (Redux, React Context)
const [accessToken, setAccessToken] = useState<string>("");

// Refresh token in HttpOnly cookie (set by server)
// Or secure storage (Mobile: Keychain/Keystore)

// ❌ NEVER store tokens in localStorage!
// localStorage.setItem('token', accessToken); // XSS vulnerable!
```

### 3. Refresh Token Rotation

```csharp
// Every refresh generates NEW tokens
public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
{
    // 1. Validate old refresh token
    var oldToken = await FindRefreshTokenAsync(refreshToken);
    if (oldToken == null || oldToken.IsRevoked) 
        throw new UnauthorizedException();
    
    // 2. Revoke old token
    oldToken.IsRevoked = true;
    
    // 3. Generate NEW tokens
    var newAccessToken = GenerateAccessToken(user);
    var newRefreshToken = GenerateRefreshToken(user);
    
    // 4. Save new refresh token
    await SaveRefreshTokenAsync(newRefreshToken);
    
    return new AuthResult { 
        AccessToken = newAccessToken, 
        RefreshToken = newRefreshToken 
    };
}
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Login (Đăng nhập)

**Nghiệp vụ thực tế:**
- User nhập email/password
- System verify credentials
- Cấp tokens để truy cập hệ thống

**Ví dụ thực tế:**
> User John login:
> - Email: john@company.com
> - Password: SecurePass123!
> - System checks:
>   * Email exists? ✓
>   * Password correct? ✓
>   * Account active? ✓
> - Generate tokens:
>   * Access token: Valid 1 hour
>   * Refresh token: Valid 7 days
> - Record login: IP, device, timestamp
> → User logged in successfully

---

### 2. Refresh Token (Làm mới token)

**Nghiệp vụ thực tế:**
- Access token expired sau 1 giờ
- Dùng refresh token để lấy access token mới
- Không cần re-enter password

**Ví dụ thực tế:**
> User đang làm việc, access token expires:
> - App detects 401 Unauthorized
> - Automatically call /auth/refresh
> - Send refresh token (still valid for 7 days)
> - Receive new access token
> → User continues working seamlessly (no login prompt)

---

### 3. Logout (Đăng xuất)

**Nghiệp vụ thực tế:**
- User muốn đăng xuất
- Revoke refresh token
- Client xóa access token

**Ví dụ thực tế:**
> User click "Logout":
> - POST /auth/logout với refresh token
> - Server revoke token (blacklist)
> - Client clears access token from memory
> - Client clears refresh token from cookies
> → User logged out, tokens invalid

---

### 4. Logout All (Đăng xuất tất cả thiết bị)

**Nghiệp vụ thực tế:**
- User muốn revoke tất cả sessions
- Useful khi:
>   * Mất điện thoại
>   * Suspect account compromise
>   * Left logged in on public computer

**Ví dụ thực tế:**
> User John left laptop at airport:
> - Login from phone
> - Click "Logout All Devices"
> - System revokes ALL refresh tokens
> - All devices (laptop, tablet, phone) logged out
> → Laptop session invalid, data secure

---

### 5. Get Profile (Xem thông tin user)

**Nghiệp vụ thực tế:**
- Get current logged-in user info
- Display in UI (profile page, navbar)

**Ví dụ thực tế:**
> App loads, fetch user profile:
> - GET /auth/me
> - Returns:
>   * Name: John Smith
>   * Email: john@company.com
>   * Roles: [Sales, Manager]
>   * Permissions: [customer.view, lead.create, ...]
> - UI displays: "Welcome, John Smith"
> - UI shows/hides features based on permissions

---

## Common Scenarios

### Scenario 1: First Login

```
User opens app
  ↓
Redirected to login page
  ↓
Enter credentials
  ↓
POST /auth/login
  ↓
Receive tokens
  ↓
Store access token in memory
  ↓
Store refresh token in cookie
  ↓
Fetch user profile (GET /auth/me)
  ↓
Redirect to dashboard
```

### Scenario 2: Access Token Expired

```
User makes API request
  ↓
Access token expired (after 1 hour)
  ↓
API returns 401 Unauthorized
  ↓
App intercepts 401
  ↓
Automatically call POST /auth/refresh
  ↓
Receive new access token
  ↓
Retry original request with new token
  ↓
Success! User doesn't notice anything
```

### Scenario 3: Refresh Token Expired

```
User inactive for 7 days
  ↓
Access token expired
  ↓
Try POST /auth/refresh
  ↓
Refresh token also expired
  ↓
401 Unauthorized
  ↓
Clear all tokens
  ↓
Redirect to login page
  ↓
User must login again
```

### Scenario 4: Security Breach (Logout All)

```
User suspects account compromise
  ↓
POST /auth/logout-all
  ↓
Server revokes ALL refresh tokens
  ↓
All devices logged out
  ↓
Change password
  ↓
Login again from trusted device
```

---

## Technical Overview

**Base URL:** `/api/v1/auth`

**Authentication:** Some endpoints allow anonymous (login, refresh), others require Bearer token

---

## Endpoints

### 1. Login

Authenticate user và nhận tokens.

```
POST /api/v1/auth/login
```

**Authentication:** Anonymous

#### Request Body

```json
{
  "email": "user@company.com",
  "password": "SecurePass123!",
  "tenantId": "tenant-guid"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | **Yes** | User email |
| `password` | string | **Yes** | User password |
| `tenantId` | Guid | No | Tenant ID (for multi-tenant) |

#### Response (200 OK)

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpc2lzYXJlZnJlc2h0b2tlbg==",
    "expiresIn": 3600,
    "user": {
      "id": "user-guid",
      "email": "user@company.com",
      "firstName": "John",
      "lastName": "Smith",
      "fullName": "John Smith",
      "roles": ["Sales", "Manager"],
      "tenantId": "tenant-guid"
    }
  }
}
```

#### Error Responses

```json
// 401 Unauthorized - Invalid credentials
{
  "success": false,
  "message": "Invalid email or password"
}

// 403 Forbidden - Account inactive
{
  "success": false,
  "message": "Account is inactive. Contact administrator."
}
```

---

### 2. Refresh Token

Làm mới access token sử dụng refresh token.

```
POST /api/v1/auth/refresh
```

**Authentication:** Anonymous (sends refresh token)

#### Request Body

```json
{
  "refreshToken": "dGhpc2lzYXJlZnJlc2h0b2tlbg=="
}
```

#### Response (200 OK)

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJNEW_TOKEN...",
    "refreshToken": "NEW_REFRESH_TOKEN==",
    "expiresIn": 3600,
    "user": {
      "id": "user-guid",
      "email": "user@company.com",
      "fullName": "John Smith"
    }
  }
}
```

#### Error Response

```json
// 401 Unauthorized - Invalid or expired refresh token
{
  "success": false,
  "message": "Invalid or expired refresh token"
}
```

---

### 3. Logout

Revoke refresh token (đăng xuất).

```
POST /api/v1/auth/logout
```

**Authentication:** Bearer Token

#### Request Body

```json
{
  "refreshToken": "dGhpc2lzYXJlZnJlc2h0b2tlbg=="
}
```

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

---

### 4. Logout All Devices

Revoke tất cả refresh tokens của user (đăng xuất khỏi tất cả thiết bị).

```
POST /api/v1/auth/logout-all
```

**Authentication:** Bearer Token

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Logged out from all devices successfully"
}
```

---

### 5. Get Current User Profile

Lấy thông tin user hiện tại.

```
GET /api/v1/auth/me
```

**Authentication:** Bearer Token

#### Response (200 OK)

```json
{
  "success": true,
  "data": {
    "id": "user-guid",
    "email": "user@company.com",
    "name": "John Smith",
    "tenantId": "tenant-guid",
    "roles": ["Sales", "Manager"],
    "permissions": [
      "customer.view",
      "customer.create",
      "lead.view",
      "lead.create",
      "opportunity.view"
    ]
  }
}
```

---

## JWT Token Structure

### Access Token Payload

```json
{
  "sub": "user-guid",
  "email": "user@company.com",
  "name": "John Smith",
  "tenant_id": "tenant-guid",
  "roles": ["Sales", "Manager"],
  "permissions": ["customer.view", "lead.create"],
  "iat": 1737187200,
  "exp": 1737190800
}
```

### Refresh Token (Stored in Database)

```json
{
  "id": "refresh-token-guid",
  "userId": "user-guid",
  "token": "hashed-token-string",
  "expiresAt": "2026-01-25T00:00:00Z",
  "createdAt": "2026-01-18T00:00:00Z",
  "createdByIp": "192.168.1.100",
  "revokedAt": null,
  "revokedByIp": null,
  "isRevoked": false,
  "isExpired": false,
  "isActive": true
}
```

---

## Client Implementation Examples

### React/TypeScript

```typescript
// authService.ts
import axios from 'axios';

interface LoginRequest {
  email: string;
  password: string;
}

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: User;
}

class AuthService {
  private accessToken: string = '';
  
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await axios.post('/api/v1/auth/login', credentials);
    const { accessToken, refreshToken } = response.data.data;
    
    // Store access token in memory
    this.accessToken = accessToken;
    
    // Store refresh token in HttpOnly cookie (handled by server)
    
    return response.data.data;
  }
  
  async refreshToken(): Promise<string> {
    const response = await axios.post('/api/v1/auth/refresh', {
      refreshToken: this.getRefreshToken()
    });
    
    this.accessToken = response.data.data.accessToken;
    return this.accessToken;
  }
  
  async logout(): Promise<void> {
    await axios.post('/api/v1/auth/logout', {
      refreshToken: this.getRefreshToken()
    });
    
    this.accessToken = '';
  }
  
  getAccessToken(): string {
    return this.accessToken;
  }
  
  private getRefreshToken(): string {
    // Get from HttpOnly cookie or secure storage
    return getCookie('refreshToken');
  }
}

export const authService = new AuthService();
```

### Axios Interceptor

```typescript
// axios-interceptor.ts
import axios from 'axios';
import { authService } from './authService';

// Add access token to requests
axios.interceptors.request.use(
  (config) => {
    const token = authService.getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Handle 401 and refresh token
axios.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      
      try {
        // Try to refresh token
        const newToken = await authService.refreshToken();
        
        // Retry original request with new token
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return axios(originalRequest);
      } catch (refreshError) {
        // Refresh failed, redirect to login
        authService.logout();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }
    
    return Promise.reject(error);
  }
);
```

---

## Permissions

No specific permissions - authentication is public functionality, but some endpoints require being authenticated.

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
