# CalendarSync API Documentation

## Tổng quan Module

### Calendar Sync là gì?

**Calendar Sync (Đồng bộ lịch)** là tính năng tích hợp CRM với các hệ thống calendar phổ biến (Google Calendar, Microsoft Outlook, Microsoft 365) để đồng bộ 2-chiều giữa CRM activities và external calendar events.

### Tại sao cần Calendar Sync?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              VẤN ĐỀ KHI KHÔNG CÓ CALENDAR SYNC                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Sales Rep:                                                                 │
│  ┌─────────────────┐         ┌─────────────────┐                           │
│  │   CRM System    │         │ Google Calendar │                           │
│  │                 │         │                 │                           │
│  │ Meeting: 2pm    │    ✗    │ Meeting: 2pm    │                           │
│  │ Task: Call lead │         │ Appointment     │                           │
│  └─────────────────┘         └─────────────────┘                           │
│                                                                             │
│  ❌ Phải nhập 2 lần (CRM + Calendar)                                        │
│  ❌ Dễ quên update một trong hai                                            │
│  ❌ Không đồng bộ → conflicts                                               │
│  ❌ Không nhận reminders từ personal calendar                               │
│                                                                             │
│                                                                             │
│              GIẢI PHÁP VỚI CALENDAR SYNC                                    │
│  ───────────────────────────────────────────────────────────────────────    │
│                                                                             │
│  Sales Rep:                                                                 │
│  ┌─────────────────┐         ┌─────────────────┐                           │
│  │   CRM System    │◄───────►│ Google Calendar │                           │
│  │                 │ Sync    │                 │                           │
│  │ Meeting: 2pm    │◄───────►│ Meeting: 2pm    │                           │
│  │ Task: Call lead │         │ Task: Call lead │                           │
│  └─────────────────┘         └─────────────────┘                           │
│                                                                             │
│  ✅ Nhập 1 lần, tự động sync                                                │
│  ✅ Luôn đồng bộ real-time                                                  │
│  ✅ Nhận reminders từ calendar app                                          │
│  ✅ Quản lý tất cả events ở một nơi                                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Sync Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      CALENDAR SYNC FLOW                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. OAUTH AUTHORIZATION                                                     │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ User clicks "Connect Google Calendar"                    │           │
│     │   ↓                                                       │           │
│     │ GET /calendar/sync/authorize/GoogleCalendar              │           │
│     │   ↓                                                       │           │
│     │ Redirect to Google OAuth consent screen                  │           │
│     │   ↓                                                       │           │
│     │ User grants permissions                                  │           │
│     │   ↓                                                       │           │
│     │ Google redirects back with auth code                     │           │
│     │   ↓                                                       │           │
│     │ POST /calendar/sync/token-exchange/GoogleCalendar        │           │
│     │   ↓                                                       │           │
│     │ CRM exchanges code for access token & refresh token      │           │
│     │   ↓                                                       │           │
│     │ Save CalendarSyncConfiguration (Active)                  │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
│  2. INITIAL SYNC                                                            │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ POST /calendar/sync/configurations/{id}/sync             │           │
│     │   ↓                                                       │           │
│     │ Fetch Google Calendar events (past 30 days + future 90)  │           │
│     │   → 45 events found                                      │           │
│     │   ↓                                                       │           │
│     │ For each event:                                          │           │
│     │   • Create CRM Activity if not exists                    │           │
│     │   • Create CalendarEventMapping                          │           │
│     │   ↓                                                       │           │
│     │ Fetch CRM Activities (past 30 days + future 90)          │           │
│     │   → 32 activities found                                  │           │
│     │   ↓                                                       │           │
│     │ For each activity:                                       │           │
│     │   • Create Google Calendar event if not exists           │           │
│     │   • Create CalendarEventMapping                          │           │
│     │   ↓                                                       │           │
│     │ Result: 45 imported, 32 exported, 77 total synced        │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
│  3. ONGOING SYNC (Scheduled every 15 minutes)                               │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ Background job runs:                                     │           │
│     │   POST /calendar/sync/sync-all (Admin)                   │           │
│     │   ↓                                                       │           │
│     │ For each active configuration:                           │           │
│     │   • Fetch changes since LastSyncAt                       │           │
│     │   • Compare SyncHash to detect changes                   │           │
│     │   • Update/Create activities/events                      │           │
│     │   • Resolve conflicts (latest wins)                      │           │
│     │   • Update LastSyncAt                                    │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
│  4. REAL-TIME PUSH (From CRM → External)                                    │
│     ┌──────────────────────────────────────────────────────────┐           │
│     │ User creates/updates CRM Activity                        │           │
│     │   ↓                                                       │           │
│     │ CRM checks: Does user have active sync?                  │           │
│     │   Yes → Push to Google Calendar immediately              │           │
│     │   ↓                                                       │           │
│     │ Create/Update event in Google Calendar                   │           │
│     │   ↓                                                       │           │
│     │ Update CalendarEventMapping                              │           │
│     └──────────────────────────────────────────────────────────┘           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Supported Providers

| Provider | OAuth Support | Features | Limitations |
|----------|---------------|----------|-------------|
| **Google Calendar** | ✅ OAuth 2.0 | Full 2-way sync, multiple calendars, reminders | Rate limit: 1M requests/day |
| **Microsoft Outlook** | ✅ OAuth 2.0 | Full 2-way sync, categories, recurrence | - |
| **Microsoft 365** | ✅ OAuth 2.0 | Same as Outlook + Teams integration | Requires 365 subscription |
| **Apple iCloud** | ⚠️ CalDAV | Basic sync via iCal export/import | No OAuth, limited |

---

## Sync Directions

### 1. CRM → External (Push)

```
CRM Activity created/updated
  ↓
Check: User has active sync config?
  ↓ Yes
Push to external calendar
  ↓
Create/Update external event
  ↓
Save CalendarEventMapping
```

**Use case:** Sales rep tạo meeting trong CRM, tự động xuất hiện trong Google Calendar.

### 2. External → CRM (Pull)

```
Scheduled sync job runs (every 15 mins)
  ↓
Fetch external calendar events
  ↓
Compare with existing mappings
  ↓
New events → Create CRM Activities
Updated events → Update CRM Activities
Deleted events → Delete CRM Activities (or mark as cancelled)
  ↓
Update mappings
```

**Use case:** Sales rep book meeting trong Google Calendar, tự động tạo activity trong CRM.

### 3. Bidirectional (2-way)

```
Both CRM → External AND External → CRM active
  ↓
Conflict detection:
  • Compare LastSyncedAt vs modified dates
  • Compare SyncHash (hash of event data)
  ↓
Conflict resolution:
  • Latest change wins (by timestamp)
  • OR manual resolution (future feature)
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Get Configurations (Get User Sync Configurations)

**Nghiệp vụ thực tế:**
- Xem tất cả calendar sync configs của user
- Check sync status, last sync time

**Ví dụ thực tế:**
> User vào Settings → Calendar Sync:
> - Google Calendar (user@gmail.com): Active, Last sync 5 mins ago
> - Outlook Calendar (user@company.com): Paused
> → 2 configs, 1 active

---

### 2. Create Configuration (Connect Calendar)

**Nghiệp vụ thực tế:**
- Connect CRM với external calendar
- Configure sync settings

**Ví dụ thực tế:**
> User click "Connect Google Calendar":
> 1. Authorize with Google (OAuth flow)
> 2. Select sync direction: Bidirectional
> 3. Select calendars: Primary, Work, Sales
> 4. Set sync range: 30 days back, 90 days forward
> → Configuration created, initial sync starts

---

### 3. Update Configuration (Modify Sync Settings)

**Nghiệp vụ thực tế:**
- Change sync direction
- Pause/Resume sync
- Update sync range

**Ví dụ thực tế:**
> User updates config:
> - Change sync direction: CRM → External only (stop pulling from Google)
> - Expand sync range: 60 days back, 180 days forward
> - Add calendar: Personal calendar
> → Config updated, re-sync triggered

---

### 4. Delete Configuration (Disconnect Calendar)

**Nghiệp vụ thực tế:**
- Disconnect external calendar
- Stop all syncing
- Clean up mappings (optional)

**Ví dụ thực tế:**
> User disconnects Google Calendar:
> - Confirmation: "This will stop all syncing. Keep existing CRM activities?"
> - User: Yes, keep activities
> → Config deleted, mappings remain (for history)

---

### 5. Get Authorization URL (OAuth Flow Step 1)

**Nghiệp vụ thực tế:**
- Generate OAuth URL
- Redirect user to provider consent screen

**Ví dụ thực tế:**
> Click "Connect Google Calendar":
> - GET /calendar/sync/authorize/GoogleCalendar?redirectUri=...
> - Returns: https://accounts.google.com/o/oauth2/v2/auth?...
> - Redirect user to Google consent screen

---

### 6. Exchange Token (OAuth Flow Step 2)

**Nghiệp vụ thực tế:**
- Exchange auth code for access token
- Save tokens for API calls

**Ví dụ thực tế:**
> User grants permissions, Google redirects back:
> - Code: abc123xyz...
> - POST /calendar/sync/token-exchange/GoogleCalendar
> - CRM exchanges code for:
>   * Access token (expires in 1 hour)
>   * Refresh token (never expires)
> → Config created with tokens

---

### 7. Get External Calendars (List Provider Calendars)

**Nghiệp vụ thực tế:**
- Fetch list of calendars from provider
- User selects which calendars to sync

**Ví dụ thực tế:**
> After connecting Google:
> - GET /calendar/sync/configurations/{id}/calendars
> - Returns:
>   * Primary (user@gmail.com)
>   * Work (work@gmail.com)
>   * Birthdays (auto-generated)
> - User selects: Primary, Work
> → Only selected calendars will sync

---

### 8. Sync Calendar (Manual Sync)

**Nghiệp vụ thực tế:**
- Trigger immediate sync
- Useful for testing or when urgent

**Ví dụ thực tế:**
> User just created important meeting in CRM:
> - Click "Sync Now"
> - POST /calendar/sync/configurations/{id}/sync
> - Result:
>   * Synced: 1 new event
>   * Duration: 2.3 seconds
> → Meeting immediately appears in Google Calendar

---

### 9. Sync All Calendars (Admin Scheduled Job)

**Nghiệp vụ thực tế:**
- Background job syncs all active configs
- Runs every 15 minutes

**Ví dụ thực tế:**
> Scheduled job at 10:00 AM:
> - POST /calendar/sync/sync-all (Admin only)
> - Syncs 45 active configurations
> - Results:
>   * 123 events synced
>   * 12 created
>   * 8 updated
>   * 2 errors (token expired)
> → All users' calendars up-to-date

---

### 10. Get Event Mapping (Check Sync Status)

**Nghiệp vụ thực tế:**
- Check if activity is synced
- View external event ID

**Ví dụ thực tế:**
> User opens CRM activity "Sales Meeting":
> - Activity shows: "Synced with Google Calendar"
> - GET /calendar/sync/mappings/activity/{activityId}
> - Returns:
>   * External Event ID: abc123
>   * Last Synced: 10 mins ago
>   * Sync Status: Synced
>   * Calendar: Primary (user@gmail.com)

---

### 11. Export iCal (Export Activities as .ics)

**Nghiệp vụ thực tế:**
- Export CRM activities to iCal format
- Import into any calendar app (Apple Calendar, Thunderbird, etc.)

**Ví dụ thực tế:**
> User wants to import activities into Apple Calendar:
> - POST /calendar/export/ical
> - Options:
>   * Date range: Next 30 days
>   * Activity types: Meeting, Call
>   * Include: Description, Location
> - Downloads: activities.ics (42 KB)
> - Double-click .ics file → Import to Apple Calendar
> → All activities imported

---

### 12. Get Activity Reminders (View Reminders)

**Nghiệp vụ thực tế:**
- View all reminders for an activity
- Check when notifications will be sent

**Ví dụ thực tế:**
> Activity "Product Demo" tomorrow at 2 PM:
> - GET /calendar/reminders/activity/{activityId}
> - Returns:
>   * Reminder 1: 1 day before (email ✓, notification ✓)
>   * Reminder 2: 1 hour before (notification ✓)
>   * Reminder 3: 15 mins before (notification ✓)
> → 3 reminders scheduled

---

### 13. Create Reminder (Set Custom Reminder)

**Nghiệp vụ thực tế:**
- Add custom reminder to activity
- Choose notification channels (email, in-app, SMS)

**Ví dụ thực tế:**
> User adds reminder to important meeting:
> - POST /calendar/reminders
> - Reminder settings:
>   * Activity: "CEO Meeting"
>   * Time: 30 minutes before
>   * Channels: Email ✓, In-app notification ✓, SMS ✓
>   * Recipient: Current user
> → Reminder created, will fire 30 mins before meeting

---

## Conflict Resolution

### Scenario: User edits same event in both CRM and Google Calendar

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CONFLICT DETECTION                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Timeline:                                                                  │
│  ────────────────────────────────────────────────────────────────────────   │
│  10:00 - Last sync: Activity "Meeting" at 2 PM                              │
│  10:30 - User edits in CRM: Change to 3 PM                                  │
│  10:45 - User edits in Google: Change to 4 PM                               │
│  11:00 - Sync job runs                                                      │
│                                                                             │
│  Conflict Detection:                                                        │
│  ────────────────────────────────────────────────────────────────────────   │
│  CRM Activity modified: 10:30                                               │
│  Google Event modified: 10:45                                               │
│  Last synced: 10:00                                                         │
│                                                                             │
│  → Both modified since last sync!                                           │
│  → CONFLICT DETECTED                                                        │
│                                                                             │
│  Resolution Strategy: "Latest Wins"                                         │
│  ────────────────────────────────────────────────────────────────────────   │
│  Google modification (10:45) is newer than CRM (10:30)                      │
│  → Google wins                                                              │
│  → Update CRM activity to 4 PM                                              │
│  → Log conflict: "Resolved using latest change (Google)"                    │
│                                                                             │
│  Result:                                                                    │
│  ────────────────────────────────────────────────────────────────────────   │
│  CRM: Meeting at 4 PM                                                       │
│  Google: Meeting at 4 PM                                                    │
│  Status: Synced                                                             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## OAuth Scopes Required

### Google Calendar

| Scope | Permission | Why Needed |
|-------|------------|------------|
| `calendar.readonly` | Read calendar events | Pull events from Google |
| `calendar.events` | Create/update events | Push activities to Google |

### Microsoft Outlook/365

| Scope | Permission | Why Needed |
|-------|------------|------------|
| `Calendars.Read` | Read calendars | Pull events from Outlook |
| `Calendars.ReadWrite` | Modify calendars | Push activities to Outlook |

---

## Sync Statistics

### Example Dashboard

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CALENDAR SYNC DASHBOARD                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Configuration: Google Calendar (user@gmail.com)                            │
│  Status: Active                                                             │
│  Direction: Bidirectional                                                   │
│  Last Sync: 5 minutes ago                                                   │
│                                                                             │
│  Total Events Synced: 1,234                                                 │
│  ─────────────────────────────────────────────────────────────────────────  │
│  • Created: 456                                                             │
│  • Updated: 678                                                             │
│  • Deleted: 100                                                             │
│                                                                             │
│  Sync Direction Breakdown:                                                  │
│  ─────────────────────────────────────────────────────────────────────────  │
│  CRM → Google:    567 events  ████████████████████████                     │
│  Google → CRM:    489 events  ████████████████████                         │
│  Conflicts:        12 events  ██                                           │
│                                                                             │
│  Recent Errors: 3                                                           │
│  ─────────────────────────────────────────────────────────────────────────  │
│  • 2x "Rate limit exceeded" (Google API)                                    │
│  • 1x "Event not found" (deleted before sync)                               │
│                                                                             │
│  Average Sync Duration: 3.2 seconds                                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Best Practices

### 1. Handle Token Expiration

```csharp
// Detect expired token
if (response.StatusCode == 401) {
    // Try refresh token
    var newToken = await RefreshAccessTokenAsync(config.RefreshToken);
    config.AccessToken = newToken;
    config.TokenExpiresAt = DateTime.UtcNow.AddHours(1);
    
    // Retry original request
    response = await RetryRequestWithNewTokenAsync();
}

// If refresh token also expired
if (refreshFailed) {
    // Update config status
    config.Status = CalendarSyncStatus.TokenExpired;
    
    // Send notification to user
    await SendNotificationAsync(
        userId,
        "Calendar sync disconnected. Please reconnect."
    );
}
```

### 2. Use Incremental Sync

```csharp
// ❌ BAD: Full sync every time (slow!)
var allEvents = await GetAllEventsAsync();

// ✅ GOOD: Incremental sync
var changes = await GetEventsSinceAsync(config.LastSyncAt);
// Only sync what changed since last sync
```

### 3. Implement Sync Hash

```csharp
// Generate hash of event data
var hash = ComputeHash($"{title}|{startAt}|{endAt}|{location}");

// Compare with stored hash
if (mapping.SyncHash != hash) {
    // Event changed, need to sync
    await UpdateActivityAsync(activity);
    mapping.SyncHash = hash;
}
```

### 4. Rate Limiting

```csharp
// Respect provider rate limits
// Google: 1M requests/day = ~11 requests/second
await rateLimiter.WaitAsync();

try {
    var response = await googleCalendarApi.CreateEventAsync(event);
}
finally {
    rateLimiter.Release();
}
```

### 5. Error Handling & Retry

```csharp
// Retry with exponential backoff
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
    );

await retryPolicy.ExecuteAsync(async () => {
    await SyncCalendarAsync(config);
});
```

---

## Technical Overview

**Base URL:** `/api/v1/calendar`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get User Sync Configurations

Lấy danh sách calendar sync configs của user hiện tại.

```
GET /api/v1/calendar/sync/configurations
```

**Permission Required:** Authenticated user

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "config-guid",
      "provider": "GoogleCalendar",
      "providerAccountEmail": "user@gmail.com",
      "isActive": true,
      "syncToExternal": true,
      "syncFromExternal": true,
      "status": "Active",
      "lastSyncAt": "2026-01-18T10:00:00Z",
      "totalEventsSynced": 1234,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### 2. Get Configuration by ID

Lấy chi tiết một sync configuration.

```
GET /api/v1/calendar/sync/configurations/{id}
```

---

### 3. Create Configuration

Tạo calendar sync configuration mới (sau OAuth flow).

```
POST /api/v1/calendar/sync/configurations
```

#### Request Body

```json
{
  "provider": 0,
  "providerAccountEmail": "user@gmail.com",
  "accessToken": "ya29.a0...",
  "refreshToken": "1//0g...",
  "tokenExpiresAt": "2026-01-18T11:00:00Z",
  "syncToExternal": true,
  "syncFromExternal": true,
  "calendarIds": "primary,work@gmail.com",
  "syncDaysBack": 30,
  "syncDaysForward": 90
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `provider` | CalendarProvider | **Yes** | Provider enum |
| `providerAccountEmail` | string | **Yes** | Account email |
| `accessToken` | string | **Yes** | OAuth access token |
| `refreshToken` | string | No | OAuth refresh token |
| `tokenExpiresAt` | DateTime | No | Token expiry time |
| `syncToExternal` | bool | No | Push CRM → External (default: true) |
| `syncFromExternal` | bool | No | Pull External → CRM (default: true) |
| `calendarIds` | string | No | Comma-separated calendar IDs |
| `syncDaysBack` | int | No | Sync history (default: 30) |
| `syncDaysForward` | int | No | Sync future (default: 90) |

---

### 4. Update Configuration

Cập nhật sync settings.

```
PUT /api/v1/calendar/sync/configurations/{id}
```

#### Request Body (All fields optional)

```json
{
  "isActive": true,
  "syncToExternal": false,
  "syncFromExternal": true,
  "syncDaysBack": 60,
  "syncDaysForward": 180
}
```

---

### 5. Delete Configuration

Xóa calendar sync configuration.

```
DELETE /api/v1/calendar/sync/configurations/{id}
```

---

### 6. Get Authorization URL

Bước 1 của OAuth flow: Generate authorization URL.

```
GET /api/v1/calendar/sync/authorize/{provider}?redirectUri={uri}
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `provider` | string | **Yes** | Provider name (GoogleCalendar, MicrosoftOutlook) |
| `redirectUri` | string | **Yes** | Callback URL after OAuth |

#### Response

```json
{
  "success": true,
  "data": {
    "authorizationUrl": "https://accounts.google.com/o/oauth2/v2/auth?client_id=...&redirect_uri=...&scope=...",
    "state": "random-state-token"
  }
}
```

---

### 7. Exchange Token

Bước 2 của OAuth flow: Exchange auth code for access token.

```
POST /api/v1/calendar/sync/token-exchange/{provider}
```

#### Request Body

```json
{
  "code": "auth-code-from-provider",
  "redirectUri": "https://app.crm.com/settings/calendar/callback"
}
```

#### Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "new-config-guid",
    "provider": "GoogleCalendar",
    "providerAccountEmail": "user@gmail.com",
    "isActive": true,
    "status": "Active"
  }
}
```

---

### 8. Get External Calendars

Lấy danh sách calendars từ provider.

```
GET /api/v1/calendar/sync/configurations/{id}/calendars
```

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "primary",
      "name": "Primary Calendar",
      "description": "user@gmail.com",
      "isPrimary": true,
      "color": "#0B8043"
    },
    {
      "id": "work@gmail.com",
      "name": "Work",
      "isPrimary": false,
      "color": "#D50000"
    }
  ]
}
```

---

### 9. Sync Calendar (Manual Sync)

Trigger immediate sync for a configuration.

```
POST /api/v1/calendar/sync/configurations/{id}/sync
```

#### Response

```json
{
  "success": true,
  "data": {
    "success": true,
    "eventsCreated": 5,
    "eventsUpdated": 12,
    "eventsDeleted": 2,
    "totalSynced": 19,
    "durationSeconds": 3.2,
    "errors": []
  }
}
```

---

### 10. Sync All Calendars (Admin Background Job)

Sync tất cả active configurations (Admin only).

```
POST /api/v1/calendar/sync/sync-all
```

**Permission Required:** `Admin` role

---

### 11. Get Event Mapping

Lấy mapping giữa CRM activity và external event.

```
GET /api/v1/calendar/sync/mappings/activity/{activityId}
```

#### Response

```json
{
  "success": true,
  "data": {
    "id": "mapping-guid",
    "activityId": "activity-guid",
    "externalEventId": "abc123xyz",
    "externalCalendarId": "primary",
    "provider": "GoogleCalendar",
    "direction": "Bidirectional",
    "lastSyncedAt": "2026-01-18T10:00:00Z",
    "syncStatus": "Synced"
  }
}
```

---

### 12. Get Event Mappings for Config

Lấy tất cả mappings của một configuration.

```
GET /api/v1/calendar/sync/configurations/{id}/mappings
```

---

### 13. Export iCal

Export CRM activities sang iCal format (.ics).

```
POST /api/v1/calendar/export/ical
```

#### Request Body

```json
{
  "startDate": "2026-01-01",
  "endDate": "2026-02-01",
  "activityTypes": ["Meeting", "Call", "Task"],
  "includeDescription": true,
  "includeLocation": true,
  "includeReminders": true
}
```

#### Response

File download: `activities.ics` (text/calendar)

---

### 14. Export Single Activity iCal

Export một activity sang iCal.

```
GET /api/v1/calendar/export/ical/{activityId}
```

#### Response

File download: `activity-{id}.ics`

---

### 15. Get Activity Reminders

Lấy danh sách reminders của một activity.

```
GET /api/v1/calendar/reminders/activity/{activityId}
```

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "reminder-guid",
      "activityId": "activity-guid",
      "type": "ActivityStart",
      "minutesBefore": 60,
      "sendEmail": true,
      "sendNotification": true,
      "sendSms": false,
      "isSent": false,
      "scheduledFor": "2026-01-19T13:00:00Z"
    }
  ]
}
```

---

### 16. Get My Reminders

Lấy tất cả reminders của user hiện tại.

```
GET /api/v1/calendar/reminders/my-reminders
```

---

### 17. Create Reminder

Tạo activity reminder mới.

```
POST /api/v1/calendar/reminders
```

#### Request Body

```json
{
  "activityId": "activity-guid",
  "type": 0,
  "minutesBefore": 60,
  "sendEmail": true,
  "sendNotification": true,
  "sendSms": false
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `activityId` | Guid | **Yes** | Activity ID |
| `type` | ActivityReminderType | **Yes** | Reminder type |
| `minutesBefore` | int | **Yes** | Minutes before activity |
| `sendEmail` | bool | No | Send email reminder |
| `sendNotification` | bool | No | Send in-app notification |
| `sendSms` | bool | No | Send SMS reminder |

---

### 18. Delete Reminder

Xóa reminder.

```
DELETE /api/v1/calendar/reminders/{id}
```

---

## Enums

### CalendarProvider

| Value | Name | Description |
|-------|------|-------------|
| 0 | GoogleCalendar | Google Calendar |
| 1 | MicrosoftOutlook | Outlook.com |
| 2 | Microsoft365 | Microsoft 365 |
| 3 | AppleCalendar | Apple iCloud Calendar |
| 4 | Other | Other providers |

### CalendarSyncStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | Syncing normally |
| 1 | Paused | User paused sync |
| 2 | Error | Sync error occurred |
| 3 | TokenExpired | OAuth token expired |
| 4 | Disconnected | User disconnected |

### CalendarSyncDirection

| Value | Name | Description |
|-------|------|-------------|
| 0 | CrmToExternal | CRM → External only |
| 1 | ExternalToCrm | External → CRM only |
| 2 | Bidirectional | 2-way sync |

### CalendarEventSyncStatus

| Value | Name | Description |
|-------|------|-------------|
| 0 | Synced | Successfully synced |
| 1 | PendingSync | Waiting for sync |
| 2 | SyncFailed | Sync failed |
| 3 | Conflict | Conflict detected |
| 4 | Deleted | Event deleted |

### ActivityReminderType

| Value | Name | Description |
|-------|------|-------------|
| 0 | ActivityStart | Before activity start |
| 1 | ActivityDeadline | Before deadline |
| 2 | Custom | Custom time |

---

## Permissions

| Permission Code | Description |
|-----------------|-------------|
| Authenticated | All authenticated users can manage their own sync configs |
| `Admin` | Admins can trigger sync-all and view all configs |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
