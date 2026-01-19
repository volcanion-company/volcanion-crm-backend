# Notifications API Documentation

## Tổng quan Module

### Notification System là gì?

**Notification System** là hệ thống thông báo real-time giúp users cập nhật về các sự kiện quan trọng trong CRM (lead mới, deal won, ticket assigned, etc.).

### Tại sao cần Notifications?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  WHY REAL-TIME NOTIFICATIONS MATTER                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  WITHOUT Notifications:                                                     │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Sales rep misses hot lead (assigned 2 hours ago)                         │
│  • Manager unaware of deal requiring approval                               │
│  • Support agent doesn't see urgent ticket                                  │
│  • Team members manually refresh pages constantly                           │
│  → Lost opportunities, slow response, poor customer experience              │
│                                                                             │
│  WITH Real-Time Notifications:                                              │
│  ─────────────────────────────────────────────────────────────────────     │
│  • Sales rep gets instant alert: "New hot lead assigned to you"             │
│  • Manager notified: "Deal $50k requires your approval"                     │
│  • Support gets bell icon: "Urgent ticket #1234 escalated"                  │
│  • Badge counter shows: (3) unread notifications                            │
│  → Immediate action, faster response, better outcomes                       │
│                                                                             │
│  Notification Channels:                                                     │
│  • In-App: Bell icon with badge counter (primary)                           │
│  • Email: Critical notifications sent to inbox                              │
│  • SMS: Urgent alerts only (optional)                                       │
│  • Push: Mobile app notifications (future)                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Notification Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      NOTIFICATION DELIVERY FLOW                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. Event Trigger                                                           │
│     ┌─────────────────────────────────────┐                                │
│     │ • Lead assigned to sales rep        │                                │
│     │ • Opportunity requires approval     │                                │
│     │ • Ticket escalated to manager       │                                │
│     │ • Customer added note               │                                │
│     │ • Contract approaching expiration   │                                │
│     └────────────────┬────────────────────┘                                │
│                      ▼                                                      │
│  2. Notification Service                                                    │
│     ┌──────────────────────────────────────┐                               │
│     │ NotificationService.SendAsync()      │                               │
│     │   • Determine recipient(s)           │                               │
│     │   • Build notification message       │                               │
│     │   • Select channels (in-app/email)   │                               │
│     │   • Apply user preferences           │                               │
│     └────────────────┬─────────────────────┘                               │
│                      ▼                                                      │
│  3. Multi-Channel Delivery                                                  │
│     ┌─────────────┬─────────────┬─────────────┐                            │
│     │   In-App    │    Email    │     SMS     │                            │
│     │             │             │             │                            │
│     │ • Save to   │ • Queue     │ • Send via  │                            │
│     │   DB        │   email     │   Twilio    │                            │
│     │ • Badge +1  │ • Template  │ • Urgent    │                            │
│     │ • Real-time │ • Send      │   only      │                            │
│     │   push      │   async     │             │                            │
│     └─────┬───────┴──────┬──────┴──────┬──────┘                            │
│           ▼              ▼             ▼                                    │
│  4. User Receipt                                                            │
│     • Bell icon updates: 🔔 (3)                                             │
│     • Email arrives in inbox                                                │
│     • SMS received on mobile                                                │
│                      ▼                                                      │
│  5. User Action                                                             │
│     • Click notification → Navigate to lead                                 │
│     • Mark as read → Badge decrements                                       │
│     • "Mark all as read" → Badge clears                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Notification Types

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        NOTIFICATION CATEGORIES                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Lead Notifications:                                                        │
│  ─────────────────────────────────────────────────────────────────────     │
│  🎯 "New lead 'Acme Corp' assigned to you"                                  │
│  🔥 "Hot lead 'Beta Inc' requires follow-up"                                │
│  ✅ "Lead 'Gamma LLC' converted to customer"                                │
│                                                                             │
│  Opportunity Notifications:                                                 │
│  ─────────────────────────────────────────────────────────────────────     │
│  💰 "Deal 'Enterprise License' moved to Negotiation ($50k)"                 │
│  ⏰ "Opportunity 'Q1 Renewal' close date in 3 days"                          │
│  ✅ "Deal 'Web Design Project' marked as Won! ($25k)"                       │
│  ⚠️  "Deal 'Cloud Migration' requires manager approval"                     │
│                                                                             │
│  Ticket Notifications:                                                      │
│  ─────────────────────────────────────────────────────────────────────     │
│  🎫 "Urgent ticket #1234 assigned to you"                                   │
│  ⏰ "Ticket #5678 SLA expiring in 1 hour"                                    │
│  🚨 "Critical ticket #999 escalated to manager"                             │
│  💬 "Customer replied to ticket #456"                                       │
│                                                                             │
│  Activity/Task Notifications:                                               │
│  ─────────────────────────────────────────────────────────────────────     │
│  📞 "Reminder: Call with Acme Corp in 15 minutes"                           │
│  ✉️  "Email follow-up due: Beta Inc proposal"                               │
│  📅 "Meeting scheduled: Demo with Gamma at 2pm"                             │
│                                                                             │
│  System Notifications:                                                      │
│  ─────────────────────────────────────────────────────────────────────     │
│  ⚙️  "Password changed successfully"                                        │
│  📊 "Monthly sales report ready"                                            │
│  🎉 "Congrats! You hit 100% of quota this month"                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Ý nghĩa nghiệp vụ từng chức năng

### 1. Get Notifications (Danh sách notifications)

**Nghiệp vụ thực tế:**
- User xem notifications của mình
- Filter unread only

**Ví dụ thực tế:**
> Sales rep clicks bell icon 🔔 (5):
> - 🎯 New lead "Acme Corp" assigned to you (5 mins ago) ⚪ Unread
> - 💰 Deal "Enterprise License" moved to Proposal ($50k) (1 hour ago) ⚪ Unread
> - ✅ Lead "Beta Inc" converted to customer (2 hours ago) ⚪ Unread
> - 📞 Reminder: Call with Gamma LLC in 15 mins (3 hours ago) • Read
> - ✉️ Email follow-up due: Delta proposal (1 day ago) • Read
> → Total: 5 notifications (3 unread)

---

### 2. Get Unread Count (Badge counter)

**Nghiệp vụ thực tế:**
- Real-time badge counter for bell icon
- Polled every 30 seconds

**Ví dụ thực tế:**
> Bell icon displays: 🔔 (3)
> - User has 3 unread notifications
> - Click to see details
> → After reading: 🔔 (0)

---

### 3. Mark as Read (Đánh dấu đã đọc)

**Nghiệp vụ thực tế:**
- User clicks notification → mark as read
- Badge counter decrements

**Ví dụ thực tế:**
> Sales rep clicks: "New lead assigned to you"
> - Notification marked as read ✓
> - Badge: 🔔 (5) → 🔔 (4)
> - Navigates to lead detail page
> → User engaged with notification

---

### 4. Mark All as Read (Đánh dấu tất cả đã đọc)

**Nghiệp vụ thực tế:**
- Clear all unread notifications
- Badge resets to 0

**Ví dụ thực tế:**
> Manager has 25 unread notifications:
> - Clicks "Mark all as read" button
> - All 25 notifications marked read
> - Badge: 🔔 (25) → 🔔 (0)
> → Clean slate

---

### 5. Send Test Notification (Admin test)

**Nghiệp vụ thực tế:**
- Admin tests notification system
- Debug delivery issues

**Ví dụ thực tế:**
> Admin testing email notifications:
> - Send test to: john@example.com
> - Title: "Test Notification"
> - Message: "This is a test"
> - Channels: In-App + Email
> → John receives notification via both channels

---

## Notification Priority Levels

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      NOTIFICATION PRIORITIES                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Priority: Low                                                              │
│  ─────────────────────────────────────────────────────────────────────     │
│  • In-App only (no email/SMS)                                               │
│  • Can be batched (digest emails)                                           │
│  • Examples:                                                                │
│    - "Report generated successfully"                                        │
│    - "Task completed"                                                       │
│    - "Comment added to opportunity"                                         │
│                                                                             │
│  Priority: Normal                                                           │
│  ─────────────────────────────────────────────────────────────────────     │
│  • In-App + Email (based on user preference)                                │
│  • Standard delivery                                                        │
│  • Examples:                                                                │
│    - "New lead assigned to you"                                             │
│    - "Deal moved to next stage"                                             │
│    - "Task reminder"                                                        │
│                                                                             │
│  Priority: High                                                             │
│  ─────────────────────────────────────────────────────────────────────     │
│  • In-App + Email (always sent)                                             │
│  • Immediate delivery                                                       │
│  • Examples:                                                                │
│    - "Deal requires approval"                                               │
│    - "Hot lead assigned"                                                    │
│    - "SLA expiring soon"                                                    │
│                                                                             │
│  Priority: Urgent                                                           │
│  ─────────────────────────────────────────────────────────────────────     │
│  • In-App + Email + SMS (all channels)                                      │
│  • Push to mobile if available                                              │
│  • Examples:                                                                │
│    - "Critical ticket escalated"                                            │
│    - "System security alert"                                                │
│    - "Payment failed - subscription suspended"                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## User Notification Preferences

### Preference Settings

```csharp
public class NotificationPreferences
{
    // Email preferences
    public bool EmailEnabled { get; set; } = true;
    public bool EmailForLeads { get; set; } = true;
    public bool EmailForOpportunities { get; set; } = true;
    public bool EmailForTickets { get; set; } = true;
    public bool EmailDigest { get; set; } = false; // Daily digest instead of real-time
    
    // In-app preferences
    public bool InAppEnabled { get; set; } = true;
    
    // SMS preferences
    public bool SmsEnabled { get; set; } = false;
    public bool SmsUrgentOnly { get; set; } = true;
    
    // Frequency limits
    public int MaxEmailsPerDay { get; set; } = 50;
    
    // Quiet hours (no notifications)
    public TimeSpan QuietHoursStart { get; set; } = new TimeSpan(22, 0, 0); // 10pm
    public TimeSpan QuietHoursEnd { get; set; } = new TimeSpan(8, 0, 0);    // 8am
}
```

**Example User Preference Scenarios:**

1. **Sales Rep (Active):**
   - In-App: ✓ Enabled
   - Email: ✓ All notifications
   - SMS: ✓ Urgent only
   - Wants real-time alerts for hot leads

2. **Manager (Selective):**
   - In-App: ✓ Enabled
   - Email: ✓ Digest mode (once daily at 9am)
   - SMS: ✗ Disabled
   - Only needs summary, not every detail

3. **Support Agent (24/7):**
   - In-App: ✓ Enabled
   - Email: ✓ Urgent tickets only
   - SMS: ✓ Critical escalations
   - No quiet hours (always on-call)

---

## Best Practices

### 1. Avoid Notification Fatigue

```
❌ BAD: Send notification for every tiny action
"User A viewed lead" (who cares?)
"User B opened report" (spam!)
"User C logged in" (noise!)
→ 100 notifications per day → User ignores all

✅ GOOD: Only actionable, important notifications
"Hot lead assigned to you" (action needed!)
"Deal requires your approval" (decision needed!)
"Ticket SLA expiring in 1 hour" (urgent!)
→ 5-10 meaningful notifications per day → User engages
```

### 2. Smart Batching

```csharp
// ❌ BAD: Send 50 emails for 50 leads
foreach (var lead in newLeads)
{
    await SendEmailAsync($"New lead: {lead.Name}");
}

// ✅ GOOD: Batch into one digest email
var leadNames = newLeads.Select(l => l.Name);
await SendEmailAsync($"You have {newLeads.Count} new leads: {string.Join(", ", leadNames)}");
```

### 3. Respect Quiet Hours

```csharp
public async Task SendNotificationAsync(NotificationRequest request)
{
    var user = await GetUserAsync(request.UserId);
    var prefs = user.NotificationPreferences;
    
    // Check quiet hours
    if (prefs.QuietHoursEnabled && 
        IsInQuietHours(DateTime.Now.TimeOfDay, prefs.QuietHoursStart, prefs.QuietHoursEnd))
    {
        if (request.Priority == NotificationPriority.Urgent)
        {
            // Send urgent notifications even in quiet hours
            await SendAsync(request);
        }
        else
        {
            // Queue for later (after quiet hours)
            await QueueForLaterAsync(request, prefs.QuietHoursEnd);
        }
    }
    else
    {
        await SendAsync(request);
    }
}
```

### 4. Notification Expiry

```csharp
// Delete old read notifications (keep DB clean)
public async Task CleanupOldNotificationsAsync()
{
    var cutoffDate = DateTime.UtcNow.AddDays(-30);
    
    await _db.Notifications
        .Where(n => n.IsRead && n.CreatedAt < cutoffDate)
        .ExecuteDeleteAsync();
        
    _logger.LogInformation("Deleted read notifications older than 30 days");
}
```

---

## Technical Overview

**Base URL:** `/api/v1/notifications`

**Authentication:** Bearer Token (JWT)

---

## Endpoints

### 1. Get Notifications

Lấy notifications của current user.

```
GET /api/v1/notifications
```

**Query Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `unreadOnly` | boolean | false | Only return unread notifications |
| `pageSize` | int | 50 | Number of notifications to return (max 100) |

**Response:**

```json
{
  "success": true,
  "data": [
    {
      "id": "notif-guid-1",
      "title": "New Lead Assigned",
      "message": "Hot lead 'Acme Corp' assigned to you",
      "type": "Lead",
      "priority": "High",
      "isRead": false,
      "entityType": "Lead",
      "entityId": "lead-guid",
      "actionUrl": "/leads/lead-guid",
      "createdAt": "2026-01-18T10:00:00Z"
    },
    {
      "id": "notif-guid-2",
      "title": "Deal Requires Approval",
      "message": "Opportunity 'Enterprise License' requires your approval ($50,000)",
      "type": "Opportunity",
      "priority": "High",
      "isRead": false,
      "entityType": "Opportunity",
      "entityId": "opp-guid",
      "actionUrl": "/opportunities/opp-guid",
      "createdAt": "2026-01-18T09:30:00Z"
    }
  ]
}
```

---

### 2. Get Unread Count

Lấy số lượng unread notifications (for badge).

```
GET /api/v1/notifications/unread-count
```

**Response:**

```json
{
  "success": true,
  "data": {
    "count": 5
  }
}
```

---

### 3. Mark as Read

Đánh dấu notification đã đọc.

```
PUT /api/v1/notifications/{id}/read
```

**Response:**

```json
{
  "success": true,
  "message": "Notification marked as read"
}
```

---

### 4. Mark All as Read

Đánh dấu tất cả notifications đã đọc.

```
PUT /api/v1/notifications/mark-all-read
```

**Response:**

```json
{
  "success": true,
  "message": "All notifications marked as read"
}
```

---

### 5. Send Test Notification (Admin Only)

Send test notification (for debugging).

```
POST /api/v1/notifications/test
```

**Permission Required:** Admin role

**Request Body:**

```json
{
  "userId": "user-guid",
  "title": "Test Notification",
  "message": "This is a test notification",
  "channels": 3
}
```

**Request Schema:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `userId` | Guid | No | Target user (default: current user) |
| `title` | string | No | Notification title (default: "Test Notification") |
| `message` | string | No | Notification message (default: "This is a test...") |
| `channels` | NotificationChannel | No | Delivery channels (default: InApp) |

**Response:**

```json
{
  "success": true,
  "message": "Test notification sent"
}
```

---

## Enums

### NotificationType

| Value | Name | Description |
|-------|------|-------------|
| 0 | System | System-level notifications |
| 1 | Lead | Lead-related notifications |
| 2 | Opportunity | Opportunity notifications |
| 3 | Ticket | Support ticket notifications |
| 4 | Activity | Task/Activity reminders |
| 5 | Approval | Approval requests |

### NotificationPriority

| Value | Name | Description |
|-------|------|-------------|
| 0 | Low | In-app only, can be batched |
| 1 | Normal | In-app + email (if enabled) |
| 2 | High | In-app + email (always sent) |
| 3 | Urgent | All channels (in-app + email + SMS) |

### NotificationChannel (Flags Enum)

| Value | Name | Description |
|-------|------|-------------|
| 1 | InApp | In-app notification |
| 2 | Email | Email notification |
| 4 | Sms | SMS notification |
| 8 | Push | Mobile push notification |

**Note:** Can combine channels: `InApp | Email = 3`, `InApp | Email | SMS = 7`

---

## Client Implementation Example

### React TypeScript NotificationBell Component

```typescript
import React, { useEffect, useState } from 'react';
import { Bell } from 'lucide-react';
import { api } from '@/lib/api';

interface Notification {
  id: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  actionUrl?: string;
}

export function NotificationBell() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);

  // Poll for unread count every 30 seconds
  useEffect(() => {
    fetchUnreadCount();
    const interval = setInterval(fetchUnreadCount, 30000);
    return () => clearInterval(interval);
  }, []);

  const fetchUnreadCount = async () => {
    const response = await api.get('/notifications/unread-count');
    setUnreadCount(response.data.count);
  };

  const fetchNotifications = async () => {
    const response = await api.get('/notifications?unreadOnly=false&pageSize=20');
    setNotifications(response.data);
  };

  const markAsRead = async (id: string) => {
    await api.put(`/notifications/${id}/read`);
    setUnreadCount(prev => Math.max(0, prev - 1));
    setNotifications(prev =>
      prev.map(n => n.id === id ? { ...n, isRead: true } : n)
    );
  };

  const markAllAsRead = async () => {
    await api.put('/notifications/mark-all-read');
    setUnreadCount(0);
    setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
  };

  const handleBellClick = () => {
    if (!isOpen) {
      fetchNotifications();
    }
    setIsOpen(!isOpen);
  };

  return (
    <div className="relative">
      {/* Bell Icon */}
      <button onClick={handleBellClick} className="relative p-2">
        <Bell size={24} />
        {unreadCount > 0 && (
          <span className="absolute top-0 right-0 bg-red-500 text-white text-xs rounded-full px-2 py-0.5">
            {unreadCount}
          </span>
        )}
      </button>

      {/* Notification Dropdown */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 bg-white shadow-lg rounded-lg z-50">
          <div className="flex justify-between items-center p-4 border-b">
            <h3 className="font-semibold">Notifications</h3>
            {unreadCount > 0 && (
              <button onClick={markAllAsRead} className="text-sm text-blue-600">
                Mark all as read
              </button>
            )}
          </div>
          <div className="max-h-96 overflow-y-auto">
            {notifications.map(notif => (
              <div
                key={notif.id}
                className={`p-4 border-b hover:bg-gray-50 cursor-pointer ${
                  !notif.isRead ? 'bg-blue-50' : ''
                }`}
                onClick={() => {
                  markAsRead(notif.id);
                  if (notif.actionUrl) {
                    window.location.href = notif.actionUrl;
                  }
                }}
              >
                <div className="flex justify-between">
                  <h4 className="font-semibold text-sm">{notif.title}</h4>
                  {!notif.isRead && (
                    <span className="w-2 h-2 bg-blue-600 rounded-full"></span>
                  )}
                </div>
                <p className="text-sm text-gray-600 mt-1">{notif.message}</p>
                <p className="text-xs text-gray-400 mt-1">
                  {new Date(notif.createdAt).toLocaleString()}
                </p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-18 | Initial release |
