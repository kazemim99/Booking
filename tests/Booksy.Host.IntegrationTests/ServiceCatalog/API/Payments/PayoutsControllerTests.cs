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
[Collection(BooksyHostTestCollection.Name)]
public class PayoutsControllerTests : ServiceCatalogIntegrationTestBase
{
    public PayoutsControllerTests(BooksyHostFactory factory) : base(factory)
    {
    }

    #region Helper Methods

    /// <summary>
    /// A provider who has actually been paid three times.
    ///
    /// The charges go through <c>POST /payments</c> rather than being written straight to the
    /// Payments table, because a payout is derived from the <b>ledger</b> — charges minus refunds
    /// minus prior payouts — and only the payment pipeline posts to it. Rows inserted behind the
    /// application left the ledger empty, so every payout in this class was correctly refused with
    /// "no payable ledger balance" and the tests proved nothing about payouts.
    /// </summary>
    private async Task<(Provider Provider, List<PaymentId> PaymentIds)> CreateProviderWithPaymentsAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        var paymentIds = new List<PaymentId>();

        // Three captured payments: 100 + 150 + 200 = 450 owed to the provider.
        for (int i = 0; i < 3; i++)
        {
            var booking = Booking.CreateBookingRequest(
                UserId.From(customerId),
                provider.Id,
                service.Id,
                provider.Id,
                DateTime.UtcNow.AddDays(-(30 - i)), // Past bookings
                service.Duration,
                service.BasePrice,
                BookingPolicy.Default,
                $"Booking {i + 1}");

            await CreateEntityAsync(booking);

            AuthenticateAsUser(customerId, "payer@test.com");
            var charge = await PostAsJsonAsync<ProcessPaymentRequest, PaymentResponse>(
                "/api/v1/payments",
                new ProcessPaymentRequest
                {
                    BookingId = booking.Id.Value,
                    ProviderId = provider.Id.Value,
                    Amount = 100 + (i * 50),
                    Currency = "USD",
                    PaymentMethod = "CreditCard",
                    PaymentMethodId = "pm_test_card",
                    CaptureImmediately = true,
                    Description = $"Charge {i + 1}"
                });

            charge.StatusCode.Should().Be(HttpStatusCode.Created, charge.Message);
            paymentIds.Add(PaymentId.From(charge.Data!.PaymentId));
        }

        return (provider, paymentIds);
    }

    #endregion

    #region Create Payout Tests

    [Fact]
    public async Task CreatePayout_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        // CreateTestProviderWithServicesAsync signs the caller in as that provider's owner, so the
        // identity a test wants has to be taken *after* the arrange. Taking admin first (as this
        // class used to) left every payout call authenticated as a provider — hence 403.
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsTestAdmin(); // Requires Admin/Finance role

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
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsTestAdmin();

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
        // POST /payouts answers 201 (its sibling test and the action's own documented contract);
        // this copy asserted 200.
        response.StatusCode.Should().Be(HttpStatusCode.Created);
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
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        ClearAuthenticationHeader();

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
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsTestAdmin();

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
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsTestAdmin();

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
        var provider = await CreateTestProviderWithServicesAsync();
        AuthenticateAsTestAdmin();

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
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsTestAdmin();

        // Create a payout first
        var payout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            payments,
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
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsTestAdmin();

        var payout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            payments,
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
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsTestAdmin();

        var payout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            payments,
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
        var (provider, payments) = await CreateProviderWithPaymentsAsync();
        AuthenticateAsTestAdmin();

        // Create pending and completed payouts
        var pendingPayout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow.AddDays(-31),
            payments,
            "Pending payout");

        var completedPayout = Payout.Create(
            provider.Id,
            Money.Create(300, "USD"),
            Money.Create(45, "USD"),
            DateTime.UtcNow.AddDays(-90),
            DateTime.UtcNow.AddDays(-61),
            payments,
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
            payments,
            "Payout 1");

        var payout2 = Payout.Create(
            provider.Id,
            Money.Create(400, "USD"),
            Money.Create(60, "USD"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            payments,
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
            payments,
            "Pending payout");

        var paidPayout = Payout.Create(
            provider.Id,
            Money.Create(400, "USD"),
            Money.Create(60, "USD"),
            DateTime.UtcNow.AddDays(-90),
            DateTime.UtcNow.AddDays(-61),
            payments,
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
