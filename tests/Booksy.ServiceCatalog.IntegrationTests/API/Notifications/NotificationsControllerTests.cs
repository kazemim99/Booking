using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Commands.Notifications.CancelNotification;
using Booksy.ServiceCatalog.Application.Commands.Notifications.ResendNotification;
using Booksy.ServiceCatalog.Application.Commands.Notifications.ScheduleNotification;
using Booksy.ServiceCatalog.Application.Commands.Notifications.SendBulkNotification;
using Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification;
using Booksy.ServiceCatalog.Application.Queries.Notifications.GetDeliveryStatus;
using Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationAnalytics;
using Booksy.ServiceCatalog.Application.Queries.Notifications.GetNotificationHistory;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Notifications;

/// <summary>
/// Integration tests for Notifications API endpoints
/// Covers: Notification operations (send, schedule, bulk, cancel, resend, history, status, analytics)
/// Endpoints: /api/v1/notifications/*
/// </summary>
public class NotificationsControllerTests : ServiceCatalogIntegrationTestBase
{
    public NotificationsControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
    {
    }

    #region Send Notification Tests

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
            Priority: NotificationPriority.High,
            PlainTextBody: "Your booking has been confirmed",
            RecipientEmail: "user@test.com",
            RecipientPhone: null);

        // Act
        var response = await PostAsJsonAsync<SendNotificationRequest, SendNotificationResult>(
            "/api/v1/notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.NotificationId.Should().NotBeEmpty();
        response.Data.Success.Should().BeTrue();
        response.Data.Channel.Should().Be(NotificationChannel.Email);

        // Verify notification exists in database
        var notification = await DbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == NotificationId.From(response.Data.NotificationId));
        notification.Should().NotBeNull();
        notification!.RecipientId.Value.Should().Be(userId);
        notification.Channel.Should().Be(NotificationChannel.Email);
        notification.Type.Should().Be(NotificationType.BookingConfirmation);
    }

    [Fact]
    public async Task SendNotification_WithValidSmsData_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var request = new SendNotificationRequest(
            RecipientId: userId,
            Type: NotificationType.BookingReminder,
            Channel: NotificationChannel.SMS,
            Subject: "Reminder",
            Body: "Your appointment is tomorrow at 10 AM",
            Priority: NotificationPriority.Normal,
            PlainTextBody: "Your appointment is tomorrow at 10 AM",
            RecipientEmail: null,
            RecipientPhone: "+1234567890");

        // Act
        var response = await PostAsJsonAsync<SendNotificationRequest, SendNotificationResult>(
            "/api/v1/notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Success.Should().BeTrue();
        response.Data.Channel.Should().Be(NotificationChannel.SMS);

        // Verify notification exists in database
        var notification = await DbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == NotificationId.From(response.Data.NotificationId));
        notification.Should().NotBeNull();
        notification!.RecipientPhone.Should().Be("+1234567890");
    }

    [Fact]
    public async Task SendNotification_WithInAppChannel_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var request = new SendNotificationRequest(
            RecipientId: userId,
            Type: NotificationType.SystemAlert,
            Channel: NotificationChannel.InApp,
            Subject: "New Feature Available",
            Body: "Check out our new feature!",
            Priority: NotificationPriority.Low,
            PlainTextBody: null,
            RecipientEmail: null,
            RecipientPhone: null);

        // Act
        var response = await PostAsJsonAsync<SendNotificationRequest, SendNotificationResult>(
            "/api/v1/notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Success.Should().BeTrue();
        response.Data.Channel.Should().Be(NotificationChannel.InApp);
    }

    [Fact]
    public async Task SendNotification_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        ClearAuthenticationHeader();

        var request = new SendNotificationRequest(
            RecipientId: Guid.NewGuid(),
            Type: NotificationType.BookingConfirmation,
            Channel: NotificationChannel.Email,
            Subject: "Test",
            Body: "Test body",
            RecipientEmail: "test@test.com");

        // Act
        var response = await PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendNotification_WithEmptySubject_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var request = new SendNotificationRequest(
            RecipientId: userId,
            Type: NotificationType.BookingConfirmation,
            Channel: NotificationChannel.Email,
            Subject: "", // Empty subject
            Body: "Test body",
            RecipientEmail: "user@test.com");

        // Act
        var response = await PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Schedule Notification Tests

    [Fact]
    public async Task ScheduleNotification_WithValidData_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var scheduledFor = DateTime.UtcNow.AddHours(24); // Schedule for tomorrow
        var request = new ScheduleNotificationRequest(
            RecipientId: userId,
            Type: NotificationType.BookingReminder,
            Channel: NotificationChannel.Email,
            Subject: "Reminder",
            Body: "Don't forget your appointment tomorrow",
            ScheduledFor: scheduledFor,
            Priority: NotificationPriority.Normal,
            PlainTextBody: "Don't forget your appointment tomorrow",
            RecipientEmail: "user@test.com",
            RecipientPhone: null);

        // Act
        var response = await PostAsJsonAsync<ScheduleNotificationRequest, ScheduleNotificationResult>(
            "/api/v1/notifications/schedule", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.NotificationId.Should().NotBeEmpty();
        response.Data.Success.Should().BeTrue();
        response.Data.ScheduledFor.Should().BeCloseTo(scheduledFor, TimeSpan.FromSeconds(1));

        // Verify notification exists in database with Queued status
        var notification = await DbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == NotificationId.From(response.Data.NotificationId));
        notification.Should().NotBeNull();
        notification!.Status.Should().Be(NotificationStatus.Queued);
        notification.ScheduledFor.Should().NotBeNull();
        notification.ScheduledFor!.Value.Should().BeCloseTo(scheduledFor, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ScheduleNotification_WithPastDate_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var pastDate = DateTime.UtcNow.AddHours(-1); // Past date
        var request = new ScheduleNotificationRequest(
            RecipientId: userId,
            Type: NotificationType.BookingReminder,
            Channel: NotificationChannel.Email,
            Subject: "Test",
            Body: "Test body",
            ScheduledFor: pastDate,
            RecipientEmail: "user@test.com");

        // Act
        var response = await PostAsJsonAsync("/api/v1/notifications/schedule", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Bulk Notification Tests

    [Fact]
    public async Task SendBulkNotification_AsAdmin_ShouldReturn200OK()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var recipientIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        var request = new SendBulkNotificationRequest(
            RecipientIds: recipientIds,
            Type: NotificationType.Promotions,
            Channel: NotificationChannel.Email,
            Subject: "Special Offer",
            Body: "Get 20% off your next booking",
            Priority: NotificationPriority.Low,
            PlainTextBody: "Get 20% off your next booking",
            CampaignId: "PROMO2025");

        // Act
        var response = await PostAsJsonAsync<SendBulkNotificationRequest, SendBulkNotificationResult>(
            "/api/v1/notifications/bulk", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.TotalCount.Should().Be(3);
        response.Data.SuccessCount.Should().BeGreaterThan(0);
        response.Data.NotificationIds.Should().HaveCount(3);

        // Verify notifications exist in database
        var notifications = await DbContext.Notifications
            .Where(n => recipientIds.Contains(n.RecipientId.Value))
            .ToListAsync();
        notifications.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task SendBulkNotification_AsProvider_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        AuthenticateAsProviderOwner(provider);

        var recipientIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var request = new SendBulkNotificationRequest(
            RecipientIds: recipientIds,
            Type: NotificationType.Promotions,
            Channel: NotificationChannel.Email,
            Subject: "New Services Available",
            Body: "Check out our new services",
            Priority: NotificationPriority.Normal,
            PlainTextBody: "Check out our new services");

        // Act
        var response = await PostAsJsonAsync<SendBulkNotificationRequest, SendBulkNotificationResult>(
            "/api/v1/notifications/bulk", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task SendBulkNotification_AsRegularUser_ShouldReturn403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var request = new SendBulkNotificationRequest(
            RecipientIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            Type: NotificationType.Promotions,
            Channel: NotificationChannel.Email,
            Subject: "Test",
            Body: "Test body");

        // Act
        var response = await PostAsJsonAsync("/api/v1/notifications/bulk", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SendBulkNotification_WithEmptyRecipientList_ShouldReturn400BadRequest()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var request = new SendBulkNotificationRequest(
            RecipientIds: new List<Guid>(), // Empty list
            Type: NotificationType.Promotions,
            Channel: NotificationChannel.Email,
            Subject: "Test",
            Body: "Test body");

        // Act
        var response = await PostAsJsonAsync("/api/v1/notifications/bulk", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Cancel Notification Tests

    [Fact]
    public async Task CancelNotification_WithValidQueuedNotification_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        // Create a scheduled notification
        var notification = Notification.Schedule(
            UserId.From(userId),
            NotificationType.BookingReminder,
            NotificationChannel.Email,
            "Reminder",
            "Your appointment is tomorrow",
            DateTime.UtcNow.AddHours(24),
            NotificationPriority.Normal,
            recipientEmail: "user@test.com");

        await CreateEntityAsync(notification);

        var cancelRequest = new CancelNotificationRequest(Reason: "User requested cancellation");

        // Act
        var response = await PostAsJsonAsync<CancelNotificationRequest, CancelNotificationResult>(
            $"/api/v1/notifications/{notification.Id.Value}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Success.Should().BeTrue();
        response.Data.Message.Should().Contain("cancelled");

        // Verify notification status in database
        var cancelledNotification = await DbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notification.Id);
        cancelledNotification.Should().NotBeNull();
        cancelledNotification!.Status.Should().Be(NotificationStatus.Cancelled);
    }

    [Fact]
    public async Task CancelNotification_WithNonExistentNotification_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var nonExistentId = Guid.NewGuid();
        var cancelRequest = new CancelNotificationRequest(Reason: "Test");

        // Act
        var response = await PostAsJsonAsync($"/api/v1/notifications/{nonExistentId}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelNotification_WithAlreadySentNotification_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        // Create a notification and mark it as sent
        var notification = Notification.CreateImmediate(
            UserId.From(userId),
            NotificationType.BookingConfirmation,
            NotificationChannel.Email,
            "Confirmation",
            "Your booking is confirmed",
            NotificationPriority.Normal,
            recipientEmail: "user@test.com");

        notification.MarkAsSent("msg-123");
        await CreateEntityAsync(notification);

        var cancelRequest = new CancelNotificationRequest(Reason: "Test");

        // Act
        var response = await PostAsJsonAsync($"/api/v1/notifications/{notification.Id.Value}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Resend Notification Tests

    [Fact]
    public async Task ResendNotification_WithFailedNotification_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        // Create a notification and mark it as failed
        var notification = Notification.CreateImmediate(
            UserId.From(userId),
            NotificationType.BookingConfirmation,
            NotificationChannel.Email,
            "Confirmation",
            "Your booking is confirmed",
            NotificationPriority.Normal,
            recipientEmail: "user@test.com");

        notification.MarkAsFailed("SMTP connection error");
        await CreateEntityAsync(notification);

        // Act
        var response = await PostAsJsonAsync<object, ResendNotificationResult>(
            $"/api/v1/notifications/{notification.Id.Value}/resend", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResendNotification_WithNonExistentNotification_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await PostAsJsonAsync($"/api/v1/notifications/{nonExistentId}/resend", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get History Tests

    [Fact]
    public async Task GetHistory_WithNotifications_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        // Create multiple notifications for the user
        for (int i = 0; i < 5; i++)
        {
            var notification = Notification.CreateImmediate(
                UserId.From(userId),
                NotificationType.BookingConfirmation,
                NotificationChannel.Email,
                $"Subject {i}",
                $"Body {i}",
                NotificationPriority.Normal,
                recipientEmail: "user@test.com");

            await CreateEntityAsync(notification);
        }

        // Act
        var response = await GetAsync<NotificationHistoryViewModel>("/api/v1/notifications/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Notifications.Should().HaveCountGreaterThanOrEqualTo(5);
        response.Data.TotalCount.Should().BeGreaterThanOrEqualTo(5);
        response.Data.Notifications.Should().AllSatisfy(n => n.RecipientId.Should().Be(userId));
    }

    [Fact]
    public async Task GetHistory_WithChannelFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        // Create email notifications
        for (int i = 0; i < 3; i++)
        {
            var notification = Notification.CreateImmediate(
                UserId.From(userId),
                NotificationType.BookingConfirmation,
                NotificationChannel.Email,
                $"Email {i}",
                $"Body {i}",
                NotificationPriority.Normal,
                recipientEmail: "user@test.com");
            await CreateEntityAsync(notification);
        }

        // Create SMS notifications
        for (int i = 0; i < 2; i++)
        {
            var notification = Notification.CreateImmediate(
                UserId.From(userId),
                NotificationType.BookingReminder,
                NotificationChannel.SMS,
                $"SMS {i}",
                $"Body {i}",
                NotificationPriority.Normal,
                recipientPhone: "+1234567890");
            await CreateEntityAsync(notification);
        }

        // Act - Filter by Email channel
        var response = await GetAsync<NotificationHistoryViewModel>(
            $"/api/v1/notifications/history?channel={NotificationChannel.Email}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Notifications.Should().HaveCountGreaterThanOrEqualTo(3);
        response.Data.Notifications.Should().AllSatisfy(n =>
            n.Channel.Should().Be(NotificationChannel.Email.ToString()));
    }

    [Fact]
    public async Task GetHistory_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        // Create 15 notifications
        for (int i = 0; i < 15; i++)
        {
            var notification = Notification.CreateImmediate(
                UserId.From(userId),
                NotificationType.BookingConfirmation,
                NotificationChannel.Email,
                $"Subject {i}",
                $"Body {i}",
                NotificationPriority.Normal,
                recipientEmail: "user@test.com");
            await CreateEntityAsync(notification);
        }

        // Act - Get page 1 with 10 items
        var response = await GetAsync<NotificationHistoryViewModel>(
            "/api/v1/notifications/history?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Notifications.Should().HaveCount(10);
        response.Data.PageNumber.Should().Be(1);
        response.Data.PageSize.Should().Be(10);
        response.Data.TotalCount.Should().BeGreaterThanOrEqualTo(15);
    }

    [Fact]
    public async Task GetHistory_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        ClearAuthenticationHeader();

        // Act
        var response = await GetAsync("/api/v1/notifications/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Delivery Status Tests

    [Fact]
    public async Task GetDeliveryStatus_WithExistingNotification_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var notification = Notification.CreateImmediate(
            UserId.From(userId),
            NotificationType.BookingConfirmation,
            NotificationChannel.Email,
            "Confirmation",
            "Your booking is confirmed",
            NotificationPriority.Normal,
            recipientEmail: "user@test.com");

        notification.MarkAsSent("msg-12345");
        await CreateEntityAsync(notification);

        // Act
        var response = await GetAsync<DeliveryStatusViewModel>(
            $"/api/v1/notifications/{notification.Id.Value}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.NotificationId.Should().Be(notification.Id.Value);
        response.Data.Status.Should().Be(NotificationStatus.Sent.ToString());
        response.Data.Channel.Should().Be(NotificationChannel.Email.ToString());
        response.Data.AttemptCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDeliveryStatus_WithNonExistentNotification_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/notifications/{nonExistentId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Analytics Tests

    [Fact]
    public async Task GetAnalytics_WithNotifications_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        // Create notifications with various statuses
        var sentNotification = Notification.CreateImmediate(
            UserId.From(userId),
            NotificationType.BookingConfirmation,
            NotificationChannel.Email,
            "Sent",
            "Body",
            NotificationPriority.Normal,
            recipientEmail: "user@test.com");
        sentNotification.MarkAsSent("msg-1");
        await CreateEntityAsync(sentNotification);

        var failedNotification = Notification.CreateImmediate(
            UserId.From(userId),
            NotificationType.BookingReminder,
            NotificationChannel.SMS,
            "Failed",
            "Body",
            NotificationPriority.Normal,
            recipientPhone: "+1234567890");
        failedNotification.MarkAsFailed("Error");
        await CreateEntityAsync(failedNotification);

        // Act
        var response = await GetAsync<NotificationAnalyticsViewModel>("/api/v1/notifications/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.TotalNotifications.Should().BeGreaterThanOrEqualTo(2);
        response.Data.NotificationsByStatus.Should().ContainKey("Sent");
        response.Data.NotificationsByChannel.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAnalytics_WithDateRange_ShouldReturnFilteredAnalytics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "user@test.com");

        // Create notification
        var notification = Notification.CreateImmediate(
            UserId.From(userId),
            NotificationType.BookingConfirmation,
            NotificationChannel.Email,
            "Test",
            "Body",
            NotificationPriority.Normal,
            recipientEmail: "user@test.com");
        await CreateEntityAsync(notification);

        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var response = await GetAsync<NotificationAnalyticsViewModel>(
            $"/api/v1/notifications/analytics?startDate={startDate:o}&endDate={endDate:o}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.TotalNotifications.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAnalytics_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        ClearAuthenticationHeader();

        // Act
        var response = await GetAsync("/api/v1/notifications/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Find a notification by ID
    /// </summary>
    private async Task<Notification?> FindNotificationAsync(Guid notificationId)
    {
        return await DbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == NotificationId.From(notificationId));
    }

    /// <summary>
    /// Assert that a notification exists in the database
    /// </summary>
    private async Task AssertNotificationExistsAsync(Guid notificationId)
    {
        var notification = await FindNotificationAsync(notificationId);
        notification.Should().NotBeNull($"Notification with ID {notificationId} should exist");
    }

    /// <summary>
    /// Assert that a notification has a specific status
    /// </summary>
    private async Task AssertNotificationStatusAsync(Guid notificationId, NotificationStatus expectedStatus)
    {
        var notification = await FindNotificationAsync(notificationId);
        notification.Should().NotBeNull();
        notification!.Status.Should().Be(expectedStatus,
            $"Notification {notificationId} should have status {expectedStatus}");
    }

    #endregion
}
