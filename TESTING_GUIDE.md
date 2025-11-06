# Testing Guide - Notification & Communication System

## Overview
This guide outlines the comprehensive testing strategy for the Notification & Communication System. Tests should be created in the appropriate test projects following the existing patterns.

---

## Test Project Structure

```
tests/
├── Booksy.ServiceCatalog.UnitTests/
│   ├── Domain/
│   │   └── NotificationAggregateTests.cs
│   ├── Application/
│   │   ├── Commands/
│   │   │   ├── SendNotificationCommandHandlerTests.cs
│   │   │   ├── SendBulkNotificationCommandHandlerTests.cs
│   │   │   └── ScheduleNotificationCommandHandlerTests.cs
│   │   └── Queries/
│   │       └── GetUserNotificationsQueryHandlerTests.cs
│   └── Infrastructure/
│       ├── Services/
│       │   ├── EmailNotificationServiceTests.cs
│       │   ├── SmsNotificationServiceTests.cs
│       │   ├── PushNotificationServiceTests.cs
│       │   └── InAppNotificationServiceTests.cs
│       └── BackgroundJobs/
│           └── ProcessScheduledNotificationsJobTests.cs
│
└── Booksy.ServiceCatalog.IntegrationTests/
    ├── Controllers/
    │   ├── NotificationsControllerTests.cs
    │   ├── NotificationPreferencesControllerTests.cs
    │   └── PaymentsControllerTests.cs
    └── Scenarios/
        ├── NotificationDeliveryScenarioTests.cs
        └── IdempotencyScenarioTests.cs
```

---

## 1. Domain Tests

### NotificationAggregateTests.cs

**Purpose:** Test the Notification aggregate root business logic

```csharp
public class NotificationAggregateTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateNotification()
    {
        // Arrange
        var userId = UserId.From(Guid.NewGuid());
        var type = NotificationType.System;
        var channel = NotificationChannel.Email;

        // Act
        var notification = Notification.Create(
            userId, type, channel,
            "user@example.com", "Subject", "Content");

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(NotificationStatus.Queued, notification.Status);
        Assert.Single(notification.DomainEvents); // NotificationCreatedEvent
    }

    [Fact]
    public void MarkAsDelivered_WhenQueued_ShouldUpdateStatus()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        notification.MarkAsDelivered("msg-123");

        // Assert
        Assert.Equal(NotificationStatus.Delivered, notification.Status);
        Assert.NotNull(notification.DeliveredAt);
        Assert.Equal("msg-123", notification.ExternalMessageId);
    }

    [Fact]
    public void MarkAsDelivered_WhenAlreadyDelivered_ShouldThrowException()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsDelivered("msg-123");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            notification.MarkAsDelivered("msg-456"));
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndErrorMessage()
    {
        // Arrange
        var notification = CreateTestNotification();
        var errorMessage = "SMTP connection failed";

        // Act
        notification.MarkAsFailed(errorMessage);

        // Assert
        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal(errorMessage, notification.ErrorMessage);
        Assert.NotNull(notification.FailedAt);
    }
}
```

**Test Cases:**
- ✅ Create notification with valid data
- ✅ Create notification with scheduled time
- ✅ MarkAsDelivered updates status correctly
- ✅ MarkAsDelivered throws when already delivered
- ✅ MarkAsFailed updates status and error
- ✅ IsExpired returns true when past expiry
- ✅ Domain events are raised correctly
- ✅ Validation throws for invalid data (null recipient, etc.)

---

## 2. Application Layer Tests

### SendNotificationCommandHandlerTests.cs

**Purpose:** Test the SendNotification command handler

```csharp
public class SendNotificationCommandHandlerTests
{
    private readonly Mock<INotificationWriteRepository> _writeRepoMock;
    private readonly Mock<IEmailNotificationService> _emailServiceMock;
    private readonly Mock<INotificationTemplateRepository> _templateRepoMock;
    private readonly SendNotificationCommandHandler _handler;

    public SendNotificationCommandHandlerTests()
    {
        _writeRepoMock = new Mock<INotificationWriteRepository>();
        _emailServiceMock = new Mock<IEmailNotificationService>();
        _templateRepoMock = new Mock<INotificationTemplateRepository>();

        _handler = new SendNotificationCommandHandler(
            _writeRepoMock.Object,
            _emailServiceMock.Object,
            _templateRepoMock.Object,
            /* other services */);
    }

    [Fact]
    public async Task Handle_ValidEmailNotification_ShouldSendAndSave()
    {
        // Arrange
        var command = new SendNotificationCommand(
            UserId: Guid.NewGuid(),
            Type: NotificationType.System,
            Channel: NotificationChannel.Email,
            Recipient: "user@example.com",
            Subject: "Test",
            Content: "Test content",
            TemplateId: null,
            ScheduledFor: null,
            Data: null,
            IdempotencyKey: Guid.NewGuid());

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "msg-123", null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("msg-123", result.MessageId);

        _writeRepoMock.Verify(x => x.AddNotificationAsync(
            It.Is<Notification>(n => n.Status == NotificationStatus.Delivered),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceFailure_ShouldMarkAsFailed()
    {
        // Arrange
        var command = CreateTestCommand();

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "SMTP error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("SMTP error", result.ErrorMessage);

        _writeRepoMock.Verify(x => x.AddNotificationAsync(
            It.Is<Notification>(n => n.Status == NotificationStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTemplate_ShouldRenderTemplate()
    {
        // Test template variable substitution
    }
}
```

**Test Cases:**
- ✅ Send email notification successfully
- ✅ Send SMS notification successfully
- ✅ Send push notification successfully
- ✅ Send in-app notification successfully
- ✅ Handle service failures gracefully
- ✅ Use template when TemplateId provided
- ✅ Render template variables correctly
- ✅ Save notification to repository
- ✅ Throw NotFoundException when template not found

---

### ScheduleNotificationCommandHandlerTests.cs

**Test Cases:**
- ✅ Schedule notification for future time
- ✅ Validate scheduled time is in future
- ✅ Throw validation error for past time
- ✅ Save with Queued status

---

### SendBulkNotificationCommandHandlerTests.cs

**Test Cases:**
- ✅ Send to multiple recipients
- ✅ Respect rate limiting (max 1000 recipients)
- ✅ Handle partial failures
- ✅ Create separate notification for each recipient
- ✅ Throw validation error when exceeds limit

---

## 3. Infrastructure Tests

### EmailNotificationServiceTests.cs

```csharp
public class EmailNotificationServiceTests
{
    private readonly Mock<ISendGridClient> _sendGridMock;
    private readonly Mock<ILogger<EmailNotificationService>> _loggerMock;
    private readonly EmailNotificationService _service;

    [Fact]
    public async Task SendEmailAsync_ValidEmail_ShouldCallSendGrid()
    {
        // Arrange
        var response = new Response(HttpStatusCode.OK, null, null);
        _sendGridMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.SendEmailAsync(
            "test@example.com",
            "Subject",
            "<html>Content</html>",
            "Content",
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.MessageId);

        _sendGridMock.Verify(x => x.SendEmailAsync(
            It.Is<SendGridMessage>(m =>
                m.Personalizations[0].Tos[0].Email == "test@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_SendGridFailure_ShouldReturnError()
    {
        // Test error handling
    }
}
```

**Test Cases:**
- ✅ Send email via SendGrid successfully
- ✅ Handle SendGrid API errors
- ✅ Handle network errors
- ✅ Retry on transient failures
- ✅ Log all operations

---

### SmsNotificationServiceTests.cs

**Test Cases:**
- ✅ Send SMS via Rahyab successfully
- ✅ Handle API authentication errors
- ✅ Handle rate limiting from provider
- ✅ Validate phone number format
- ✅ Retry on transient failures

---

### PushNotificationServiceTests.cs

**Test Cases:**
- ✅ Send push via Firebase successfully
- ✅ Handle invalid device tokens
- ✅ Handle FCM errors
- ✅ Support Android and iOS platforms
- ✅ Include custom data payload

---

### InAppNotificationServiceTests.cs

**Test Cases:**
- ✅ Send to user via SignalR
- ✅ Send to group of users
- ✅ Handle disconnected users
- ✅ Log delivery attempts

---

### ProcessScheduledNotificationsJobTests.cs

```csharp
public class ProcessScheduledNotificationsJobTests
{
    [Fact]
    public async Task ExecuteAsync_WithDueNotifications_ShouldProcessAll()
    {
        // Arrange
        var dueNotifications = new List<Notification>
        {
            CreateScheduledNotification(DateTime.UtcNow.AddMinutes(-5)),
            CreateScheduledNotification(DateTime.UtcNow.AddMinutes(-2))
        };

        _readRepoMock
            .Setup(x => x.GetScheduledNotificationsDueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dueNotifications);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "msg-123", null));

        // Act
        await _job.ExecuteAsync(CancellationToken.None);

        // Assert
        _writeRepoMock.Verify(x => x.UpdateNotificationAsync(
            It.Is<Notification>(n => n.Status == NotificationStatus.Delivered),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_ServiceFailure_ShouldMarkAsFailed()
    {
        // Test failure handling
    }
}
```

**Test Cases:**
- ✅ Process all due notifications
- ✅ Send via correct channel
- ✅ Mark as delivered on success
- ✅ Mark as failed on error
- ✅ Handle exceptions gracefully
- ✅ Respect batch size limit (100)
- ✅ Log all operations

---

## 4. Integration Tests

### NotificationsControllerTests.cs

```csharp
public class NotificationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public NotificationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SendNotification_ValidRequest_Returns201Created()
    {
        // Arrange
        var request = new
        {
            userId = Guid.NewGuid(),
            type = "System",
            channel = "Email",
            recipient = "test@example.com",
            subject = "Test",
            content = "Test content"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/notifications/send",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SendNotificationResult>();
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task SendNotification_InvalidChannel_Returns400BadRequest()
    {
        // Test validation error handling
    }

    [Fact]
    public async Task SendNotification_WithIdempotencyKey_ReturnsCachedResult()
    {
        // Test idempotency behavior
    }
}
```

**Test Cases:**
- ✅ Send notification returns 201 Created
- ✅ Invalid request returns 400 Bad Request
- ✅ Missing required fields returns validation errors
- ✅ Unauthorized request returns 401
- ✅ Idempotency key prevents duplicates
- ✅ Get notifications returns paginated list
- ✅ Mark as read updates status
- ✅ Delete notification soft-deletes record

---

### IdempotencyScenarioTests.cs

**Purpose:** Test idempotency across the entire stack

```csharp
[Fact]
public async Task SendSameCommandTwice_WithIdempotencyKey_ReturnsS ameResult()
{
    // Arrange
    var idempotencyKey = Guid.NewGuid();
    var request = CreateTestRequest(idempotencyKey);

    // Act
    var response1 = await _client.PostAsJsonAsync("/api/v1/notifications/send", request);
    var response2 = await _client.PostAsJsonAsync("/api/v1/notifications/send", request);

    // Assert
    var result1 = await response1.Content.ReadFromJsonAsync<SendNotificationResult>();
    var result2 = await response2.Content.ReadFromJsonAsync<SendNotificationResult>();

    Assert.Equal(result1.NotificationId, result2.NotificationId);
    Assert.Equal(result1.MessageId, result2.MessageId);

    // Verify only one email was sent
    _emailServiceMock.Verify(x => x.SendEmailAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

---

### NotificationDeliveryScenarioTests.cs

**Test Cases:**
- ✅ End-to-end email delivery
- ✅ End-to-end SMS delivery
- ✅ End-to-end push notification
- ✅ End-to-end in-app notification
- ✅ Scheduled notification processing
- ✅ Bulk notification delivery
- ✅ Template rendering and delivery

---

## 5. Test Data Builders

Create test data builders for complex objects:

```csharp
public class NotificationBuilder
{
    private UserId _userId = UserId.From(Guid.NewGuid());
    private NotificationType _type = NotificationType.System;
    private NotificationChannel _channel = NotificationChannel.Email;
    private string _recipient = "test@example.com";
    private string _subject = "Test Subject";
    private string _content = "Test Content";

    public NotificationBuilder WithUserId(UserId userId)
    {
        _userId = userId;
        return this;
    }

    public NotificationBuilder WithChannel(NotificationChannel channel)
    {
        _channel = channel;
        return this;
    }

    public Notification Build()
    {
        return Notification.Create(
            _userId, _type, _channel,
            _recipient, _subject, _content);
    }
}

// Usage:
var notification = new NotificationBuilder()
    .WithUserId(userId)
    .WithChannel(NotificationChannel.SMS)
    .Build();
```

---

## 6. Test Coverage Goals

- **Domain Layer:** 100% coverage (all business logic)
- **Application Layer:** 90%+ coverage (command/query handlers)
- **Infrastructure Layer:** 80%+ coverage (services, repositories)
- **Controllers:** 80%+ coverage (integration tests)
- **Overall:** 85%+ code coverage

---

## 7. Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test tests/Booksy.ServiceCatalog.UnitTests

# Run specific test class
dotnet test --filter "FullyQualifiedName~NotificationAggregateTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

---

## 8. Mocking Guidelines

**Use Moq for mocking:**
```csharp
var mockRepo = new Mock<INotificationRepository>();
mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<NotificationId>(), default))
    .ReturnsAsync(notification);
```

**Verify method calls:**
```csharp
mockRepo.Verify(x => x.AddNotificationAsync(
    It.Is<Notification>(n => n.Status == NotificationStatus.Queued),
    It.IsAny<CancellationToken>()), Times.Once);
```

---

## 9. Test Naming Convention

**Pattern:** `MethodName_Scenario_ExpectedResult`

**Examples:**
- `Create_WithValidData_ShouldCreateNotification`
- `SendEmailAsync_WhenServiceFails_ShouldReturnError`
- `Handle_WithNullTemplate_ShouldThrowNotFoundException`

---

## 10. Priority Test Implementation Order

1. **High Priority** (Implement First):
   - NotificationAggregateTests
   - SendNotificationCommandHandlerTests
   - ProcessScheduledNotificationsJobTests
   - NotificationsControllerTests

2. **Medium Priority**:
   - EmailNotificationServiceTests
   - SmsNotificationServiceTests
   - ScheduleNotificationCommandHandlerTests
   - IdempotencyScenarioTests

3. **Low Priority**:
   - PushNotificationServiceTests
   - InAppNotificationServiceTests
   - Other query handler tests

---

## Summary

This testing strategy ensures comprehensive coverage of the Notification & Communication System. Start with high-priority domain and application layer tests, then move to infrastructure and integration tests. Use the provided patterns and examples as templates for implementing the actual test files.
