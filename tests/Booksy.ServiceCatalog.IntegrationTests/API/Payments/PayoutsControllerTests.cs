using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Application.Queries.Payout.GetPendingPayouts;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// Integration tests for Payouts API endpoints
/// Covers: Payout creation, execution, and provider payout history
/// Endpoints: /api/v1/payouts/*
/// </summary>
public class PayoutsControllerTests : ServiceCatalogIntegrationTestBase
{
    public PayoutsControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
    {
    }

    #region Helper Methods

    private async Task<(Provider Provider, List<Payment> Payments)> CreateProviderWithPaymentsAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        var payments = new List<Payment>();

        // Create multiple paid payments for the provider
        for (int i = 0; i < 3; i++)
        {
            var booking = Booking.CreateBookingRequest(
                UserId.From(customerId),
                provider.Id,
                service.Id,
                provider.Staff.First().Id,
                DateTime.UtcNow.AddDays(-(30 - i)), // Past bookings
                service.Duration,
                service.BasePrice,
                BookingPolicy.Default,
                $"Booking {i + 1}");

            await CreateEntityAsync(booking);

            var payment = Payment.CreateForBooking(
                booking.Id,
                UserId.From(customerId),
                provider.Id,
                Money.Create(100 + (i * 50), "USD"),
                PaymentMethod.CreditCard);

            payment.ProcessCharge($"pi_test_{i}", "pm_test_card");
            payments.Add(payment);
            await CreateEntityAsync(payment);
        }

        return (provider, payments);
    }

    #endregion

    #region Create Payout Tests

    [Fact]
    public async Task CreatePayout_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        AuthenticateAsTestAdmin(); // Requires Admin/Finance role

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        var request = new CreatePayoutRequest
        {
            ProviderId = provider.Id.Value,
            PeriodStart = DateTime.UtcNow.AddDays(-31),
            PeriodEnd = DateTime.UtcNow.AddDays(-1),
            CommissionPercentage = 15m,
            Notes = "Monthly payout"
        };

        // Act
        var response = await PostAsJsonAsync<CreatePayoutRequest, PayoutResponse>(
            "/api/v1/payouts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.PayoutId.Should().NotBeEmpty();
        response.Data.ProviderId.Should().Be(provider.Id.Value);
        response.Data.Status.Should().Be("Pending");
        response.Data.GrossAmount.Should().BeGreaterThan(0);
        response.Data.CommissionAmount.Should().BeGreaterThan(0);
        response.Data.NetAmount.Should().BeGreaterThan(0);
        response.Data.PaymentCount.Should().Be(3);

        // Verify payout exists in database
        var payout = await DbContext.Set<Payout>()
            .FirstOrDefaultAsync(p => p.Id == PayoutId.From(response.Data.PayoutId));
        payout.Should().NotBeNull();
        payout!.Status.Should().Be(PayoutStatus.Pending);
    }

    [Fact]
    public async Task CreatePayout_WithCustomCommission_ShouldCalculateCorrectly()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        var request = new CreatePayoutRequest
        {
            ProviderId = provider.Id.Value,
            PeriodStart = DateTime.UtcNow.AddDays(-31),
            PeriodEnd = DateTime.UtcNow.AddDays(-1),
            CommissionPercentage = 20m, // Custom commission rate
            Notes = "Custom commission payout"
        };

        // Act
        var response = await PostAsJsonAsync<CreatePayoutRequest, PayoutResponse>(
            "/api/v1/payouts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();

        // Verify commission calculation
        var grossAmount = response.Data!.GrossAmount;
        var expectedCommission = grossAmount * 0.20m;
        response.Data.CommissionAmount.Should().BeApproximately(expectedCommission, 0.01m);
        response.Data.NetAmount.Should().Be(grossAmount - response.Data.CommissionAmount);
    }

    [Fact]
    public async Task CreatePayout_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        ClearAuthenticationHeader();

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        var request = new CreatePayoutRequest
        {
            ProviderId = provider.Id.Value,
            PeriodStart = DateTime.UtcNow.AddDays(-31),
            PeriodEnd = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/payouts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePayout_WithInvalidDateRange_ShouldReturn400BadRequest()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        var request = new CreatePayoutRequest
        {
            ProviderId = provider.Id.Value,
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow.AddDays(-31), // End before start
            CommissionPercentage = 15m
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/payouts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayout_WithFuturePeriodEnd_ShouldReturn400BadRequest()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        var request = new CreatePayoutRequest
        {
            ProviderId = provider.Id.Value,
            PeriodStart = DateTime.UtcNow.AddDays(-30),
            PeriodEnd = DateTime.UtcNow.AddDays(1), // Future date
            CommissionPercentage = 15m
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/payouts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayout_WithNoPaymentsInPeriod_ShouldReturn400BadRequest()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var provider = await CreateTestProviderWithServicesAsync();

        var request = new CreatePayoutRequest
        {
            ProviderId = provider.Id.Value,
            PeriodStart = DateTime.UtcNow.AddDays(-60),
            PeriodEnd = DateTime.UtcNow.AddDays(-50), // No payments in this period
            CommissionPercentage = 15m
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/payouts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Execute Payout Tests

    [Fact]
    public async Task ExecutePayout_WithValidPayout_ShouldReturn200Ok()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        // Create a payout first
        var payout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            payments.Select(p => p.Id).ToList(),
            "Test payout");

        await CreateEntityAsync(payout);

        var request = new ExecutePayoutRequest
        {
            ConnectedAccountId = "acct_test_123",
            Description = "Monthly payout execution"
        };

        // Act
        var response = await PostAsJsonAsync<ExecutePayoutRequest, PayoutResponse>(
            $"/api/v1/payouts/{payout.Id.Value}/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().BeOneOf("Processing", "Paid");
        response.Data.ExternalPayoutId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecutePayout_WithoutConnectedAccountId_ShouldReturn400BadRequest()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        var payout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            payments.Select(p => p.Id).ToList(),
            "Test payout");

        await CreateEntityAsync(payout);

        var request = new ExecutePayoutRequest
        {
            ConnectedAccountId = "", // Missing account ID
            Description = "Should fail"
        };

        // Act
        var response = await PostAsJsonAsync(
            $"/api/v1/payouts/{payout.Id.Value}/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExecutePayout_WithNonExistentPayout_ShouldReturn404NotFound()
    {
        // Arrange
        AuthenticateAsTestAdmin();
        var nonExistentPayoutId = Guid.NewGuid();

        var request = new ExecutePayoutRequest
        {
            ConnectedAccountId = "acct_test_123",
            Description = "Should fail"
        };

        // Act
        var response = await PostAsJsonAsync(
            $"/api/v1/payouts/{nonExistentPayoutId}/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Payout Tests

    [Fact]
    public async Task GetPayoutById_WithValidId_ShouldReturn200Ok()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        var payout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            payments.Select(p => p.Id).ToList(),
            "Test payout");

        await CreateEntityAsync(payout);

        // Act
        var response = await GetAsync<PayoutDetailsDto>(
            $"/api/v1/payouts/{payout.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.PayoutId.Should().Be(payout.Id.Value);
        response.Data.GrossAmount.Should().Be(300m);
        response.Data.CommissionAmount.Should().Be(45m);
        response.Data.NetAmount.Should().Be(255m);
    }

    [Fact]
    public async Task GetPayoutById_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        AuthenticateAsTestAdmin();
        var nonExistentPayoutId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/payouts/{nonExistentPayoutId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Pending Payouts Tests

    [Fact]
    public async Task GetPendingPayouts_ShouldReturnOnlyPendingPayouts()
    {
        // Arrange
        AuthenticateAsTestAdmin();

        var (provider, payments) = await CreateProviderWithPaymentsAsync();

        // Create pending and completed payouts
        var pendingPayout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow.AddDays(-31),
            payments.Select(p => p.Id).ToList(),
            "Pending payout");

        var completedPayout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-90),
            DateTime.UtcNow.AddDays(-61),
            payments.Select(p => p.Id).ToList(),
            "Completed payout");

        completedPayout.MarkAsProcessing("po_test_123");
        completedPayout.MarkAsPaid("1234", "Test Bank");

        await CreateEntityAsync(pendingPayout);
        await CreateEntityAsync(completedPayout);

        // Act
        var response = await GetAsync<List<PayoutSummaryDto>>("/api/v1/payouts/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Should().Contain(p => p.PayoutId == pendingPayout.Id.Value);
        response.Data.Should().NotContain(p => p.PayoutId == completedPayout.Id.Value);
    }

    #endregion

    #region Get Provider Payouts Tests

    [Fact]
    public async Task GetProviderPayouts_ShouldReturnAllProviderPayouts()
    {
        // Arrange
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsProviderOwner(provider);

        // Create multiple payouts for the provider
        var payout1 = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow.AddDays(-31),
            payments.Select(p => p.Id).ToList(),
            "Payout 1");

        var payout2 = Payout.Create(
            provider.Id,
            Money.Create(400, "USD"),
            Money.Create(60, "USD"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            payments.Select(p => p.Id).ToList(),
            "Payout 2");

        await CreateEntityAsync(payout1);
        await CreateEntityAsync(payout2);

        // Act
        var response = await GetAsync<List<PayoutDetailsDto>>(
            $"/api/v1/payouts/provider/{provider.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCount(2);
        response.Data.Should().Contain(p => p.PayoutId == payout1.Id.Value);
        response.Data.Should().Contain(p => p.PayoutId == payout2.Id.Value);
    }

    [Fact]
    public async Task GetProviderPayouts_WithStatusFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsProviderOwner(provider);

        var pendingPayout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow.AddDays(-31),
            payments.Select(p => p.Id).ToList(),
            "Pending payout");

        var paidPayout = Payout.Create(
            provider.Id,
            Money.Create(400, "USD"),
            Money.Create(60, "USD"),
            DateTime.UtcNow.AddDays(-90),
            DateTime.UtcNow.AddDays(-61),
            payments.Select(p => p.Id).ToList(),
            "Paid payout");

        paidPayout.MarkAsProcessing("po_test_123");
        paidPayout.MarkAsPaid("1234", "Test Bank");

        await CreateEntityAsync(pendingPayout);
        await CreateEntityAsync(paidPayout);

        // Act
        var response = await GetAsync<List<PayoutDetailsDto>>(
            $"/api/v1/payouts/provider/{provider.Id.Value}?status=Paid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCount(1);
        response.Data.Should().Contain(p => p.PayoutId == paidPayout.Id.Value);
        response.Data.Should().NotContain(p => p.PayoutId == pendingPayout.Id.Value);
    }

    #endregion
}
