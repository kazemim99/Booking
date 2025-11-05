using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ValueObjects;

public class PaymentInfoTests
{
    [Fact]
    public void Create_Should_Initialize_Payment_With_Pending_Status()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");

        // Act
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount);

        // Assert
        Assert.NotNull(paymentInfo);
        Assert.Equal(100m, paymentInfo.TotalAmount.Amount);
        Assert.Equal(20m, paymentInfo.DepositAmount.Amount);
        Assert.Equal(0m, paymentInfo.PaidAmount.Amount);
        Assert.Equal(0m, paymentInfo.RefundedAmount.Amount);
        Assert.Equal(PaymentStatus.Pending, paymentInfo.Status);
        Assert.Null(paymentInfo.PaymentIntentId);
        Assert.Null(paymentInfo.PaidAt);
    }

    [Fact]
    public void CreateWithNoDeposit_Should_Initialize_With_Zero_Deposit()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");

        // Act
        var paymentInfo = PaymentInfo.CreateWithNoDeposit(totalAmount);

        // Assert
        Assert.Equal(0m, paymentInfo.DepositAmount.Amount);
        Assert.Equal(PaymentStatus.Pending, paymentInfo.Status);
    }

    [Fact]
    public void WithDepositPaid_Should_Update_Status_To_PartiallyPaid()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount);
        var paymentIntentId = "pi_deposit123";

        // Act
        var updated = paymentInfo.WithDepositPaid(paymentIntentId);

        // Assert
        Assert.Equal(PaymentStatus.PartiallyPaid, updated.Status);
        Assert.Equal(20m, updated.PaidAmount.Amount);
        Assert.Equal(paymentIntentId, updated.DepositPaymentIntentId);
        Assert.NotNull(updated.PaidAt);
    }

    [Fact]
    public void WithDepositPaid_Should_Mark_As_Paid_When_Deposit_Equals_Total()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(100m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount);

        // Act
        var updated = paymentInfo.WithDepositPaid("pi_full");

        // Assert
        Assert.Equal(PaymentStatus.Paid, updated.Status);
        Assert.True(updated.IsFullyPaid());
    }

    [Fact]
    public void WithDepositPaid_Should_Throw_When_No_Deposit_Required()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var paymentInfo = PaymentInfo.CreateWithNoDeposit(totalAmount);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            paymentInfo.WithDepositPaid("pi_test"));
    }

    [Fact]
    public void WithFullPayment_Should_Update_Status_To_Paid()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount);
        var paymentIntentId = "pi_full123";

        // Act
        var updated = paymentInfo.WithFullPayment(paymentIntentId);

        // Assert
        Assert.Equal(PaymentStatus.Paid, updated.Status);
        Assert.Equal(100m, updated.PaidAmount.Amount);
        Assert.Equal(paymentIntentId, updated.PaymentIntentId);
        Assert.True(updated.IsFullyPaid());
        Assert.NotNull(updated.PaidAt);
    }

    [Fact]
    public void WithPartialPayment_Should_Update_Paid_Amount()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount)
            .WithDepositPaid("pi_deposit");
        var additionalPayment = Money.Create(30m, "USD");

        // Act
        var updated = paymentInfo.WithPartialPayment(additionalPayment, "pi_partial");

        // Assert
        Assert.Equal(50m, updated.PaidAmount.Amount); // 20 + 30
        Assert.Equal(PaymentStatus.PartiallyPaid, updated.Status);
    }

    [Fact]
    public void WithPartialPayment_Should_Mark_As_Paid_When_Reaching_Total()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount)
            .WithDepositPaid("pi_deposit");
        var remainingPayment = Money.Create(80m, "USD");

        // Act
        var updated = paymentInfo.WithPartialPayment(remainingPayment, "pi_remaining");

        // Assert
        Assert.Equal(100m, updated.PaidAmount.Amount);
        Assert.Equal(PaymentStatus.Paid, updated.Status);
        Assert.True(updated.IsFullyPaid());
    }

    [Fact]
    public void WithPartialPayment_Should_Throw_When_Exceeding_Total()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var paymentInfo = PaymentInfo.CreateWithNoDeposit(totalAmount);
        var excessPayment = Money.Create(150m, "USD");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            paymentInfo.WithPartialPayment(excessPayment, "pi_test"));
    }

    [Fact]
    public void WithRefund_Should_Update_Refunded_Amount_And_Status()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var paymentInfo = PaymentInfo.CreateWithNoDeposit(totalAmount)
            .WithFullPayment("pi_full");
        var refundAmount = Money.Create(50m, "USD");
        var refundId = "re_test123";

        // Act
        var updated = paymentInfo.WithRefund(refundAmount, refundId);

        // Assert
        Assert.Equal(50m, updated.RefundedAmount.Amount);
        Assert.Equal(PaymentStatus.PartiallyRefunded, updated.Status);
        Assert.Equal(refundId, updated.RefundId);
        Assert.NotNull(updated.RefundedAt);
    }

    [Fact]
    public void WithRefund_Should_Mark_As_Fully_Refunded_When_Refunding_Total()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var paymentInfo = PaymentInfo.CreateWithNoDeposit(totalAmount)
            .WithFullPayment("pi_full");
        var fullRefund = Money.Create(100m, "USD");

        // Act
        var updated = paymentInfo.WithRefund(fullRefund, "re_full");

        // Assert
        Assert.Equal(100m, updated.RefundedAmount.Amount);
        Assert.Equal(PaymentStatus.Refunded, updated.Status);
    }

    [Fact]
    public void WithRefund_Should_Throw_When_Exceeding_Paid_Amount()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount)
            .WithDepositPaid("pi_deposit");
        var excessRefund = Money.Create(50m, "USD");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            paymentInfo.WithRefund(excessRefund, "re_test"));
    }

    [Fact]
    public void MarkAsFailed_Should_Update_Status()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var paymentInfo = PaymentInfo.CreateWithNoDeposit(totalAmount);

        // Act
        var updated = paymentInfo.MarkAsFailed();

        // Assert
        Assert.Equal(PaymentStatus.Failed, updated.Status);
    }

    [Fact]
    public void GetRemainingAmount_Should_Return_Unpaid_Balance()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount)
            .WithDepositPaid("pi_deposit");

        // Act
        var remaining = paymentInfo.GetRemainingAmount();

        // Assert
        Assert.Equal(80m, remaining.Amount);
        Assert.Equal("USD", remaining.Currency);
    }

    [Fact]
    public void GetRemainingAmount_Should_Return_Zero_When_Fully_Paid()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var paymentInfo = PaymentInfo.CreateWithNoDeposit(totalAmount)
            .WithFullPayment("pi_full");

        // Act
        var remaining = paymentInfo.GetRemainingAmount();

        // Assert
        Assert.Equal(0m, remaining.Amount);
    }

    [Fact]
    public void IsDepositPaid_Should_Return_True_After_Deposit_Payment()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount)
            .WithDepositPaid("pi_deposit");

        // Act & Assert
        Assert.True(paymentInfo.IsDepositPaid());
    }

    [Fact]
    public void IsDepositPaid_Should_Return_False_Before_Payment()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount);

        // Act & Assert
        Assert.False(paymentInfo.IsDepositPaid());
    }

    [Fact]
    public void IsFullyPaid_Should_Return_True_After_Full_Payment()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var paymentInfo = PaymentInfo.CreateWithNoDeposit(totalAmount)
            .WithFullPayment("pi_full");

        // Act & Assert
        Assert.True(paymentInfo.IsFullyPaid());
    }

    [Fact]
    public void IsFullyPaid_Should_Return_False_When_Partially_Paid()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "USD");
        var paymentInfo = PaymentInfo.Create(totalAmount, depositAmount)
            .WithDepositPaid("pi_deposit");

        // Act & Assert
        Assert.False(paymentInfo.IsFullyPaid());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_Should_Throw_When_Total_Amount_Is_Invalid(decimal amount)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            PaymentInfo.Create(Money.Create(amount, "USD"), Money.Create(0, "USD")));
    }

    [Fact]
    public void Create_Should_Throw_When_Deposit_Exceeds_Total()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var excessDeposit = Money.Create(150m, "USD");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            PaymentInfo.Create(totalAmount, excessDeposit));
    }

    [Fact]
    public void Create_Should_Throw_When_Currencies_Dont_Match()
    {
        // Arrange
        var totalAmount = Money.Create(100m, "USD");
        var depositAmount = Money.Create(20m, "EUR");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            PaymentInfo.Create(totalAmount, depositAmount));
    }
}
