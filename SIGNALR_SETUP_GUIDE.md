# SignalR Real-Time Notifications Setup Guide

## Overview
SignalR has been implemented for real-time in-app notifications in the Booking system. This guide explains how to configure it in your API.

## 1. Register SignalR Services

In your `Program.cs` or `Startup.cs`, add SignalR services:

```csharp
// Add SignalR for real-time notifications
builder.Services.AddSignalRNotifications(builder.Configuration);
```

## 2. Map SignalR Hub Endpoint

After `app.Build()`, map the NotificationHub endpoint:

```csharp
var app = builder.Build();

// ... other middleware configuration ...

// Map SignalR hub (BEFORE MapControllers)
app.MapHub<NotificationHub>("/hubs/notifications");

// Map controllers
app.MapControllers();

app.Run();
```

## 3. Configure CORS (if needed)

If your frontend is on a different domain, add the SignalR CORS configuration to `appsettings.json`:

```json
{
  "SignalR": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:8080",
      "https://yourdomain.com"
    ]
  }
}
```

Then enable CORS in Program.cs:

```csharp
// Use CORS policy for SignalR
app.UseCors("SignalRPolicy");

// Map SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");
```

## 4. Frontend Integration

### JavaScript/TypeScript Client

```typescript
import * as signalR from "@microsoft/signalr";

// Create connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/notifications", {
    accessTokenFactory: () => yourAuthToken
  })
  .withAutomaticReconnect()
  .build();

// Listen for notifications
connection.on("ReceiveNotification", (notification) => {
  console.log("New notification:", notification);
  // notification object:
  // {
  //   Id: "guid",
  //   Title: "string",
  //   Message: "string",
  //   Type: "BookingConfirmed|PaymentReceived|etc",
  //   Timestamp: "datetime",
  //   Metadata: { ... }
  // }
});

// Listen for read confirmations
connection.on("NotificationRead", (notificationId) => {
  console.log("Notification marked as read:", notificationId);
});

connection.on("AllNotificationsRead", () => {
  console.log("All notifications marked as read");
});

// Start connection
connection.start()
  .then(() => console.log("Connected to NotificationHub"))
  .catch(err => console.error("Connection error:", err));

// Mark notification as read
connection.invoke("MarkAsRead", notificationId);

// Mark multiple as read
connection.invoke("MarkMultipleAsRead", [id1, id2, id3]);

// Mark all as read
connection.invoke("MarkAllAsRead");
```

### Vue.js Example

```vue
<script setup>
import { onMounted, onUnmounted, ref } from 'vue'
import * as signalR from '@microsoft/signalr'

const notifications = ref([])
let connection = null

onMounted(async () => {
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/notifications', {
      accessTokenFactory: () => localStorage.getItem('authToken')
    })
    .withAutomaticReconnect()
    .build()

  connection.on('ReceiveNotification', (notification) => {
    notifications.value.unshift(notification)
  })

  await connection.start()
})

onUnmounted(async () => {
  if (connection) {
    await connection.stop()
  }
})

const markAsRead = async (notificationId) => {
  await connection.invoke('MarkAsRead', notificationId)
}
</script>
```

## 5. Authentication

The NotificationHub automatically extracts the user ID from JWT claims:
- `sub` claim (standard)
- `userId` claim (custom)

Make sure your JWT includes one of these claims for proper user targeting.

## 6. Testing

### Test Connection
```bash
# Install SignalR client globally
npm install -g @microsoft/signalr

# Create test script
node test-signalr.js
```

```javascript
// test-signalr.js
const signalR = require('@microsoft/signalr');

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/hubs/notifications')
  .build();

connection.on('ReceiveNotification', (notification) => {
  console.log('Notification received:', notification);
});

connection.start()
  .then(() => console.log('Connected!'))
  .catch(err => console.error(err));
```

## 7. Notification Types

The following notification types are automatically sent:

### Booking Notifications
- `BookingConfirmation` - When booking is confirmed
- `BookingCancellation` - When booking is cancelled
- `BookingRescheduled` - When booking is rescheduled
- `BookingNoShow` - When customer doesn't show up
- `ReviewRequest` - Request for review after booking completion

### Payment Notifications
- `PaymentReceived` - Payment successful
- `PaymentFailed` - Payment failed
- `RefundProcessed` - Refund completed
- `PayoutCompleted` - Provider payout completed

## 8. Monitoring

Add logging to monitor SignalR connections:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Information);
    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Information);
});
```

## 9. Scaling (Optional)

For multi-server deployments, use Redis backplane:

```bash
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
```

```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(configuration.GetConnectionString("Redis"));
```

## 10. Security Best Practices

1. **Always use HTTPS in production**
2. **Validate JWT tokens** - The hub automatically uses ASP.NET Core authentication
3. **Limit connection rate** - Prevent abuse
4. **Monitor connection count** - Track active connections
5. **Use message size limits** - Already configured (1MB max)

## Troubleshooting

### Connection Fails
- Check CORS configuration
- Verify JWT token is valid
- Check SignalR endpoint URL

### Notifications Not Received
- Verify user is connected (`OnConnectedAsync` logs)
- Check user ID extraction from JWT
- Verify notification is being sent via `IInAppNotificationService`

### Multiple Connections
- This is normal - users can have multiple tabs/devices
- All connections receive notifications via user groups
