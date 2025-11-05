using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.FinancialValueObjects;

public class RefundPolicyTests
{
    [Fact]
    public void Flexible_Policy_Should_Allow_Full_Refund_24_Hours_Before()
    {
        // Arrange
        var policy = RefundPolicy.Flexible;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(2);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(100m, refundAmount.Amount); // Full refund
    }

    [Fact]
    public void Flexible_Policy_Should_Allow_50_Percent_Refund_Within_24_Hours()
    {
        // Arrange
        var policy = RefundPolicy.Flexible;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(12);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(50m, refundAmount.Amount); // 50% refund
    }

    [Fact]
    public void Flexible_Policy_Should_Allow_No_Refund_After_Booking_Time()
    {
        // Arrange
        var policy = RefundPolicy.Flexible;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(-1);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(0m, refundAmount.Amount); // No refund
    }

    [Fact]
    public void Moderate_Policy_Should_Allow_Full_Refund_More_Than_48_Hours()
    {
        // Arrange
        var policy = RefundPolicy.Moderate;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(3);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(100m, refundAmount.Amount); // Full refund
    }

    [Fact]
    public void Moderate_Policy_Should_Allow_50_Percent_Refund_Between_24_And_48_Hours()
    {
        // Arrange
        var policy = RefundPolicy.Moderate;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(36);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(50m, refundAmount.Amount); // 50% refund
    }

    [Fact]
    public void Moderate_Policy_Should_Allow_No_Refund_Within_24_Hours()
    {
        // Arrange
        var policy = RefundPolicy.Moderate;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(12);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(0m, refundAmount.Amount); // No refund
    }

    [Fact]
    public void Strict_Policy_Should_Allow_Full_Refund_More_Than_7_Days()
    {
        // Arrange
        var policy = RefundPolicy.Strict;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(10);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(100m, refundAmount.Amount); // Full refund
    }

    [Fact]
    public void Strict_Policy_Should_Allow_50_Percent_Refund_Between_3_And_7_Days()
    {
        // Arrange
        var policy = RefundPolicy.Strict;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(5);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(50m, refundAmount.Amount); // 50% refund
    }

    [Fact]
    public void Strict_Policy_Should_Allow_No_Refund_Within_3_Days()
    {
        // Arrange
        var policy = RefundPolicy.Strict;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(2);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(0m, refundAmount.Amount); // No refund
    }

    [Fact]
    public void NoRefunds_Policy_Should_Never_Allow_Refund()
    {
        // Arrange
        var policy = RefundPolicy.NoRefunds;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(30);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(0m, refundAmount.Amount); // No refund ever
    }

    [Fact]
    public void Custom_Policy_Should_Calculate_Based_On_Custom_Tiers()
    {
        // Arrange
        var policy = RefundPolicy.Create(
            fullRefundHours: 72,
            partialRefundHours: 24,
            partialRefundPercentage: 25);

        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(48);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(25m, refundAmount.Amount); // 25% refund (between 24 and 72 hours)
    }

    [Fact]
    public void Custom_Policy_Should_Allow_Full_Refund_Beyond_Full_Refund_Window()
    {
        // Arrange
        var policy = RefundPolicy.Create(
            fullRefundHours: 72,
            partialRefundHours: 24,
            partialRefundPercentage: 25);

        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(100);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(100m, refundAmount.Amount); // Full refund
    }

    [Fact]
    public void Custom_Policy_Should_Allow_No_Refund_Within_Partial_Refund_Window()
    {
        // Arrange
        var policy = RefundPolicy.Create(
            fullRefundHours: 72,
            partialRefundHours: 24,
            partialRefundPercentage: 25);

        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(12);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(0m, refundAmount.Amount); // No refund
    }

    [Fact]
    public void Custom_Should_Throw_When_Full_Refund_Hours_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            RefundPolicy.Create(-1, 24, 50));
        Assert.Contains("full refund hours", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Custom_Should_Throw_When_Partial_Refund_Hours_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            RefundPolicy.Create(72, -1, 50));
        Assert.Contains("partial refund hours", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Custom_Should_Throw_When_Partial_Percentage_Exceeds_100()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            RefundPolicy.Create(72, 24, 105));
        Assert.Contains("percentage", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Custom_Should_Throw_When_Full_Refund_Hours_Less_Than_Partial()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            RefundPolicy.Create(24, 72, 50));
        Assert.Contains("full refund hours must be greater", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(200, 24, 200)]   // Flexible: >24h = 100%
    [InlineData(200, 12, 100)]   // Flexible: <24h = 50%
    [InlineData(200, 0, 0)]      // Flexible: past booking = 0%
    public void Flexible_Policy_Should_Calculate_Various_Amounts_Correctly(
        decimal paidAmount, int hoursUntilBooking, decimal expectedRefund)
    {
        // Arrange
        var policy = RefundPolicy.Flexible;
        var money = Money.Create(paidAmount, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(hoursUntilBooking);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(money, bookingTime, currentTime);

        // Assert
        Assert.Equal(expectedRefund, refundAmount.Amount);
    }

    [Fact]
    public void CalculateRefundAmount_Should_Return_Zero_For_Zero_Paid_Amount()
    {
        // Arrange
        var policy = RefundPolicy.Flexible;
        var paidAmount = Money.Zero("USD");
        var bookingTime = DateTime.UtcNow.AddDays(2);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(0m, refundAmount.Amount);
    }
}
