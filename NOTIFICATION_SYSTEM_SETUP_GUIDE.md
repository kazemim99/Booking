# Notification & Communication System - Complete Setup Guide

## 📋 Table of Contents
1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Database Setup](#database-setup)
6. [Running the System](#running-the-system)
7. [API Documentation](#api-documentation)
8. [Frontend Integration](#frontend-integration)
9. [Testing](#testing)
10. [Production Deployment](#production-deployment)
11. [Monitoring & Troubleshooting](#monitoring--troubleshooting)

---

## Overview

The Notification & Communication System provides:
- **Multi-channel delivery**: Email, SMS, Push Notifications, In-App (SignalR)
- **Template management**: 15 pre-built templates with variable substitution
- **Background processing**: Automatic queuing, scheduling, and retry logic
- **User preferences**: Granular control over notification channels and types
- **Real-time delivery**: SignalR for instant in-app notifications
- **Analytics**: Delivery tracking, success/failure rates, user engagement

---

## Prerequisites

### Required Software
- .NET 8.0 SDK or later
- PostgreSQL 14+ (for database)
- Redis (optional, for SignalR scaling)
- Node.js 18+ (for frontend)

### External Services (Optional but Recommended)
- **SendGrid** - Email delivery ([Get API Key](https://sendgrid.com))
- **Rahyab SMS** - SMS delivery (Iranian SMS provider)
- **Firebase Cloud Messaging** - Push notifications ([Setup FCM](https://firebase.google.com))

---

## Installation

### 1. Clone the Repository
```bash
cd /path/to/Booking
git checkout claude/notification-communication-system-011CUqTSNUvx1YVDjTFAb3v7
```

### 2. Restore NuGet Packages
```bash
dotnet restore
```

### 3. Install Required Packages (if not already included)
```bash
# SignalR
dotnet add package Microsoft.AspNetCore.SignalR

# For Redis backplane (optional, for scaling)
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis

# SendGrid
dotnet add package SendGrid

# For background services (already in .NET)
# No additional packages needed
```

---

## Configuration

### 1. Update `appsettings.json`

Add the following configuration to your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=booksy;Username=postgres;Password=yourpassword",
    "Redis": "localhost:6379" // Optional, for SignalR scaling
  },

  "SendGrid": {
    "ApiKey": "SG.your-sendgrid-api-key-here",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Booksy Notifications"
  },

  "Rahyab": {
    "ApiKey": "your-rahyab-api-key",
    "BaseUrl": "https://api.rahyab.ir",
    "SenderId": "your-sender-number"
  },

  "Firebase": {
    "ServerKey": "your-firebase-server-key",
    "SenderId": "your-sender-id",
    "ProjectId": "your-project-id"
  },

  "SignalR": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:8080",
      "https://yourdomain.com"
    ]
  },

  "Notification": {
    "DefaultExpirationHours": 72,
    "MaxRetryAttempts": 5,
    "RetryDelays": [5, 15, 45, 135, 405],
    "ProcessorIntervalSeconds": 30,
    "SchedulerIntervalMinutes": 1,
    "CleanupIntervalHours": 1,
    "BatchSize": 100
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore.SignalR": "Information",
      "Booksy.ServiceCatalog.Application.Services.BackgroundServices": "Information"
    }
  }
}
```

### 2. Update `Program.cs`

Add the required services to your `Program.cs`:

```csharp
using Booksy.Infrastructure.External.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ... existing service registrations ...

// Add Notification System
builder.Services.AddServiceCatalogApplication();
builder.Services.AddServiceCatalogInfrastructure(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);

// Add Background Workers for Notifications
builder.Services.AddNotificationBackgroundServices();

// Add SignalR for Real-Time Notifications
builder.Services.AddSignalRNotifications(builder.Configuration);

var app = builder.Build();

// ... existing middleware ...

// Enable CORS for SignalR (if needed)
app.UseCors("SignalRPolicy");

// Map SignalR Hub (BEFORE MapControllers)
app.MapHub<NotificationHub>("/hubs/notifications");

// Map API Controllers
app.MapControllers();

app.Run();
```

---

## Database Setup

### 1. Create Migration

```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

dotnet ef migrations add AddNotificationSystem \
  --context ServiceCatalogDbContext \
  --output-dir Persistence/Migrations
```

### 2. Apply Migration

```bash
dotnet ef database update --context ServiceCatalogDbContext
```

### 3. Verify Tables Created

Connect to your PostgreSQL database and verify these tables exist:
- `Notifications`
- `NotificationTemplates`
- `UserNotificationPreferences`

### 4. Seed Notification Templates

The system includes a seeder with 15 pre-built templates. Run it:

```csharp
// In your Program.cs or Startup
await app.Services.InitializeDatabaseAsync();
```

Or manually seed:

```bash
dotnet run --seed-data
```

---

## Running the System

### 1. Start the API

```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run
```

### 2. Verify Background Workers Started

Check your logs for:
```
info: NotificationProcessorService[0] NotificationProcessorService started
info: ScheduledNotificationService[0] ScheduledNotificationService started
info: NotificationCleanupService[0] NotificationCleanupService started
```

### 3. Test SignalR Connection

Open browser console and test:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:5001/hubs/notifications")
  .build();

connection.on("ReceiveNotification", (notification) => {
  console.log("Notification received:", notification);
});

await connection.start();
console.log("Connected to NotificationHub");
```

---

## API Documentation

### Base URL
```
https://localhost:5001/api/v1
```

### Authentication
All endpoints require JWT Bearer token:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

### Send Notification

**POST** `/notifications`

```json
{
  "recipientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "BookingConfirmation",
  "channel": "Email, SMS, InApp",
  "subject": "Booking Confirmed",
  "body": "<h1>Your booking is confirmed!</h1>",
  "priority": "High",
  "metadata": {
    "bookingId": "booking-123"
  }
}
```

**Response:**
```json
{
  "notificationId": "notification-guid",
  "status": "Queued",
  "queuedAt": "2025-11-06T10:30:00Z"
}
```

### Schedule Notification

**POST** `/notifications/schedule`

```json
{
  "recipientId": "user-guid",
  "type": "BookingReminder",
  "channel": "Email, SMS",
  "subject": "Appointment Reminder",
  "body": "Your appointment is tomorrow at 2 PM",
  "scheduledFor": "2025-11-07T14:00:00Z",
  "priority": "Normal"
}
```

### Get Notification History

**GET** `/notifications/history?pageNumber=1&pageSize=20&channel=Email&status=Delivered`

**Response:**
```json
{
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8,
  "notifications": [
    {
      "notificationId": "guid",
      "type": "BookingConfirmation",
      "channel": "Email",
      "status": "Delivered",
      "subject": "Booking Confirmed",
      "createdAt": "2025-11-06T10:00:00Z",
      "deliveredAt": "2025-11-06T10:00:15Z"
    }
  ]
}
```

### Update Preferences

**PUT** `/notifications/preferences`

```json
{
  "enabledChannels": "Email, InApp",
  "enabledTypes": "BookingConfirmation, BookingCancellation, PaymentReceived",
  "quietHoursStart": "22:00",
  "quietHoursEnd": "08:00",
  "marketingOptIn": false,
  "maxNotificationsPerDay": 50,
  "preferredLanguage": "en"
}
```

### Get Analytics

**GET** `/notifications/analytics?startDate=2025-11-01&endDate=2025-11-06`

**Response:**
```json
{
  "totalNotifications": 342,
  "sentNotifications": 340,
  "deliveredNotifications": 335,
  "failedNotifications": 7,
  "deliveryRate": 98.5,
  "notificationsByChannel": {
    "Email": 150,
    "SMS": 100,
    "InApp": 92
  },
  "notificationsByStatus": {
    "Delivered": 335,
    "Sent": 5,
    "Failed": 2
  }
}
```

---

## Frontend Integration

### Install SignalR Client

```bash
npm install @microsoft/signalr
```

### Vue.js Example

```vue
<template>
  <div class="notifications">
    <div v-for="notification in notifications" :key="notification.id" class="notification-item">
      <h4>{{ notification.title }}</h4>
      <p>{{ notification.message }}</p>
      <button @click="markAsRead(notification.id)">Mark as Read</button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'

const notifications = ref([])
let connection = null

onMounted(async () => {
  // Create SignalR connection
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/notifications', {
      accessTokenFactory: () => localStorage.getItem('authToken')
    })
    .withAutomaticReconnect()
    .build()

  // Listen for new notifications
  connection.on('ReceiveNotification', (notification) => {
    notifications.value.unshift(notification)
    showToast(notification.title, notification.message)
  })

  // Listen for read confirmations
  connection.on('NotificationRead', (notificationId) => {
    const index = notifications.value.findIndex(n => n.id === notificationId)
    if (index !== -1) {
      notifications.value.splice(index, 1)
    }
  })

  // Start connection
  try {
    await connection.start()
    console.log('Connected to NotificationHub')
  } catch (err) {
    console.error('SignalR connection error:', err)
  }
})

onUnmounted(async () => {
  if (connection) {
    await connection.stop()
  }
})

const markAsRead = async (notificationId) => {
  try {
    await connection.invoke('MarkAsRead', notificationId)
  } catch (err) {
    console.error('Error marking notification as read:', err)
  }
}

const showToast = (title, message) => {
  // Your toast notification implementation
}
</script>
```

### React Example

```typescript
import { useEffect, useState } from 'react'
import * as signalR from '@microsoft/signalr'

export function useNotifications() {
  const [notifications, setNotifications] = useState([])
  const [connection, setConnection] = useState(null)

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => localStorage.getItem('authToken')
      })
      .withAutomaticReconnect()
      .build()

    newConnection.on('ReceiveNotification', (notification) => {
      setNotifications(prev => [notification, ...prev])
    })

    newConnection.start()
      .then(() => console.log('Connected to NotificationHub'))
      .catch(err => console.error('Connection error:', err))

    setConnection(newConnection)

    return () => {
      newConnection.stop()
    }
  }, [])

  const markAsRead = async (notificationId) => {
    if (connection) {
      await connection.invoke('MarkAsRead', notificationId)
    }
  }

  return { notifications, markAsRead }
}
```

---

## Testing

### 1. Test Background Workers

Send a test notification via API:

```bash
curl -X POST https://localhost:5001/api/v1/notifications \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientId": "user-guid",
    "type": "BookingConfirmation",
    "channel": "Email",
    "subject": "Test Notification",
    "body": "This is a test"
  }'
```

Check logs to verify:
```
info: NotificationProcessorService[0] Processing 1 queued notifications
info: NotificationProcessorService[0] Sent notification {guid} via Email to user {user-guid}
```

### 2. Test Scheduled Notifications

Schedule a notification for 2 minutes from now:

```bash
curl -X POST https://localhost:5001/api/v1/notifications/schedule \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientId": "user-guid",
    "type": "BookingReminder",
    "channel": "Email, SMS",
    "subject": "Scheduled Test",
    "body": "This should arrive in 2 minutes",
    "scheduledFor": "2025-11-06T14:32:00Z"
  }'
```

Wait 2 minutes and check logs for:
```
info: ScheduledNotificationService[0] Processing 1 scheduled notifications
info: ScheduledNotificationService[0] Queued scheduled notification {guid}
```

### 3. Test SignalR

Open browser console:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://localhost:5001/hubs/notifications')
  .build()

connection.on('ReceiveNotification', (notification) => {
  console.log('Received:', notification)
})

await connection.start()

// Then send a notification via API with InApp channel
// You should see it in the console immediately
```

---

## Production Deployment

### 1. Environment Configuration

Create `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db.yourdomain.com;Database=booksy;Username=prod_user;Password=SECURE_PASSWORD",
    "Redis": "prod-redis.yourdomain.com:6379"
  },
  "SendGrid": {
    "ApiKey": "SG.production-key"
  },
  "SignalR": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://app.yourdomain.com"
    ]
  },
  "Notification": {
    "ProcessorIntervalSeconds": 10,
    "BatchSize": 500
  }
}
```

### 2. Enable Redis Backplane (for multi-server)

In `ExternalServicesExtensions.cs`:

```csharp
public static IServiceCollection AddSignalRNotifications(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var redisConnection = configuration.GetConnectionString("Redis");

    if (!string.IsNullOrEmpty(redisConnection))
    {
        // Use Redis for scaling across multiple servers
        services.AddSignalR()
            .AddStackExchangeRedis(redisConnection);
    }
    else
    {
        services.AddSignalR();
    }

    return services;
}
```

### 3. HTTPS Configuration

Ensure SignalR uses HTTPS in production:

```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

### 4. Health Checks

Add health check endpoint:

```csharp
app.MapHealthChecks("/health");
```

Test:
```bash
curl https://your-domain.com/health
```

### 5. Monitoring

Add Application Insights or similar:

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

---

## Monitoring & Troubleshooting

### Common Issues

#### 1. Background Workers Not Running

**Symptoms:** Notifications stay in "Pending" status

**Solution:**
- Check logs for worker startup messages
- Verify `AddNotificationBackgroundServices()` is called in `Program.cs`
- Ensure database connection is working

#### 2. SignalR Connections Failing

**Symptoms:** Frontend can't connect to `/hubs/notifications`

**Solution:**
- Check CORS configuration
- Verify hub is mapped: `app.MapHub<NotificationHub>("/hubs/notifications")`
- Check JWT token is valid
- Test with browser dev tools Network tab

#### 3. Notifications Not Sending

**Symptoms:** Status changes to "Sent" but emails/SMS not received

**Solution:**
- Verify external service API keys (SendGrid, Rahyab, Firebase)
- Check service implementation logs
- Test services independently
- Verify recipient email/phone is valid

#### 4. High Database Load

**Symptoms:** Slow queries, timeouts

**Solution:**
- Add database indexes on frequently queried columns
- Increase `ProcessorIntervalSeconds` to reduce query frequency
- Reduce `BatchSize` for smaller batches
- Archive old notifications

### Useful Queries

**Check pending notifications:**
```sql
SELECT COUNT(*) FROM "Notifications"
WHERE "Status" IN ('Pending', 'Queued');
```

**Check failed notifications:**
```sql
SELECT * FROM "Notifications"
WHERE "Status" = 'Failed'
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

**Check delivery rates:**
```sql
SELECT
  "Status",
  COUNT(*) as Count,
  ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER(), 2) as Percentage
FROM "Notifications"
WHERE "CreatedAt" >= NOW() - INTERVAL '24 hours'
GROUP BY "Status";
```

---

## Support

For issues or questions:
1. Check logs in `Logs/` directory
2. Review this guide
3. Check SignalR Setup Guide: `SIGNALR_SETUP_GUIDE.md`
4. Open an issue on GitHub

---

## Summary

✅ **Installation Complete**
✅ **Configuration Ready**
✅ **Database Migrated**
✅ **Background Workers Running**
✅ **API Endpoints Active**
✅ **SignalR Connected**
✅ **Frontend Integrated**

**Your Notification System is Ready for Production!** 🚀
