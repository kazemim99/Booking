using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ValueObjects;

public class BookingPolicyTests
{
    [Fact]
    public void Create_Should_Create_Policy_With_Valid_Parameters()
    {
        // Arrange & Act
        var policy = BookingPolicy.Create(
            minAdvanceBookingHours: 2,
            maxAdvanceBookingDays: 90,
            cancellationWindowHours: 24,
            cancellationFeePercentage: 50,
            allowRescheduling: true,
            rescheduleWindowHours: 24,
            requireDeposit: true,
            depositPercentage: 20);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(2, policy.MinAdvanceBookingHours);
        Assert.Equal(90, policy.MaxAdvanceBookingDays);
        Assert.Equal(24, policy.CancellationWindowHours);
        Assert.Equal(50, policy.CancellationFeePercentage);
        Assert.True(policy.AllowRescheduling);
        Assert.Equal(24, policy.RescheduleWindowHours);
        Assert.True(policy.RequireDeposit);
        Assert.Equal(20, policy.DepositPercentage);
    }

    [Fact]
    public void Create_Should_Throw_When_MinAdvanceBookingHours_Is_Negative()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            BookingPolicy.Create(-1, 90, 24, 50, true, 24, true, 20));
    }

    [Fact]
    public void Create_Should_Throw_When_MaxAdvanceBookingDays_Is_Zero()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            BookingPolicy.Create(2, 0, 24, 50, true, 24, true, 20));
    }

    [Fact]
    public void Create_Should_Throw_When_CancellationFeePercentage_Exceeds_100()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            BookingPolicy.Create(2, 90, 24, 150, true, 24, true, 20));
    }

    [Fact]
    public void Create_Should_Throw_When_DepositPercentage_Is_Negative()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            BookingPolicy.Create(2, 90, 24, 50, true, 24, true, -10));
    }

    [Fact]
    public void Default_Should_Return_Standard_Policy()
    {
        // Act
        var policy = BookingPolicy.Default;

        // Assert
        Assert.Equal(2, policy.MinAdvanceBookingHours);
        Assert.Equal(90, policy.MaxAdvanceBookingDays);
        Assert.Equal(24, policy.CancellationWindowHours);
        Assert.Equal(50, policy.CancellationFeePercentage);
        Assert.True(policy.AllowRescheduling);
        Assert.True(policy.RequireDeposit);
        Assert.Equal(20, policy.DepositPercentage);
    }

    [Fact]
    public void Flexible_Should_Return_Customer_Friendly_Policy()
    {
        // Act
        var policy = BookingPolicy.Flexible;

        // Assert
        Assert.Equal(1, policy.MinAdvanceBookingHours);
        Assert.Equal(60, policy.MaxAdvanceBookingDays);
        Assert.Equal(12, policy.CancellationWindowHours);
        Assert.Equal(25, policy.CancellationFeePercentage);
        Assert.True(policy.AllowRescheduling);
        Assert.False(policy.RequireDeposit);
        Assert.Equal(0, policy.DepositPercentage);
    }

    [Fact]
    public void Strict_Should_Return_Provider_Protective_Policy()
    {
        // Act
        var policy = BookingPolicy.Strict;

        // Assert
        Assert.Equal(24, policy.MinAdvanceBookingHours);
        Assert.Equal(180, policy.MaxAdvanceBookingDays);
        Assert.Equal(72, policy.CancellationWindowHours);
        Assert.Equal(100, policy.CancellationFeePercentage);
        Assert.True(policy.AllowRescheduling);
        Assert.True(policy.RequireDeposit);
        Assert.Equal(50, policy.DepositPercentage);
    }

    [Fact]
    public void CalculateDepositAmount_Should_Return_Correct_Amount()
    {
        // Arrange
        var policy = BookingPolicy.Create(2, 90, 24, 50, true, 24, true, 25);
        var totalPrice = Money.Create(200m, "USD");

        // Act
        var deposit = policy.CalculateDepositAmount(totalPrice);

        // Assert
        Assert.Equal(50m, deposit.Amount); // 25% of 200
        Assert.Equal("USD", deposit.Currency);
    }

    [Fact]
    public void CalculateDepositAmount_Should_Return_Zero_When_No_Deposit_Required()
    {
        // Arrange
        var policy = BookingPolicy.Flexible; // No deposit
        var totalPrice = Money.Create(100m, "USD");

        // Act
        var deposit = policy.CalculateDepositAmount(totalPrice);

        // Assert
        Assert.Equal(0m, deposit.Amount);
    }

    [Fact]
    public void CalculateCancellationFee_Should_Return_Correct_Amount()
    {
        // Arrange
        var policy = BookingPolicy.Default; // 50% fee
        var totalPrice = Money.Create(100m, "USD");

        // Act
        var fee = policy.CalculateCancellationFee(totalPrice);

        // Assert
        Assert.Equal(50m, fee.Amount);
        Assert.Equal("USD", fee.Currency);
    }

    [Fact]
    public void CanCancelWithoutFee_Should_Return_True_When_Within_Window()
    {
        // Arrange
        var policy = BookingPolicy.Default; // 24 hour window
        var bookingTime = DateTime.UtcNow.AddHours(48);
        var currentTime = DateTime.UtcNow;

        // Act
        var canCancel = policy.CanCancelWithoutFee(bookingTime, currentTime);

        // Assert
        Assert.True(canCancel);
    }

    [Fact]
    public void CanCancelWithoutFee_Should_Return_False_When_Outside_Window()
    {
        // Arrange
        var policy = BookingPolicy.Default; // 24 hour window
        var bookingTime = DateTime.UtcNow.AddHours(12);
        var currentTime = DateTime.UtcNow;

        // Act
        var canCancel = policy.CanCancelWithoutFee(bookingTime, currentTime);

        // Assert
        Assert.False(canCancel);
    }

    [Fact]
    public void CanReschedule_Should_Return_True_When_Within_Window_And_Allowed()
    {
        // Arrange
        var policy = BookingPolicy.Default; // Allows rescheduling, 24 hour window
        var bookingTime = DateTime.UtcNow.AddDays(2);
        var currentTime = DateTime.UtcNow;

        // Act
        var canReschedule = policy.CanReschedule(bookingTime, currentTime);

        // Assert
        Assert.True(canReschedule);
    }

    [Fact]
    public void CanReschedule_Should_Return_False_When_Not_Allowed_By_Policy()
    {
        // Arrange
        var policy = BookingPolicy.Create(2, 90, 24, 50, false, 24, true, 20); // Rescheduling not allowed
        var bookingTime = DateTime.UtcNow.AddDays(2);
        var currentTime = DateTime.UtcNow;

        // Act
        var canReschedule = policy.CanReschedule(bookingTime, currentTime);

        // Assert
        Assert.False(canReschedule);
    }

    [Fact]
    public void CanReschedule_Should_Return_False_When_Outside_Window()
    {
        // Arrange
        var policy = BookingPolicy.Default; // 24 hour window
        var bookingTime = DateTime.UtcNow.AddHours(12);
        var currentTime = DateTime.UtcNow;

        // Act
        var canReschedule = policy.CanReschedule(bookingTime, currentTime);

        // Assert
        Assert.False(canReschedule);
    }

    [Fact]
    public void IsWithinBookingWindow_Should_Return_True_For_Valid_Time()
    {
        // Arrange
        var policy = BookingPolicy.Default; // 2 hours min, 90 days max
        var bookingTime = DateTime.UtcNow.AddDays(30);
        var currentTime = DateTime.UtcNow;

        // Act
        var isValid = policy.IsWithinBookingWindow(bookingTime, currentTime);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsWithinBookingWindow_Should_Return_False_When_Too_Soon()
    {
        // Arrange
        var policy = BookingPolicy.Default; // 2 hours min
        var bookingTime = DateTime.UtcNow.AddMinutes(30);
        var currentTime = DateTime.UtcNow;

        // Act
        var isValid = policy.IsWithinBookingWindow(bookingTime, currentTime);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsWithinBookingWindow_Should_Return_False_When_Too_Far()
    {
        // Arrange
        var policy = BookingPolicy.Default; // 90 days max
        var bookingTime = DateTime.UtcNow.AddDays(120);
        var currentTime = DateTime.UtcNow;

        // Act
        var isValid = policy.IsWithinBookingWindow(bookingTime, currentTime);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void TwoPolicies_With_Same_Values_Should_Be_Equal()
    {
        // Arrange
        var policy1 = BookingPolicy.Create(2, 90, 24, 50, true, 24, true, 20);
        var policy2 = BookingPolicy.Create(2, 90, 24, 50, true, 24, true, 20);

        // Act & Assert
        Assert.Equal(policy1, policy2);
        Assert.True(policy1 == policy2);
        Assert.False(policy1 != policy2);
    }

    [Fact]
    public void TwoPolicies_With_Different_Values_Should_Not_Be_Equal()
    {
        // Arrange
        var policy1 = BookingPolicy.Default;
        var policy2 = BookingPolicy.Flexible;

        // Act & Assert
        Assert.NotEqual(policy1, policy2);
        Assert.False(policy1 == policy2);
        Assert.True(policy1 != policy2);
    }
}
