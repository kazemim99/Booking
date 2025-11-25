using Booksy.Core.Domain.Enums;
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
            PaymentMethod.CreditCard
            );

        // Assert
        Assert.NotNull(payment);
        Assert.NotEqual(Guid.Empty, payment.Id.Value);
        Assert.Equal(_bookingId, payment.BookingId);
        Assert.Equal(_customerId, payment.CustomerId);
        Assert.Equal(_providerId, payment.ProviderId);
        Assert.Equal(_amount, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(PaymentMethod.CreditCard, payment.Method);
        Assert.Equal(Money.Zero("USD"), payment.PaidAmount);
        Assert.Equal(Money.Zero("USD"), payment.RefundedAmount);
        Assert.True((DateTime.UtcNow - payment.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void Authorize_Should_Store_Authorization_Details()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            _amount,
            PaymentMethod.CreditCard
            );

        var paymentIntentId = "pi_test_123";

        // Act
        payment.Authorize(paymentIntentId, "pm_test_456");

        // Assert
        // Status remains Pending until captured (authorization only holds funds)
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(paymentIntentId, payment.PaymentIntentId);
        Assert.Single(payment.Transactions);
        Assert.Equal(TransactionType.Authorization, payment.Transactions[0].Type);
        Assert.NotNull(payment.AuthorizedAt);
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
            PaymentMethod.CreditCard
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
            PaymentMethod.CreditCard
            );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => payment.Capture("ch_test_789"));
        Assert.Contains("authorized", exception.Message);
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
            PaymentMethod.CreditCard
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
            PaymentMethod.CreditCard
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
            PaymentMethod.CreditCard
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
            PaymentMethod.CreditCard
            );

        var refundAmount = Money.Create(50, "USD");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            payment.Refund(refundAmount, "re_test_123", RefundReason.CustomerCancellation));
        Assert.Contains("Pending", exception.Message);
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
            PaymentMethod.CreditCard
            );

        payment.ProcessCharge("pi_test_123", "pm_test_456");

        var excessiveRefundAmount = Money.Create(150, "USD");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            payment.Refund(excessiveRefundAmount, "re_test_123", RefundReason.CustomerCancellation));
        Assert.Contains("exceed", exception.Message.ToLower());
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
            PaymentMethod.CreditCard
            );

        payment.ProcessCharge("pi_test_123", "pm_test_456");

        var differentCurrencyRefund = Money.Create(50, "EUR");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            payment.Refund(differentCurrencyRefund, "re_test_123", RefundReason.CustomerCancellation));
        Assert.Contains("currency", exception.Message.ToLower());
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
            PaymentMethod.CreditCard
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
            PaymentMethod.CreditCard
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
            PaymentMethod.CreditCard
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
            PaymentMethod.CreditCard
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
            PaymentMethod.CreditCard,
            metadata: metadata);

        // Assert
        Assert.NotNull(payment.Metadata);
        Assert.Equal(3, payment.Metadata.Count);
        Assert.Equal("192.168.1.1", payment.Metadata["ip_address"]);
    }

    // ========================================
    // ZarinPal-Specific Tests
    // ========================================

    [Fact]
    public void RecordPaymentRequest_Should_Store_Authority_And_PaymentUrl()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        var authority = "A00000000000000000000000000000123456";
        var paymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/" + authority;

        // Act
        payment.RecordPaymentRequest(authority, paymentUrl);

        // Assert
        Assert.Equal(authority, payment.Authority);
        Assert.Equal(paymentUrl, payment.PaymentUrl);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Single(payment.Transactions);
        Assert.Equal(TransactionType.PaymentRequest, payment.Transactions.First().Type);
    }

    [Fact]
    public void RecordPaymentRequest_Should_Publish_PaymentRequestCreatedEvent()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        payment.ClearDomainEvents();

        var authority = "A00000000000000000000000000000123456";
        var paymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/" + authority;

        // Act
        payment.RecordPaymentRequest(authority, paymentUrl);

        // Assert
        var events = payment.DomainEvents;
        Assert.Contains(events, e => e.GetType().Name == "PaymentRequestCreatedEvent");
    }

    [Fact]
    public void RecordPaymentRequest_Should_Throw_When_Authority_Is_Empty()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            payment.RecordPaymentRequest("", "https://zarinpal.com/pg/StartPay/123"));
        Assert.Contains("Authority", exception.Message);
    }

    [Fact]
    public void RecordPaymentRequest_Should_Throw_When_PaymentUrl_Is_Empty()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            payment.RecordPaymentRequest("A00000000000000000000000000000123456", ""));
        Assert.Contains("Payment URL", exception.Message);
    }

    [Fact]
    public void VerifyPayment_Should_Update_Status_To_Paid_And_Store_Details()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        payment.RecordPaymentRequest("A00000000000000000000000000000123456", "https://zarinpal.com/pg/StartPay/123");

        var refNumber = "123456789";
        var cardPan = "6274****1234";
        var fee = 500m;

        // Act
        payment.VerifyPayment(refNumber, cardPan, fee);

        // Assert
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(refNumber, payment.RefNumber);
        Assert.Equal(cardPan, payment.CardPan);
        Assert.NotNull(payment.Fee);
        Assert.Equal(fee, payment.Fee.Amount);
        Assert.Equal(payment.Amount, payment.PaidAmount);
        Assert.Equal(2, payment.Transactions.Count);
        Assert.Contains(payment.Transactions, t => t.Type == TransactionType.Verification);
    }

    [Fact]
    public void VerifyPayment_Should_Publish_PaymentVerifiedEvent()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        payment.RecordPaymentRequest("A00000000000000000000000000000123456", "https://zarinpal.com/pg/StartPay/123");
        payment.ClearDomainEvents();

        // Act
        payment.VerifyPayment("123456789", "6274****1234", 500m);

        // Assert
        var events = payment.DomainEvents;
        Assert.Contains(events, e => e.GetType().Name == "PaymentVerifiedEvent");
    }

    [Fact]
    public void VerifyPayment_Should_Throw_When_RefNumber_Is_Empty()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        payment.RecordPaymentRequest("A00000000000000000000000000000123456", "https://zarinpal.com/pg/StartPay/123");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            payment.VerifyPayment("", "6274****1234"));
        Assert.Contains("Reference number", exception.Message);
    }

    [Fact]
    public void VerifyPayment_Should_Work_Without_CardPan_And_Fee()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        payment.RecordPaymentRequest("A00000000000000000000000000000123456", "https://zarinpal.com/pg/StartPay/123");

        // Act
        payment.VerifyPayment("123456789");

        // Assert
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("123456789", payment.RefNumber);
        Assert.Null(payment.CardPan);
        Assert.Null(payment.Fee);
    }

    [Fact]
    public void MarkPaymentRequestAsFailed_Should_Update_Status_And_Store_Error()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        payment.RecordPaymentRequest("A00000000000000000000000000000123456", "https://zarinpal.com/pg/StartPay/123");

        var errorCode = "-11";
        var errorMessage = "Invalid merchant credentials";

        // Act
        payment.MarkPaymentRequestAsFailed(errorCode, errorMessage);

        // Assert
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Contains(errorCode, payment.FailureReason);
        Assert.Contains(errorMessage, payment.FailureReason);
        Assert.NotNull(payment.FailedAt);
        Assert.Equal(2, payment.Transactions.Count);
        Assert.Contains(payment.Transactions, t => t.Type == TransactionType.Failed);
    }

    [Fact]
    public void GetNetAmount_Should_Calculate_Amount_Minus_Fee_And_Refunds()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        payment.RecordPaymentRequest("A00000000000000000000000000000123456", "https://zarinpal.com/pg/StartPay/123");
        payment.VerifyPayment("123456789", "6274****1234", 500m);
        payment.Refund(Money.Create(5000, "IRR"), "refund_123", RefundReason.CustomerCancellation);

        // Act
        var netAmount = payment.GetNetAmount();

        // Assert
        // 50000 (paid) - 5000 (refund) - 500 (fee) = 44500
        Assert.Equal(44500m, netAmount.Amount);
    }

    [Fact]
    public void GetNetAmount_Should_Calculate_Without_Fee_When_Null()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal);

        payment.RecordPaymentRequest("A00000000000000000000000000000123456", "https://zarinpal.com/pg/StartPay/123");
        payment.VerifyPayment("123456789"); // No fee
        payment.Refund(Money.Create(5000, "IRR"), "refund_123", RefundReason.CustomerCancellation);

        // Act
        var netAmount = payment.GetNetAmount();

        // Assert
        // 50000 (paid) - 5000 (refund) = 45000
        Assert.Equal(45000m, netAmount.Amount);
    }

    [Fact]
    public void ZarinPal_Payment_Full_Flow_Should_Work_Correctly()
    {
        // Arrange
        var payment = Payment.CreateForBooking(
            _bookingId,
            _customerId,
            _providerId,
            Money.Create(50000, "IRR"),
            PaymentMethod.ZarinPal,
            PaymentProvider.ZarinPal,
            "Test booking payment");

        // Act & Assert - Step 1: Record payment request
        payment.RecordPaymentRequest("A00000000000000000000000000000123456", "https://sandbox.zarinpal.com/pg/StartPay/A123");
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("A00000000000000000000000000000123456", payment.Authority);

        // Act & Assert - Step 2: Verify payment
        payment.VerifyPayment("987654321", "6274****5678", 750m);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("987654321", payment.RefNumber);
        Assert.Equal(750m, payment.Fee.Amount);

        // Act & Assert - Step 3: Partial refund
        payment.Refund(Money.Create(10000, "IRR"), "refund_001", RefundReason.CustomerCancellation, "Customer request");
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(10000m, payment.RefundedAmount.Amount);

        // Assert final state
        Assert.Equal(39250m, payment.GetNetAmount().Amount); // 50000 - 10000 - 750
        Assert.Equal(3, payment.Transactions.Count); // Request, Verification, Refund
    }
}
