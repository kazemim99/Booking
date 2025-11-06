# Notification System - Quick Reference Guide

## 🚀 Quick Start (5 Minutes)

### 1. Add to Program.cs
```csharp
using Booksy.Infrastructure.External.Hubs;

// Add services
builder.Services.AddNotificationBackgroundServices();
builder.Services.AddSignalRNotifications(builder.Configuration);

var app = builder.Build();

// Map hub
app.MapHub<NotificationHub>("/hubs/notifications");
```

### 2. Create Migration
```bash
dotnet ef migrations add AddNotificationSystem
dotnet ef database update
```

### 3. Done! Start the app
```bash
dotnet run
```

---

## 📝 Common Usage Patterns

### Sending a Notification (from Code)

```csharp
// Inject IMediator
private readonly ISender _mediator;

// Send immediately
var command = new SendNotificationCommand(
    RecipientId: userId,
    Type: NotificationType.BookingConfirmation,
    Channel: NotificationChannel.Email | NotificationChannel.SMS,
    Subject: "Booking Confirmed!",
    Body: "<h1>Your booking is confirmed</h1>",
    Priority: NotificationPriority.High);

var result = await _mediator.Send(command);
```

### Using Templates

```csharp
var command = new SendNotificationCommand(
    RecipientId: userId,
    Type: NotificationType.BookingConfirmation,
    Channel: NotificationChannel.Email,
    Subject: "", // Will be filled by template
    Body: "", // Will be filled by template
    TemplateKey: "booking-confirmation",
    TemplateVariables: new Dictionary<string, object>
    {
        ["CustomerName"] = "John Doe",
        ["ServiceName"] = "Haircut",
        ["BookingDate"] = DateTime.Now,
        ["ProviderName"] = "Beauty Salon"
    });

await _mediator.Send(command);
```

### Creating Event Handlers

```csharp
public class MyEventNotificationHandler : IDomainEventHandler<MyDomainEvent>
{
    private readonly ISender _mediator;

    public async Task Handle(MyDomainEvent notification, CancellationToken ct)
    {
        var command = new SendNotificationCommand(
            RecipientId: notification.UserId,
            Type: NotificationType.Custom,
            Channel: NotificationChannel.All,
            Subject: "Something happened!",
            Body: "Details here...");

        await _mediator.Send(command, ct);
    }
}
```

---

## 🔔 Notification Types

```csharp
BookingConfirmation       // Booking confirmed
BookingCancellation       // Booking cancelled
BookingRescheduled        // Booking rescheduled
BookingReminder           // Upcoming appointment
BookingNoShow             // Customer no-show
ReviewRequest             // Review request after service

PaymentReceived           // Payment successful
PaymentFailed             // Payment failed
RefundProcessed           // Refund completed
InvoiceGenerated          // Invoice created
PayoutCompleted           // Provider payout

WelcomeEmail              // New user welcome
EmailVerification         // Email verification
PasswordReset             // Password reset
PhoneVerificationOtp      // Phone OTP

Announcement              // System announcements
Custom                    // Custom notifications
```

---

## 📡 Channels

```csharp
NotificationChannel.Email              // Send via email
NotificationChannel.SMS                // Send via SMS
NotificationChannel.PushNotification   // Send push notification
NotificationChannel.InApp              // Send via SignalR (real-time)

// Multiple channels
NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp
```

---

## ⚡ Priority Levels

```csharp
NotificationPriority.Low       // Newsletters, tips
NotificationPriority.Normal    // General notifications
NotificationPriority.High      // Booking confirmations, payments
NotificationPriority.Urgent    // Critical alerts, failures
```

---

## 📊 API Endpoints Cheat Sheet

### Send Notification
```http
POST /api/v1/notifications
Authorization: Bearer {token}

{
  "recipientId": "guid",
  "type": "BookingConfirmation",
  "channel": "Email, SMS",
  "subject": "Title",
  "body": "Message"
}
```

### Get History
```http
GET /api/v1/notifications/history?pageNumber=1&pageSize=20&channel=Email&status=Delivered
Authorization: Bearer {token}
```

### Update Preferences
```http
PUT /api/v1/notifications/preferences
Authorization: Bearer {token}

{
  "enabledChannels": "Email, InApp",
  "quietHoursStart": "22:00",
  "quietHoursEnd": "08:00",
  "marketingOptIn": false
}
```

---

## 🎨 Frontend Integration

### Vue.js
```javascript
import * as signalR from '@microsoft/signalr'

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/notifications', {
    accessTokenFactory: () => localStorage.getItem('token')
  })
  .withAutomaticReconnect()
  .build()

connection.on('ReceiveNotification', (notification) => {
  console.log(notification)
})

await connection.start()
```

### React
```typescript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/notifications')
  .build()

connection.on('ReceiveNotification', (notification) => {
  setNotifications(prev => [notification, ...prev])
})

connection.start()
```

---

## 🔧 Configuration (appsettings.json)

```json
{
  "SendGrid": {
    "ApiKey": "SG.xxx",
    "FromEmail": "noreply@domain.com"
  },
  "SignalR": {
    "AllowedOrigins": ["http://localhost:3000"]
  },
  "Notification": {
    "ProcessorIntervalSeconds": 30,
    "MaxRetryAttempts": 5,
    "BatchSize": 100
  }
}
```

---

## 🐛 Debugging

### Check Background Workers Running
```bash
# Look for these in logs:
NotificationProcessorService started
ScheduledNotificationService started
NotificationCleanupService started
```

### Check Notification Status
```sql
SELECT "Type", "Channel", "Status", COUNT(*)
FROM "Notifications"
WHERE "CreatedAt" >= NOW() - INTERVAL '1 hour'
GROUP BY "Type", "Channel", "Status";
```

### Test SignalR Connection
```javascript
// Browser console
const conn = new signalR.HubConnectionBuilder()
  .withUrl('https://localhost:5001/hubs/notifications')
  .build()

conn.on('ReceiveNotification', n => console.log(n))
await conn.start()
console.log('Connected!')
```

---

## ⏱️ Background Worker Intervals

| Service | Interval | Purpose |
|---------|----------|---------|
| NotificationProcessor | 30 seconds | Send queued notifications |
| ScheduledNotification | 1 minute | Process scheduled notifications |
| NotificationCleanup | 1 hour | Clean expired notifications |

---

## 🔐 Security

### JWT Claims Required
```json
{
  "sub": "user-guid",          // Preferred
  "userId": "user-guid"        // Alternative
}
```

### Role Requirements
- All endpoints: `Authenticated User`
- Bulk send: `Admin` or `Provider` role

---

## 📦 Available Templates

```
booking-confirmation           booking-reminder
booking-cancellation          booking-rescheduled
booking-no-show              booking-review-request

payment-success              payment-failed
refund-processed             invoice-generated
payout-completed

welcome-email                email-verification
password-reset               phone-verification-otp
```

---

## 🎯 Template Variables

```csharp
// Booking templates
CustomerName, ServiceName, ProviderName, BookingDate,
StartTime, EndTime, Duration, Location, Price,
CancellationPolicy

// Payment templates
Amount, Currency, PaymentMethod, TransactionId,
PaymentDate, RefundAmount, InvoiceNumber

// User templates
FirstName, LastName, Email, PhoneNumber,
VerificationCode, ResetLink, ActivationLink
```

---

## 📈 Monitoring Queries

### Delivery Rate (Last 24 Hours)
```sql
SELECT
  ROUND(COUNT(CASE WHEN "Status" = 'Delivered' THEN 1 END) * 100.0 / COUNT(*), 2) as DeliveryRate
FROM "Notifications"
WHERE "CreatedAt" >= NOW() - INTERVAL '24 hours';
```

### Channel Distribution
```sql
SELECT "Channel", COUNT(*) as Count
FROM "Notifications"
WHERE "CreatedAt" >= NOW() - INTERVAL '7 days'
GROUP BY "Channel"
ORDER BY Count DESC;
```

### Failed Notifications
```sql
SELECT "Type", "FailureReason", COUNT(*)
FROM "Notifications"
WHERE "Status" = 'Failed'
  AND "CreatedAt" >= NOW() - INTERVAL '24 hours'
GROUP BY "Type", "FailureReason";
```

---

## 🚨 Common Errors & Solutions

| Error | Solution |
|-------|----------|
| `Notifications stay Pending` | Check background workers started |
| `SignalR 401 Unauthorized` | Verify JWT token in accessTokenFactory |
| `Email not sending` | Check SendGrid API key |
| `SMS not sending` | Check Rahyab credentials |
| `High DB load` | Increase ProcessorIntervalSeconds |

---

## 📚 Full Documentation

- **Complete Setup**: `NOTIFICATION_SYSTEM_SETUP_GUIDE.md`
- **SignalR Setup**: `SIGNALR_SETUP_GUIDE.md`
- **Postman Collection**: `Notification_API.postman_collection.json`
- **Config Example**: `appsettings.Notifications.example.json`
