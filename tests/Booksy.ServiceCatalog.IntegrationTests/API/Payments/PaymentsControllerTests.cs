using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// Integration tests for Payments API endpoints
/// Covers: Payment processing, capture, refund, pricing calculations
/// Endpoints: /api/v1/payments/*
/// </summary>
public class PaymentsControllerTests : ServiceCatalogIntegrationTestBase
{
    public PaymentsControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
    {
    }

    #region Helper Methods

    private async Task<(Provider Provider, Service Service, Booking Booking)> CreateTestBookingAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        AuthenticateAsUser(customerId, "customer@test.com");

        var booking = Booking.CreateBookingRequest(
            UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Staff.First().Id,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            BookingPolicy.Default,
            "Test booking");

        await CreateEntityAsync(booking);
        return (provider, service, booking);
    }

    #endregion

    #region Process Payment Tests

    [Fact]
    public async Task ProcessPayment_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var request = new ProcessPaymentRequest
        {
            BookingId = booking.Id.Value,
            ProviderId = provider.Id.Value,
            Amount = 100.00m,
            Currency = "USD",
            PaymentMethod = "CreditCard",
            PaymentMethodId = "pm_test_card",
            CaptureImmediately = true,
            Description = "Test payment"
        };

        // Act
        var response = await PostAsJsonAsync<ProcessPaymentRequest, PaymentResponse>(
            "/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.PaymentId.Should().NotBeEmpty();
        response.Data.BookingId.Should().Be(booking.Id.Value);
        response.Data.ProviderId.Should().Be(provider.Id.Value);
        response.Data.Amount.Should().Be(100.00m);
        response.Data.Currency.Should().Be("USD");
        response.Data.Status.Should().Be("Paid");

        // Verify payment exists in database
        var payment = await DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(response.Data.PaymentId));
        payment.Should().NotBeNull();
        payment!.Amount.Amount.Should().Be(100.00m);
    }

    [Fact]
    public async Task ProcessPayment_WithAuthorizationOnly_ShouldReturn201WithAuthorizedStatus()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var request = new ProcessPaymentRequest
        {
            BookingId = booking.Id.Value,
            ProviderId = provider.Id.Value,
            Amount = 100.00m,
            Currency = "USD",
            PaymentMethod = "CreditCard",
            PaymentMethodId = "pm_test_card",
            CaptureImmediately = false, // Authorization only
            Description = "Test authorization"
        };

        // Act
        var response = await PostAsJsonAsync<ProcessPaymentRequest, PaymentResponse>(
            "/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().Be("Authorized");

        var payment = await DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(response.Data.PaymentId));
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Authorized);
    }

    [Fact]
    public async Task ProcessPayment_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();
        ClearAuthenticationHeader();

        var request = new ProcessPaymentRequest
        {
            BookingId = booking.Id.Value,
            ProviderId = provider.Id.Value,
            Amount = 100.00m,
            Currency = "USD",
            PaymentMethod = "CreditCard",
            PaymentMethodId = "pm_test_card",
            CaptureImmediately = true
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProcessPayment_WithInvalidAmount_ShouldReturn400BadRequest()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var request = new ProcessPaymentRequest
        {
            BookingId = booking.Id.Value,
            ProviderId = provider.Id.Value,
            Amount = -50.00m, // Invalid negative amount
            Currency = "USD",
            PaymentMethod = "CreditCard",
            PaymentMethodId = "pm_test_card",
            CaptureImmediately = true
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessPayment_WithInvalidCurrency_ShouldReturn400BadRequest()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var request = new ProcessPaymentRequest
        {
            BookingId = booking.Id.Value,
            ProviderId = provider.Id.Value,
            Amount = 100.00m,
            Currency = "INVALID", // Invalid currency code
            PaymentMethod = "CreditCard",
            PaymentMethodId = "pm_test_card",
            CaptureImmediately = true
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Capture Payment Tests

    [Fact]
    public async Task CapturePayment_WithAuthorizedPayment_ShouldReturn200Ok()
    {
        // Arrange - Create an authorized payment first
        var (provider, service, booking) = await CreateTestBookingAsync();

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(booking.CustomerId.Value),
            provider.Id,
            Money.Create(100, "USD"),
            PaymentMethod.CreditCard,
            RefundPolicy.Moderate);

        payment.Authorize("pi_test_123", "pm_test_card");
        await CreateEntityAsync(payment);

        var request = new CapturePaymentRequest
        {
            Amount = null, // Full capture
            Notes = "Capturing full payment"
        };

        // Act
        var response = await PostAsJsonAsync<CapturePaymentRequest, PaymentResponse>(
            $"/api/v1/payments/{payment.Id.Value}/capture", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().Be("Paid");

        var updatedPayment = await DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task CapturePayment_WithPartialAmount_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(booking.CustomerId.Value),
            provider.Id,
            Money.Create(100, "USD"),
            PaymentMethod.CreditCard,
            RefundPolicy.Moderate);

        payment.Authorize("pi_test_123", "pm_test_card");
        await CreateEntityAsync(payment);

        var request = new CapturePaymentRequest
        {
            Amount = 50.00m, // Partial capture
            Notes = "Partial capture"
        };

        // Act
        var response = await PostAsJsonAsync<CapturePaymentRequest, PaymentResponse>(
            $"/api/v1/payments/{payment.Id.Value}/capture", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CapturePayment_WithNonExistentPayment_ShouldReturn404NotFound()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();
        var nonExistentPaymentId = Guid.NewGuid();

        var request = new CapturePaymentRequest
        {
            Amount = null,
            Notes = "Should fail"
        };

        // Act
        var response = await PostAsJsonAsync(
            $"/api/v1/payments/{nonExistentPaymentId}/capture", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Refund Payment Tests

    [Fact]
    public async Task RefundPayment_WithPaidPayment_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(booking.CustomerId.Value),
            provider.Id,
            Money.Create(100, "USD"),
            PaymentMethod.CreditCard,
            RefundPolicy.Flexible);

        payment.ProcessCharge("pi_test_123", "pm_test_card");
        await CreateEntityAsync(payment);

        var request = new RefundPaymentRequest
        {
            Amount = 100.00m, // Full refund
            Reason = "CustomerRequest",
            Notes = "Customer requested refund"
        };

        // Act
        var response = await PostAsJsonAsync<RefundPaymentRequest, PaymentResponse>(
            $"/api/v1/payments/{payment.Id.Value}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().Be("Refunded");

        var updatedPayment = await DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task RefundPayment_WithPartialAmount_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(booking.CustomerId.Value),
            provider.Id,
            Money.Create(100, "USD"),
            PaymentMethod.CreditCard,
            RefundPolicy.Flexible);

        payment.ProcessCharge("pi_test_123", "pm_test_card");
        await CreateEntityAsync(payment);

        var request = new RefundPaymentRequest
        {
            Amount = 50.00m, // Partial refund
            Reason = "ServiceCancellation",
            Notes = "Partial service cancellation"
        };

        // Act
        var response = await PostAsJsonAsync<RefundPaymentRequest, PaymentResponse>(
            $"/api/v1/payments/{payment.Id.Value}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().Be("PartiallyRefunded");

        var updatedPayment = await DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        updatedPayment.RefundedAmount.Amount.Should().Be(50.00m);
    }

    [Fact]
    public async Task RefundPayment_WithoutReason_ShouldReturn400BadRequest()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(booking.CustomerId.Value),
            provider.Id,
            Money.Create(100, "USD"),
            PaymentMethod.CreditCard,
            RefundPolicy.Flexible);

        payment.ProcessCharge("pi_test_123", "pm_test_card");
        await CreateEntityAsync(payment);

        var request = new RefundPaymentRequest
        {
            Amount = 100.00m,
            Reason = "", // Missing reason
            Notes = "Should fail"
        };

        // Act
        var response = await PostAsJsonAsync(
            $"/api/v1/payments/{payment.Id.Value}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Payment Details Tests

    [Fact]
    public async Task GetPaymentById_WithValidId_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, service, booking) = await CreateTestBookingAsync();

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(booking.CustomerId.Value),
            provider.Id,
            Money.Create(100, "USD"),
            PaymentMethod.CreditCard,
            RefundPolicy.Moderate);

        payment.ProcessCharge("pi_test_123", "pm_test_card");
        await CreateEntityAsync(payment);

        // Act
        var response = await GetAsync<PaymentDetailsViewModel>(
            $"/api/v1/payments/{payment.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.PaymentId.Should().Be(payment.Id.Value);
        response.Data.Amount.Should().Be(100.00m);
        response.Data.Status.Should().Be("Paid");
        response.Data.Transactions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPaymentById_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentPaymentId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/payments/{nonExistentPaymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Customer Payments Tests

    [Fact]
    public async Task GetCustomerPayments_ShouldReturnAllCustomerPayments()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        AuthenticateAsUser(customerId, "customer@test.com");

        var (provider, service, booking1) = await CreateTestBookingAsync();

        // Create multiple payments
        var payment1 = Payment.CreateForBooking(
            booking1.Id,
            UserId.From(customerId),
            provider.Id,
            Money.Create(100, "USD"),
            PaymentMethod.CreditCard,
            RefundPolicy.Moderate);
        payment1.ProcessCharge("pi_test_1", "pm_test_card");

        var booking2 = Booking.CreateBookingRequest(
            UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Staff.First().Id,
            DateTime.UtcNow.AddDays(3),
            service.Duration,
            service.BasePrice,
            BookingPolicy.Default,
            "Second booking");
        await CreateEntityAsync(booking2);

        var payment2 = Payment.CreateForBooking(
            booking2.Id,
            UserId.From(customerId),
            provider.Id,
            Money.Create(150, "USD"),
            PaymentMethod.CreditCard,
            RefundPolicy.Moderate);
        payment2.ProcessCharge("pi_test_2", "pm_test_card");

        await CreateEntityAsync(payment1);
        await CreateEntityAsync(payment2);

        // Act
        var response = await GetAsync<List<PaymentSummaryDto>>(
            $"/api/v1/payments/customer/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCountGreaterOrEqualTo(2);
    }

    #endregion

    #region Calculate Pricing Tests

    [Fact]
    public async Task CalculatePricing_WithValidData_ShouldReturn200Ok()
    {
        // Arrange
        var request = new CalculatePricingRequest
        {
            BaseAmount = 100.00m,
            Currency = "USD",
            TaxPercentage = 10m,
            TaxInclusive = false,
            DiscountPercentage = 15m,
            PlatformFeePercentage = 5m,
            DepositPercentage = 30m
        };

        // Act
        var response = await PostAsJsonAsync<CalculatePricingRequest, PricingCalculationViewModel>(
            "/api/v1/payments/calculate-pricing", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.BaseAmount.Should().Be(100.00m);
        response.Data.Breakdown.Should().NotBeNull();
        response.Data.Breakdown.DiscountAmount.Should().BeGreaterThan(0);
        response.Data.Breakdown.TaxAmount.Should().BeGreaterThan(0);
        response.Data.Breakdown.PlatformFee.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculatePricing_WithInclusiveTax_ShouldCalculateCorrectly()
    {
        // Arrange
        var request = new CalculatePricingRequest
        {
            BaseAmount = 120.00m,
            Currency = "USD",
            TaxPercentage = 20m,
            TaxInclusive = true // VAT-style tax
        };

        // Act
        var response = await PostAsJsonAsync<CalculatePricingRequest, PricingCalculationViewModel>(
            "/api/v1/payments/calculate-pricing", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Breakdown.TaxAmount.Should().Be(20.00m);
    }

    [Fact]
    public async Task CalculatePricing_WithoutAuthentication_ShouldReturn200Ok()
    {
        // Arrange
        ClearAuthenticationHeader(); // This endpoint should allow anonymous access

        var request = new CalculatePricingRequest
        {
            BaseAmount = 100.00m,
            Currency = "USD",
            TaxPercentage = 10m
        };

        // Act
        var response = await PostAsJsonAsync<CalculatePricingRequest, PricingCalculationViewModel>(
            "/api/v1/payments/calculate-pricing", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
