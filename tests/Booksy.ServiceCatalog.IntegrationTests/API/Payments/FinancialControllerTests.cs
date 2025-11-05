using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Queries.Payment.GetProviderEarnings;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// Integration tests for Financial API endpoints
/// Covers: Provider earnings calculations and financial reporting
/// Endpoints: /api/v1/financial/*
/// </summary>
public class FinancialControllerTests : ServiceCatalogIntegrationTestBase
{
    public FinancialControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
    {
    }

    #region Helper Methods

    private async Task<(Provider Provider, List<Payment> Payments)> CreateProviderWithEarningsAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        var payments = new List<Payment>();

        // Create payments across different dates
        var dates = new[]
        {
            DateTime.UtcNow.AddDays(-25),
            DateTime.UtcNow.AddDays(-20),
            DateTime.UtcNow.AddDays(-15),
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(-5)
        };

        foreach (var date in dates)
        {
            var booking = Booking.CreateBookingRequest(
                UserId.From(customerId),
                provider.Id,
                service.Id,
                provider.Staff.First().Id,
                date,
                service.Duration,
                service.BasePrice,
                BookingPolicy.Default,
                "Earnings test booking");

            await CreateEntityAsync(booking);

            var payment = Payment.CreateForBooking(
                booking.Id,
                UserId.From(customerId),
                provider.Id,
                Money.Create(100, "USD"),
                PaymentMethod.CreditCard);

            payment.ProcessCharge($"pi_test_{Guid.NewGuid()}", "pm_test_card");
            payments.Add(payment);
            await CreateEntityAsync(payment);
        }

        return (provider, payments);
    }

    #endregion

    #region Get Provider Earnings Tests

    [Fact]
    public async Task GetProviderEarnings_WithValidDateRange_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, payments) = await CreateProviderWithEarningsAsync();
        AuthenticateAsProviderOwner(provider);

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var response = await GetAsync<ProviderEarningsViewModel>(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.ProviderId.Should().Be(provider.Id.Value);
        response.Data.GrossEarnings.Should().Be(500m); // 5 payments * $100
        response.Data.CommissionAmount.Should().Be(75m); // 15% of $500
        response.Data.NetEarnings.Should().Be(425m); // $500 - $75
        response.Data.Currency.Should().Be("USD");
        response.Data.TotalPayments.Should().Be(5);
        response.Data.EarningsByDate.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProviderEarnings_WithCustomCommission_ShouldCalculateCorrectly()
    {
        // Arrange
        var (provider, payments) = await CreateProviderWithEarningsAsync();
        AuthenticateAsProviderOwner(provider);

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var customCommission = 20m; // 20% commission

        // Act
        var response = await GetAsync<ProviderEarningsViewModel>(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&commissionPercentage={customCommission}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.GrossEarnings.Should().Be(500m);
        response.Data.CommissionAmount.Should().Be(100m); // 20% of $500
        response.Data.NetEarnings.Should().Be(400m); // $500 - $100
    }

    [Fact]
    public async Task GetProviderEarnings_WithNoPayments_ShouldReturnZeroEarnings()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        AuthenticateAsProviderOwner(provider);

        var startDate = DateTime.UtcNow.AddDays(-60);
        var endDate = DateTime.UtcNow.AddDays(-50); // Period with no payments

        // Act
        var response = await GetAsync<ProviderEarningsViewModel>(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.GrossEarnings.Should().Be(0m);
        response.Data.NetEarnings.Should().Be(0m);
        response.Data.TotalPayments.Should().Be(0);
    }

    [Fact]
    public async Task GetProviderEarnings_WithRefunds_ShouldSubtractRefundedAmount()
    {
        // Arrange
        var (provider, payments) = await CreateProviderWithEarningsAsync();

        // Refund one of the payments
        var paymentToRefund = payments.First();
        paymentToRefund.Refund(
            Money.Create(50, "USD"),
            "re_test_123",
            RefundReason.CustomerCancellation,
            "Partial refund");

        AuthenticateAsProviderOwner(provider);

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var response = await GetAsync<ProviderEarningsViewModel>(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.TotalRefunded.Should().Be(50m);
        // Net earnings = (Gross - Commission - Refunded)
        // = (500 - 75 - 50) = 375
        response.Data.NetEarnings.Should().Be(375m);
    }

    [Fact]
    public async Task GetProviderEarnings_WithInvalidDateRange_ShouldReturn400BadRequest()
    {
        // Arrange
        var (provider, payments) = await CreateProviderWithEarningsAsync();
        AuthenticateAsProviderOwner(provider);

        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-30); // End before start

        // Act
        var response = await GetAsync(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProviderEarnings_AsUnauthorizedUser_ShouldReturn401Unauthorized()
    {
        // Arrange
        var (provider, payments) = await CreateProviderWithEarningsAsync();
        ClearAuthenticationHeader();

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var response = await GetAsync(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProviderEarnings_EarningsByDate_ShouldGroupByDate()
    {
        // Arrange
        var (provider, payments) = await CreateProviderWithEarningsAsync();
        AuthenticateAsProviderOwner(provider);

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var response = await GetAsync<ProviderEarningsViewModel>(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.EarningsByDate.Should().NotBeEmpty();
        response.Data.EarningsByDate.Should().HaveCount(5); // 5 different dates

        // Verify daily breakdown structure
        foreach (var dailyEarning in response.Data.EarningsByDate)
        {
            dailyEarning.Date.Should().NotBe(default);
            dailyEarning.GrossAmount.Should().Be(100m); // $100 per day
            dailyEarning.PaymentCount.Should().Be(1);
        }
    }

    #endregion

    #region Get Current Month Earnings Tests

    [Fact]
    public async Task GetCurrentMonthEarnings_ShouldReturnCurrentMonthData()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        // Create a payment in current month
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15);
        var booking = Booking.CreateBookingRequest(
            UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Staff.First().Id,
            thisMonth,
            service.Duration,
            service.BasePrice,
            BookingPolicy.Default,
            "Current month booking");

        await CreateEntityAsync(booking);

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(customerId),
            provider.Id,
            Money.Create(200, "USD"),
            PaymentMethod.CreditCard);

        payment.ProcessCharge("pi_current_month", "pm_test_card");
        await CreateEntityAsync(payment);

        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync<ProviderEarningsViewModel>(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings/current-month");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.GrossEarnings.Should().Be(200m);
        response.Data.PeriodStart.Month.Should().Be(DateTime.UtcNow.Month);
        response.Data.EndDate.Month.Should().Be(DateTime.UtcNow.Month);
    }

    #endregion

    #region Get Previous Month Earnings Tests

    [Fact]
    public async Task GetPreviousMonthEarnings_ShouldReturnPreviousMonthData()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        // Create a payment in previous month
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var booking = Booking.CreateBookingRequest(
            UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Staff.First().Id,
            new DateTime(lastMonth.Year, lastMonth.Month, 15),
            service.Duration,
            service.BasePrice,
            BookingPolicy.Default,
            "Previous month booking");

        await CreateEntityAsync(booking);

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(customerId),
            provider.Id,
            Money.Create(300, "USD"),
            PaymentMethod.CreditCard);

        payment.ProcessCharge("pi_previous_month", "pm_test_card");
        await CreateEntityAsync(payment);

        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync<ProviderEarningsViewModel>(
            $"/api/v1/financial/provider/{provider.Id.Value}/earnings/previous-month");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.GrossEarnings.Should().Be(300m);
        response.Data.PeriodStart.Month.Should().Be(lastMonth.Month);
        response.Data.EndDate.Month.Should().Be(lastMonth.Month);
    }

    #endregion
}
