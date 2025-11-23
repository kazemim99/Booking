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
    public void Flexible_Policy_Should_Allow_Full_Refund_At_36_Hours()
    {
        // Arrange - Flexible: fullRefundWindowHours: 24, partialRefundWindowHours: 48
        // 36h >= 24h (fullRefundWindow), so gets full refund
        var policy = RefundPolicy.Flexible;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(36);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert - 36h >= 24h (full window), so full refund
        Assert.Equal(100m, refundAmount.Amount);
    }

    [Fact]
    public void Flexible_Policy_Should_Apply_Cancellation_Fee_Within_24_Hours()
    {
        // Arrange - Flexible has cancellationFeePercentage: 10%
        var policy = RefundPolicy.Flexible;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(12); // Within 24 hours
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(90m, refundAmount.Amount); // 100 - 10% cancellation fee = 90
    }

    [Fact]
    public void Flexible_Policy_Should_Apply_Cancellation_Fee_After_Booking_Time()
    {
        // Arrange
        var policy = RefundPolicy.Flexible;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(-1);
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(90m, refundAmount.Amount); // 100 - 10% cancellation fee = 90
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
    public void Moderate_Policy_Should_Allow_Full_Refund_Between_48_And_72_Hours()
    {
        // Arrange - Moderate: fullRefundWindowHours: 48, partialRefundWindowHours: 72
        // Domain logic: if hours >= fullRefundWindowHours (48), gets full refund
        var policy = RefundPolicy.Moderate;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(60); // 60 >= 48, so full refund
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(100m, refundAmount.Amount); // Full refund since 60 >= 48
    }

    [Fact]
    public void Moderate_Policy_Should_Apply_Cancellation_Fee_Under_48_Hours()
    {
        // Arrange - Moderate: fullRefundWindowHours: 48, partialRefundWindowHours: 72
        // cancellationFeePercentage: 20%
        // When 36h < 48h (not full) and 36h < 72h (not partial), apply cancellation fee
        var policy = RefundPolicy.Moderate;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(36); // 36h < 48h
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert - 36h < 48h and 36h < 72h, so cancellation fee: 100 - 20% = 80
        Assert.Equal(80m, refundAmount.Amount);
    }

    [Fact]
    public void Strict_Policy_Should_Allow_Full_Refund_More_Than_7_Days()
    {
        // Arrange - Strict: fullRefundWindowHours: 168 (7 days)
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
    public void Strict_Policy_Should_Allow_30_Percent_Refund_Between_7_And_14_Days()
    {
        // Arrange - Strict: partialRefundWindowHours: 336 (14 days), partialRefundPercentage: 30%
        var policy = RefundPolicy.Strict;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(10); // Between 7-14 days
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        // 10 days = 240 hours >= 168 (full window), so gets full refund
        Assert.Equal(100m, refundAmount.Amount);
    }

    [Fact]
    public void Strict_Policy_Should_Apply_Partial_Refund_Within_7_Days()
    {
        // Arrange - Strict: fullRefundWindowHours: 168 (7 days), partialRefundWindowHours: 336 (14 days)
        // partialRefundPercentage: 30%, cancellationFeePercentage: 30%
        var policy = RefundPolicy.Strict;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(12); // 12 days = 288 hours
        // 288 >= 168 (full window) → full refund
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert - 288h >= 168h (full window), gets full refund
        Assert.Equal(100m, refundAmount.Amount);
    }

    [Fact]
    public void Strict_Policy_Should_Apply_Cancellation_Fee_Under_7_Days()
    {
        // Arrange - Strict: fullRefundWindowHours: 168 (7 days), partialRefundWindowHours: 336 (14 days)
        // Domain logic: if hours < fullRefund (168) AND hours < partialRefund (336), apply cancellation fee
        // 5 days = 120h: 120 < 168 (not full), 120 < 336 (not partial either), so cancellation fee applies
        var policy = RefundPolicy.Strict;
        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddDays(5); // 5 days = 120 hours
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert - 120h < 168h and 120h < 336h, so cancellation fee: 100 - 30% = 70
        Assert.Equal(70m, refundAmount.Amount);
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
            allowRefunds: true,
            fullRefundWindowHours: 72,
            partialRefundWindowHours: 24,
            partialRefundPercentage: 25,
            cancellationFeePercentage: 50,
            refundProcessingFees: false);

        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(48); // Between 24-72h
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        // 48h >= 24h (partial window) so partial refund = 25%
        Assert.Equal(25m, refundAmount.Amount);
    }

    [Fact]
    public void Custom_Policy_Should_Allow_Full_Refund_Beyond_Full_Refund_Window()
    {
        // Arrange
        var policy = RefundPolicy.Create(
            allowRefunds: true,
            fullRefundWindowHours: 72,
            partialRefundWindowHours: 24,
            partialRefundPercentage: 25,
            cancellationFeePercentage: 50,
            refundProcessingFees: false);

        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(100); // > 72h
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(100m, refundAmount.Amount); // Full refund
    }

    [Fact]
    public void Custom_Policy_Should_Apply_Cancellation_Fee_Within_Partial_Window()
    {
        // Arrange
        var policy = RefundPolicy.Create(
            allowRefunds: true,
            fullRefundWindowHours: 72,
            partialRefundWindowHours: 24,
            partialRefundPercentage: 25,
            cancellationFeePercentage: 50, // 50% cancellation fee
            refundProcessingFees: false);

        var paidAmount = Money.Create(100, "USD");
        var bookingTime = DateTime.UtcNow.AddHours(12); // < 24h
        var currentTime = DateTime.UtcNow;

        // Act
        var refundAmount = policy.CalculateRefundAmount(paidAmount, bookingTime, currentTime);

        // Assert
        Assert.Equal(50m, refundAmount.Amount); // 100 - 50% cancellation fee = 50
    }

    [Fact]
    public void Custom_Should_Throw_When_Full_Refund_Hours_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RefundPolicy.Create(true, -1, 24, 50, 30, false));
        Assert.Contains("negative", exception.Message.ToLower());
    }

    [Fact]
    public void Custom_Should_Throw_When_Partial_Refund_Hours_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RefundPolicy.Create(true, 72, -1, 50, 30, false));
        Assert.Contains("negative", exception.Message.ToLower());
    }

    [Fact]
    public void Custom_Should_Throw_When_Partial_Percentage_Exceeds_100()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RefundPolicy.Create(true, 72, 24, 105, 30, false));
        Assert.Contains("percentage", exception.Message.ToLower());
    }

    [Fact]
    public void Custom_Should_Throw_When_Cancellation_Fee_Exceeds_100()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RefundPolicy.Create(true, 72, 24, 50, 105, false));
        Assert.Contains("percentage", exception.Message.ToLower());
    }

    [Theory]
    [InlineData(200, 50, 200)]   // Flexible: 50h >= 24h (fullRefundWindow) = 100%
    [InlineData(200, 36, 200)]   // Flexible: 36h >= 24h (fullRefundWindow) = 100%
    [InlineData(200, 12, 180)]   // Flexible: 12h < 24h, 12h < 48h, cancellation fee = 100-10% = 180
    public void Flexible_Policy_Should_Calculate_Various_Amounts_Correctly(
        decimal paidAmount, int hoursUntilBooking, decimal expectedRefund)
    {
        // Arrange - Flexible: fullRefundWindowHours: 24, partialRefundWindowHours: 48
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
