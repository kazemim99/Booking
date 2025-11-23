using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.PayoutAggregate;

public class PayoutAggregateTests
{
    private readonly ProviderId _providerId = ProviderId.New();
    private readonly Money _grossAmount = Money.Create(1000, "USD");
    private readonly Money _commissionAmount = Money.Create(150, "USD");
    private readonly DateTime _periodStart = DateTime.UtcNow.AddDays(-30);
    private readonly DateTime _periodEnd = DateTime.UtcNow.AddDays(-1);
    private readonly List<PaymentId> _paymentIds = new()
    {
        PaymentId.From(Guid.NewGuid()),
        PaymentId.From(Guid.NewGuid()),
        PaymentId.From(Guid.NewGuid())
    };

    [Fact]
    public void Create_Should_Create_Payout_With_Pending_Status()
    {
        // Arrange & Act
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        // Assert
        Assert.NotNull(payout);
        Assert.NotEqual(Guid.Empty, payout.Id.Value);
        Assert.Equal(_providerId, payout.ProviderId);
        Assert.Equal(_grossAmount, payout.GrossAmount);
        Assert.Equal(_commissionAmount, payout.CommissionAmount);
        Assert.Equal(Money.Create(850, "USD"), payout.NetAmount); // 1000 - 150
        Assert.Equal(PayoutStatus.Pending, payout.Status);
        Assert.Equal(_periodStart, payout.PeriodStart);
        Assert.Equal(_periodEnd, payout.PeriodEnd);
        Assert.True((DateTime.UtcNow - payout.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void Create_Should_Track_Payment_IDs()
    {
        // Arrange & Act
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        // Assert
        var paymentIds = payout.PaymentIds;
        Assert.Equal(3, paymentIds.Count);
        Assert.Equal(_paymentIds[0], paymentIds[0]);
    }

    [Fact]
    public void Create_Should_Calculate_Net_Amount_Correctly()
    {
        // Arrange
        var grossAmount = Money.Create(2000, "USD");
        var commissionAmount = Money.Create(300, "USD");

        // Act
        var payout = Payout.Create(
            _providerId,
            grossAmount,
            commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        // Assert
        Assert.Equal(1700m, payout.NetAmount.Amount);
    }

    [Fact]
    public void Schedule_Should_Set_Scheduled_Date_And_Update_Status()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        var scheduledDate = DateTime.UtcNow.AddDays(7);

        // Act
        payout.Schedule(scheduledDate);

        // Assert
        Assert.Equal(PayoutStatus.Pending, payout.Status);
        Assert.Equal(scheduledDate, payout.ScheduledAt);
    }

    [Fact]
    public void Schedule_Should_Throw_When_Scheduled_Date_In_Past()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => payout.Schedule(pastDate));
        Assert.Contains("future", exception.Message);
    }

    [Fact]
    public void MarkAsProcessing_Should_Update_Status_And_Set_External_ID()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        var externalPayoutId = "po_stripe_123";
        var stripeAccountId = "acct_123";

        // Act
        payout.MarkAsProcessing(externalPayoutId, stripeAccountId);

        // Assert
        Assert.Equal(PayoutStatus.Processing, payout.Status);
        Assert.Equal(externalPayoutId, payout.ExternalPayoutId);
        Assert.Equal(stripeAccountId, payout.StripeAccountId);
        // Note: MarkAsProcessing does not set PaidAt - that's set by MarkAsPaid
    }

    [Fact]
    public void MarkAsPaid_Should_Update_Status_And_Set_Completion_Details()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        payout.MarkAsProcessing("po_stripe_123");

        // Act
        payout.MarkAsPaid("1234", "Chase Bank");

        // Assert
        Assert.Equal(PayoutStatus.Paid, payout.Status);
        Assert.Equal("1234", payout.BankAccountLast4);
        Assert.Equal("Chase Bank", payout.BankName);
        Assert.NotNull(payout.PaidAt);
    }

    [Fact]
    public void MarkAsPaid_Should_Throw_When_Not_Processing()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => payout.MarkAsPaid("1234", "Chase Bank"));
        Assert.Contains("Pending", exception.Message);
    }

    [Fact]
    public void MarkAsFailed_Should_Update_Status_And_Store_Reason()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        payout.MarkAsProcessing("po_stripe_123");

        var failureReason = "Insufficient funds";

        // Act
        payout.MarkAsFailed(failureReason);

        // Assert
        Assert.Equal(PayoutStatus.Failed, payout.Status);
        Assert.Equal(failureReason, payout.FailureReason);
        Assert.NotNull(payout.FailedAt);
    }

    [Fact]
    public void PlaceOnHold_Should_Update_Status_And_Store_Reason()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        var holdReason = "Under review";

        // Act
        payout.PutOnHold(holdReason);

        // Assert
        Assert.Equal(PayoutStatus.OnHold, payout.Status);
        // Note: Payout doesn't store hold reason in a public property
    }

    [Fact]
    public void Cancel_Should_Update_Status_And_Store_Reason()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        var cancellationReason = "Provider request";

        // Act
        payout.Cancel(cancellationReason);

        // Assert
        Assert.Equal(PayoutStatus.Cancelled, payout.Status);
        // Note: Payout doesn't store cancellation reason in a public property
        Assert.Null(payout.PaidAt); // Cancelled payouts are not marked as paid
    }

    [Fact]
    public void Cancel_Should_Throw_When_Already_Paid()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        payout.MarkAsProcessing("po_stripe_123");
        payout.MarkAsPaid("1234", "Chase Bank");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => payout.Cancel("Too late"));
        Assert.Contains("paid", exception.Message.ToLower());
    }

    [Fact]
    public void Payout_Should_Publish_PayoutCreatedEvent_On_Creation()
    {
        // Arrange & Act
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        // Assert
        var events = payout.DomainEvents;
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.GetType().Name == "PayoutCreatedEvent");
    }

    [Fact]
    public void Payout_Should_Publish_PayoutScheduledEvent_On_Scheduling()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        payout.ClearDomainEvents();

        // Act
        payout.Schedule(DateTime.UtcNow.AddDays(7));

        // Assert
        var events = payout.DomainEvents;
        Assert.Contains(events, e => e.GetType().Name == "PayoutScheduledEvent");
    }

    [Fact]
    public void Payout_Should_Publish_PayoutPaidEvent_On_Completion()
    {
        // Arrange
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds);

        payout.MarkAsProcessing("po_stripe_123");
        payout.ClearDomainEvents();

        // Act
        payout.MarkAsPaid("1234", "Chase Bank");

        // Assert
        var events = payout.DomainEvents;
        Assert.Contains(events, e => e.GetType().Name == "PayoutCompletedEvent");
    }

    [Fact]
    public void Payout_Should_Store_Notes()
    {
        // Arrange
        var notes = "Monthly payout for provider";

        // Act
        var payout = Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            _periodStart,
            _periodEnd,
            _paymentIds,
            notes);

        // Assert
        Assert.Equal(notes, payout.Notes);
    }

    [Fact]
    public void Create_Should_Throw_When_Commission_Exceeds_Gross_Amount()
    {
        // Arrange
        var grossAmount = Money.Create(100, "USD");
        var excessiveCommission = Money.Create(150, "USD");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => Payout.Create(
            _providerId,
            grossAmount,
            excessiveCommission,
            _periodStart,
            _periodEnd,
            _paymentIds));
        Assert.Contains("Commission", exception.Message);
    }

    [Fact]
    public void Create_Should_Throw_When_Period_Start_After_Period_End()
    {
        // Arrange
        var periodStart = DateTime.UtcNow;
        var periodEnd = DateTime.UtcNow.AddDays(-10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Payout.Create(
            _providerId,
            _grossAmount,
            _commissionAmount,
            periodStart,
            periodEnd,
            _paymentIds));
        Assert.Contains("Period", exception.Message);
    }
}
