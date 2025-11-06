# API Response Pattern Documentation

**Created:** Session 9
**Purpose:** Document the API response wrapping pattern for all integration tests

---

## Overview

All API endpoints in the Booksy application return responses wrapped in a standardized `ApiResponse<T>` structure. This wrapping is handled automatically by the `ApiResponseMiddleware`.

---

## ApiResponseMiddleware

**Location:** `src/Core/Booksy.Core.Domain/Infrastructure/Middleware/ApiResponseMiddleware.cs`

### What It Does

The middleware intercepts all API requests (paths starting with `/api/`) and:

1. **Wraps successful responses** in `ApiResponse<object>`
2. **Wraps error responses** in `ApiResponse<object>` with error details
3. **Skips wrapping** for `204 NoContent` responses
4. **Adds metadata** (request ID, timestamp, duration, path, method, version)

### Success Response Structure

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }           // Always true for success
    public int StatusCode { get; set; }         // HTTP status code (200, 201, etc.)
    public string Message { get; set; }         // Success message
    public T? Data { get; set; }                // The actual controller response
    public ResponseMetadata Metadata { get; set; }
}

public class ResponseMetadata
{
    public string RequestId { get; set; }       // TraceIdentifier
    public DateTimeOffset Timestamp { get; set; }
    public long Duration { get; set; }          // Elapsed milliseconds
    public string Path { get; set; }
    public string Method { get; set; }
    public string Version { get; set; }
}
```

### Example Response

**Controller returns:**
```csharp
public async Task<IActionResult> SendNotification(...)
{
    var result = await _mediator.Send(command);
    return Ok(result);  // Returns SendNotificationResult
}
```

**Client receives:**
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Request completed successfully",
  "data": {
    "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "success": true,
    "channel": "Email",
    "sentAt": "2025-11-06T14:30:00Z"
  },
  "metadata": {
    "requestId": "0HN2QKQJ5V2JL:00000001",
    "timestamp": "2025-11-06T14:30:00.123Z",
    "duration": 245,
    "path": "/api/v1/notifications",
    "method": "POST",
    "version": "1.0"
  }
}
```

### Error Response Structure

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }           // Always false for errors
    public int StatusCode { get; set; }         // HTTP status code (400, 404, 500, etc.)
    public string Message { get; set; }         // Error message
    public ErrorResponse? Error { get; set; }   // Detailed error info
    public ResponseMetadata Metadata { get; set; }
}

public class ErrorResponse
{
    public string Code { get; set; }            // Error code (e.g., "VALIDATION_ERROR")
    public string Message { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
    public string? StackTrace { get; set; }     // Only in development
}
```

---

## Integration Test Pattern

### Base Class Helper Methods

**Location:** `tests/Booksy.Tests.Commons/IntegrationTestBase.cs`

The base class provides helper methods that automatically deserialize to `ApiResponse<T>`:

```csharp
// POST with response type
protected async Task<ApiResponse<TResponse>> PostAsJsonAsync<TRequest, TResponse>(string url, TRequest data)

// POST without response type (returns non-generic ApiResponse)
protected async Task<ApiResponse> PostAsJsonAsync<TRequest>(string url, TRequest data)

// PUT with response type
protected async Task<ApiResponse<TResponse>> PutAsJsonAsync<TRequest, TResponse>(string url, TRequest data)

// PUT without response type
protected async Task<ApiResponse> PutAsJsonAsync<TRequest>(string url, TRequest data)

// GET with response type
protected async Task<ApiResponse<TResponse>> GetAsync<TResponse>(string url)

// GET without deserialization (returns raw HttpResponseMessage)
protected async Task<HttpResponseMessage> GetAsync(string url)

// DELETE
protected async Task<ApiResponse> DeleteAsync(string url)
```

### Test Pattern Example

```csharp
[Fact]
public async Task SendNotification_WithValidEmailData_ShouldReturn200OK()
{
    // Arrange
    var userId = Guid.NewGuid();
    AuthenticateAsUser(userId, "user@test.com");

    var request = new SendNotificationRequest(
        RecipientId: userId,
        Type: NotificationType.BookingConfirmation,
        Channel: NotificationChannel.Email,
        Subject: "Booking Confirmed",
        Body: "<p>Your booking has been confirmed</p>",
        RecipientEmail: "user@test.com");

    // Act
    var response = await PostAsJsonAsync<SendNotificationRequest, SendNotificationResult>(
        "/api/v1/notifications", request);

    // Assert
    // ✅ Access wrapper properties
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Success.Should().BeTrue();
    response.Message.Should().Be("Request completed successfully");

    // ✅ Access the actual data
    response.Data.Should().NotBeNull();
    response.Data!.NotificationId.Should().NotBeEmpty();
    response.Data.Success.Should().BeTrue();
    response.Data.Channel.Should().Be(NotificationChannel.Email);

    // ✅ Access metadata
    response.Metadata.Should().NotBeNull();
    response.Metadata.RequestId.Should().NotBeNullOrEmpty();
    response.Metadata.Duration.Should().BeGreaterThan(0);

    // ✅ Verify database state
    var notification = await DbContext.Notifications
        .FirstOrDefaultAsync(n => n.Id == NotificationId.From(response.Data.NotificationId));
    notification.Should().NotBeNull();
}
```

---

## Common Test Scenarios

### 1. Testing Successful POST (201 Created)

```csharp
var response = await PostAsJsonAsync<CreateBookingRequest, BookingResponse>(
    "/api/v1/bookings", request);

response.StatusCode.Should().Be(HttpStatusCode.Created);
response.Data.Should().NotBeNull();
response.Data!.Id.Should().NotBeEmpty();
```

### 2. Testing Successful GET (200 OK)

```csharp
var response = await GetAsync<NotificationHistoryViewModel>("/api/v1/notifications/history");

response.StatusCode.Should().Be(HttpStatusCode.OK);
response.Data.Should().NotBeNull();
response.Data!.Notifications.Should().HaveCountGreaterOrEqualTo(5);
```

### 3. Testing Validation Error (400 BadRequest)

```csharp
var response = await PostAsJsonAsync("/api/v1/notifications", invalidRequest);

response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
response.Success.Should().BeFalse();
response.Error.Should().NotBeNull();
response.Error!.Code.Should().Be("VALIDATION_ERROR");
response.Error.ValidationErrors.Should().ContainKey("Subject");
```

### 4. Testing Unauthorized (401)

```csharp
ClearAuthenticationHeader();

var response = await GetAsync("/api/v1/notifications/history");

response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
response.Success.Should().BeFalse();
```

### 5. Testing Not Found (404)

```csharp
var nonExistentId = Guid.NewGuid();
var response = await GetAsync($"/api/v1/notifications/{nonExistentId}/status");

response.StatusCode.Should().Be(HttpStatusCode.NotFound);
response.Success.Should().BeFalse();
response.Error.Should().NotBeNull();
response.Error!.Code.Should().Be("NOT_FOUND");
```

### 6. Testing Forbidden (403)

```csharp
AuthenticateAsUser(userId, "user@test.com");

var response = await PostAsJsonAsync("/api/v1/notifications/bulk", bulkRequest);

response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
response.Success.Should().BeFalse();
```

---

## Important Notes

### ✅ DO's

1. **Always use the generic helper methods** when expecting typed data:
   ```csharp
   var response = await PostAsJsonAsync<Request, Response>(...);
   ```

2. **Access data via `response.Data`**:
   ```csharp
   response.Data.Should().NotBeNull();
   response.Data!.Id.Should().NotBeEmpty();
   ```

3. **Check `response.StatusCode`** for HTTP status:
   ```csharp
   response.StatusCode.Should().Be(HttpStatusCode.OK);
   ```

4. **Check `response.Success`** for success/failure:
   ```csharp
   response.Success.Should().BeTrue();
   ```

5. **Access errors via `response.Error`**:
   ```csharp
   response.Error.Should().NotBeNull();
   response.Error!.Code.Should().Be("VALIDATION_ERROR");
   ```

### ❌ DON'Ts

1. **Don't expect raw controller response types**:
   ```csharp
   // ❌ WRONG
   var response = await PostAsJsonAsync<Request, Response>(...);
   response.Id.Should().NotBeEmpty();  // Response doesn't have Id

   // ✅ CORRECT
   response.Data!.Id.Should().NotBeEmpty();  // Access via Data property
   ```

2. **Don't use HttpResponseMessage for typed responses**:
   ```csharp
   // ❌ WRONG - Requires manual deserialization
   var httpResponse = await GetAsync("/api/v1/notifications/history");

   // ✅ CORRECT - Automatic deserialization
   var response = await GetAsync<NotificationHistoryViewModel>("/api/v1/notifications/history");
   ```

3. **Don't forget null checks**:
   ```csharp
   // ❌ WRONG - Can throw NullReferenceException
   response.Data.Id.Should().NotBeEmpty();

   // ✅ CORRECT - Null check first
   response.Data.Should().NotBeNull();
   response.Data!.Id.Should().NotBeEmpty();
   ```

---

## Status Code Messages

The middleware automatically sets messages based on status codes:

| Status Code | Message |
|-------------|---------|
| 200 | "Request completed successfully" |
| 201 | "Resource created successfully" |
| 202 | "Request accepted for processing" |
| 204 | "Request completed with no content" (not wrapped) |
| Other | "Request processed" |

---

## Examples from Existing Tests

### BookingsControllerTests (✅ Correct Pattern)

```csharp
var response = await PostAsJsonAsync<CreateBookingRequest, BookingResponse>(
    "/api/v1/bookings", request);

response.StatusCode.Should().Be(HttpStatusCode.Created);
response.Data.Should().NotBeNull();
response.Data!.Id.Should().NotBeEmpty();
response.Data.CustomerId.Should().Be(customerId);
```

### NotificationsControllerTests (✅ Correct Pattern)

```csharp
var response = await PostAsJsonAsync<SendNotificationRequest, SendNotificationResult>(
    "/api/v1/notifications", request);

response.StatusCode.Should().Be(HttpStatusCode.OK);
response.Data.Should().NotBeNull();
response.Data!.NotificationId.Should().NotBeEmpty();
response.Data.Success.Should().BeTrue();
```

### PaymentsControllerTests (✅ Correct Pattern)

```csharp
var response = await PostAsJsonAsync<ProcessPaymentRequest, PaymentResponse>(
    "/api/v1/payments", request);

response.StatusCode.Should().Be(HttpStatusCode.OK);
response.Data.Should().NotBeNull();
response.Data!.PaymentId.Should().NotBeEmpty();
```

---

## Summary

### Key Takeaways

1. **All API responses are wrapped** in `ApiResponse<T>` by middleware
2. **Access controller data** via `response.Data`
3. **Check status** via `response.StatusCode` and `response.Success`
4. **Access errors** via `response.Error`
5. **Use helper methods** from `IntegrationTestBase` for automatic deserialization
6. **Follow existing test patterns** (BookingsControllerTests, PaymentsControllerTests)

### Quick Reference

```csharp
// Successful response
response.Success                  // true
response.StatusCode              // HttpStatusCode.OK (200)
response.Message                 // "Request completed successfully"
response.Data                    // Your controller's return value
response.Metadata.RequestId      // Trace identifier
response.Metadata.Duration       // Request duration in ms

// Error response
response.Success                 // false
response.StatusCode              // HttpStatusCode.BadRequest (400)
response.Message                 // Error message
response.Error.Code              // "VALIDATION_ERROR"
response.Error.ValidationErrors  // Dictionary of validation errors
```

---

## Conclusion

The `ApiResponseMiddleware` provides a consistent, standardized response format across all API endpoints. Integration tests must account for this wrapping by:

1. Using the `ApiResponse<T>` return type
2. Accessing data via the `.Data` property
3. Following the established test patterns

**All NotificationsControllerTests are already correctly implemented** following this pattern. No changes are needed.

---

**Document Status:** ✅ Complete and Ready for Reference
**Last Updated:** Session 9
