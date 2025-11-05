using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.PaymentAggregate;

public class PaymentAggregateTests
{
    private readonly BookingId _bookingId = BookingId.From(Guid.NewGuid());
    private readonly UserId _customerId = UserId.From(Guid.NewGuid());
    private readonly ProviderId _providerId = ProviderId.New();
    private readonly Money _amount = Money.Create(100, "USD");
    private readonly RefundPolicy _refundPolicy = RefundPolicy.Moderate;

    [Fact]
    public void CreateForBooking_Should_Create_Payment_With_Pending_Status()
    {
        // Arrange & Act
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        // Assert
        Assert.NotNull(payment);
        Assert.NotEqual(Guid.Empty, payment.Id.Value);
        Assert.Equal(_bookingId, payment.BookingId);
        Assert.Equal(_customerId, payment.CustomerId);
        Assert.Equal(_providerId, payment.ProviderId);
        Assert.Equal(_amount, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(PaymentMethod.Card, payment.Method);
        Assert.Equal(Money.Zero("USD"), payment.PaidAmount);
        Assert.Equal(Money.Zero("USD"), payment.RefundedAmount);
        Assert.True((DateTime.UtcNow - payment.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void Authorize_Should_Change_Status_To_Authorized()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        var paymentIntentId = "pi_test_123";

        // Act
        payment.Authorize(paymentIntentId, "pm_test_456");

        // Assert
        Assert.Equal(PaymentStatus.PartiallyPaid, payment.Status);
        Assert.Equal(paymentIntentId, payment.PaymentIntentId);
        Assert.Single(payment.Transactions);
        Assert.Equal(TransactionType.Authorization, payment.Transactions.First().Type);
    }

    [Fact]
    public void Capture_Authorized_Payment_Should_Change_Status_To_Paid()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        payment.Authorize("pi_test_123", "pm_test_456");

        // Act
        payment.Capture("ch_test_789");

        // Assert
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(_amount, payment.PaidAmount);
        Assert.Equal(2, payment.Transactions.Count);
        Assert.Contains(payment.Transactions, t => t.Type == TransactionType.Capture);
    }

    [Fact]
    public void Capture_Should_Throw_When_Payment_Not_Authorized()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => payment.Capture("ch_test_789"));
        Assert.Contains("can only be captured when authorized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessCharge_Should_Create_Paid_Payment_Directly()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        // Act
        payment.ProcessCharge("pi_test_123", "pm_test_456");

        // Assert
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(_amount, payment.PaidAmount);
        Assert.Single(payment.Transactions);
        Assert.Equal(TransactionType.Charge, payment.Transactions.First().Type);
    }

    [Fact]
    public void Refund_Should_Update_Status_To_Refunded_For_Full_Refund()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        payment.ProcessCharge("pi_test_123", "pm_test_456");

        var refundAmount = _amount;
        var refundId = "re_test_123";

        // Act
        payment.Refund(refundAmount, refundId, RefundReason.CustomerCancellation, "Full refund");

        // Assert
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(refundAmount, payment.RefundedAmount);
        Assert.Equal(2, payment.Transactions.Count);
        Assert.Contains(payment.Transactions, t => t.Type == TransactionType.Refund);
    }

    [Fact]
    public void Refund_Should_Update_Status_To_PartiallyRefunded_For_Partial_Refund()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        payment.ProcessCharge("pi_test_123", "pm_test_456");

        var refundAmount = Money.Create(50, "USD");
        var refundId = "re_test_123";

        // Act
        payment.Refund(refundAmount, refundId, RefundReason.CustomerCancellation, "Partial refund");

        // Assert
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(refundAmount, payment.RefundedAmount);
    }

    [Fact]
    public void Refund_Should_Throw_When_Payment_Not_Paid()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        var refundAmount = Money.Create(50, "USD");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            payment.Refund(refundAmount, "re_test_123", RefundReason.CustomerCancellation));
        Assert.Contains("can only be refunded when paid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Refund_Should_Throw_When_Refund_Amount_Exceeds_Paid_Amount()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        payment.ProcessCharge("pi_test_123", "pm_test_456");

        var excessiveRefundAmount = Money.Create(150, "USD");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            payment.Refund(excessiveRefundAmount, "re_test_123", RefundReason.CustomerCancellation));
        Assert.Contains("refund amount", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Refund_Should_Throw_When_Currency_Mismatch()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        payment.ProcessCharge("pi_test_123", "pm_test_456");

        var differentCurrencyRefund = Money.Create(50, "EUR");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            payment.Refund(differentCurrencyRefund, "re_test_123", RefundReason.CustomerCancellation));
        Assert.Contains("currency", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Multiple_Partial_Refunds_Should_Track_Total_Refunded_Amount()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(100, "USD"),
            PaymentMethod.Card
            );

        payment.ProcessCharge("pi_test_123", "pm_test_456");

        // Act
        payment.Refund(Money.Create(30, "USD"), "re_test_1", RefundReason.CustomerCancellation);
        payment.Refund(Money.Create(20, "USD"), "re_test_2", RefundReason.CustomerCancellation);

        // Assert
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(50m, payment.RefundedAmount.Amount);
        Assert.Equal(3, payment.Transactions.Count); // 1 charge + 2 refunds
        Assert.Equal(2, payment.Transactions.Count(t => t.Type == TransactionType.Refund));
    }

    [Fact]
    public void MarkAsFailed_Should_Change_Status_To_Failed()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        // Act
        payment.MarkAsFailed("Card declined");

        // Assert
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public void Payment_Should_Publish_PaymentCreatedEvent_On_Creation()
    {
        // Arrange & Act
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        // Assert
        var events = payment.DomainEvents;
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.GetType().Name == "PaymentCreatedEvent");
    }

    [Fact]
    public void Payment_Should_Publish_PaymentAuthorizedEvent_On_Authorization()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card
            );

        payment.ClearDomainEvents();

        // Act
        payment.Authorize("pi_test_123", "pm_test_456");

        // Assert
        var events = payment.DomainEvents;
        Assert.Contains(events, e => e.GetType().Name == "PaymentAuthorizedEvent");
    }

    [Fact]
    public void Payment_Should_Store_Metadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "ip_address", "192.168.1.1" },
            { "device", "mobile" },
            { "session_id", "sess_123" }
        };

        // Act
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.Card,
            metadata: metadata);

        // Assert
        Assert.NotNull(payment.Metadata);
        Assert.Equal(3, payment.Metadata.Count);
        Assert.Equal("192.168.1.1", payment.Metadata["ip_address"]);
    }
}
