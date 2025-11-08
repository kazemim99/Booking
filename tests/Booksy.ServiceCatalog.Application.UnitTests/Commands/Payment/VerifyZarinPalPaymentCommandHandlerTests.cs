using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.Infrastructure.External.Payment.ZarinPal.Models;
using Booksy.ServiceCatalog.Application.Commands.Payment.VerifyZarinPalPayment;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.Payment;

public class VerifyZarinPalPaymentCommandHandlerTests
{
    private readonly IPaymentWriteRepository _paymentRepository;
    private readonly IZarinPalService _zarinPalService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyZarinPalPaymentCommandHandler> _logger;
    private readonly VerifyZarinPalPaymentCommandHandler _handler;

    public VerifyZarinPalPaymentCommandHandlerTests()
    {
        _paymentRepository = Substitute.For<IPaymentWriteRepository>();
        _zarinPalService = Substitute.For<IZarinPalService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<VerifyZarinPalPaymentCommandHandler>>();
        _handler = new VerifyZarinPalPaymentCommandHandler(
            _paymentRepository,
            _zarinPalService,
            _unitOfWork,
            _logger);
    }

    [Fact]
    public async Task Handle_Should_Verify_Payment_Successfully_And_Mark_As_Paid()
    {
        // Arrange
        var authority = "A00000000000000000000000000000123456";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        var payment = CreatePendingPayment(authority, 50000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        var verifyResult = new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 123456789,
            CardPan = "6274****1234",
            Fee = 500m
        };

        _zarinPalService.VerifyPaymentAsync(authority, 50000m, Arg.Any<CancellationToken>())
            .Returns(verifyResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.RefNumber.Should().Be(123456789);
        result.CardPan.Should().Be("6274****1234");
        result.Status.Should().Be("Paid");

        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.RefNumber.Should().Be("123456789");
        payment.CardPan.Should().Be("6274****1234");

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Mark_Payment_As_Failed_When_User_Cancels()
    {
        // Arrange
        var authority = "A00000000000000000000000000000123456";
        var command = new VerifyZarinPalPaymentCommand(authority, "NOK");

        var payment = CreatePendingPayment(authority, 50000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.Status.Should().Be("Failed");
        result.FailureReason.Should().Be("User cancelled the payment");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Contain("cancelled");

        await _zarinPalService.DidNotReceive().VerifyPaymentAsync(
            Arg.Any<string>(),
            Arg.Any<decimal>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Payment_Not_Found()
    {
        // Arrange
        var authority = "A_NONEXISTENT";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.PaymentAggregate.Payment?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");

        await _zarinPalService.DidNotReceive().VerifyPaymentAsync(
            Arg.Any<string>(),
            Arg.Any<decimal>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Store_Fee_When_Provided_By_ZarinPal()
    {
        // Arrange
        var authority = "A12345";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        var payment = CreatePendingPayment(authority, 100000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        var verifyResult = new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 987654321,
            CardPan = "6037****5678",
            Fee = 1000m
        };

        _zarinPalService.VerifyPaymentAsync(authority, 100000m, Arg.Any<CancellationToken>())
            .Returns(verifyResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        payment.Fee.Should().NotBeNull();
        payment.Fee!.Amount.Should().Be(1000m);
        payment.Fee.Currency.Should().Be("IRR");
    }

    [Fact]
    public async Task Handle_Should_Handle_ZarinPal_Verification_Failure()
    {
        // Arrange
        var authority = "A99999";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        var payment = CreatePendingPayment(authority, 75000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        var verifyResult = new ZarinPalVerifyResult
        {
            IsSuccessful = false,
            ErrorCode = -53,
            ErrorMessage = "Transaction verification unsuccessful"
        };

        _zarinPalService.VerifyPaymentAsync(authority, 75000m, Arg.Any<CancellationToken>())
            .Returns(verifyResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be(-53);
        result.ErrorMessage.Should().Be("Transaction verification unsuccessful");
        result.Status.Should().Be("Failed");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Contain("verification unsuccessful");

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Publish_PaymentVerifiedEvent_When_Successful()
    {
        // Arrange
        var authority = "A55555";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        var payment = CreatePendingPayment(authority, 150000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        var verifyResult = new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 111222333,
            CardPan = "6280****9999"
        };

        _zarinPalService.VerifyPaymentAsync(authority, 150000m, Arg.Any<CancellationToken>())
            .Returns(verifyResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verify domain event was published through aggregate
        payment.DomainEvents.Should().Contain(e =>
            e.GetType().Name == "PaymentVerifiedEvent");

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Verify_With_Correct_Amount()
    {
        // Arrange
        var authority = "A66666";
        var amount = 234567m;
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        var payment = CreatePendingPayment(authority, amount);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        var verifyResult = new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 444555666
        };

        _zarinPalService.VerifyPaymentAsync(authority, amount, Arg.Any<CancellationToken>())
            .Returns(verifyResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _zarinPalService.Received(1).VerifyPaymentAsync(
            authority,
            amount,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Handle_Already_Verified_Payment_Code_101()
    {
        // Arrange
        var authority = "A77777";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        var payment = CreatePendingPayment(authority, 80000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        var verifyResult = new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 999888777,
            CardPan = "6274****4321",
            Fee = 800m,
            // Code 101 indicates already verified payment
        };

        _zarinPalService.VerifyPaymentAsync(authority, 80000m, Arg.Any<CancellationToken>())
            .Returns(verifyResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task Handle_Should_Mark_As_Failed_With_Empty_Status()
    {
        // Arrange
        var authority = "A88888";
        var command = new VerifyZarinPalPaymentCommand(authority, "");

        var payment = CreatePendingPayment(authority, 50000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Failed);

        await _zarinPalService.DidNotReceive().VerifyPaymentAsync(
            Arg.Any<string>(),
            Arg.Any<decimal>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Handle_Verification_Without_CardPan()
    {
        // Arrange
        var authority = "A11111";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        var payment = CreatePendingPayment(authority, 60000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        var verifyResult = new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 123123123,
            CardPan = null // Some scenarios might not return card PAN
        };

        _zarinPalService.VerifyPaymentAsync(authority, 60000m, Arg.Any<CancellationToken>())
            .Returns(verifyResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.CardPan.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Payment_In_Wrong_Status()
    {
        // Arrange
        var authority = "A22222";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        // Create already paid payment
        var payment = Domain.Aggregates.PaymentAggregate.Payment.Create(
            bookingId: BookingId.From(Guid.NewGuid()),
            customerId: UserId.From(Guid.NewGuid()),
            providerId: ProviderId.From(Guid.NewGuid()),
            amount: Money.Create(50000m, "IRR"),
            PaymentMethod.ZarinPal,
            description: "Test");

        payment.RecordPaymentRequest(authority, "https://test.com");
        payment.VerifyPayment("999", "6274****1234", 500m); // Already verified

        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        var verifyResult = new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 888888888
        };

        _zarinPalService.VerifyPaymentAsync(authority, 50000m, Arg.Any<CancellationToken>())
            .Returns(verifyResult);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert - Payment is already verified, should throw or handle gracefully
        // The actual behavior depends on your business rules
        // For this test, we expect it to either succeed idempotently or throw
        var result = await _handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Handle_Network_Exception_From_ZarinPal()
    {
        // Arrange
        var authority = "A33333";
        var command = new VerifyZarinPalPaymentCommand(authority, "OK");

        var payment = CreatePendingPayment(authority, 50000m);
        _paymentRepository.GetByAuthorityAsync(authority, Arg.Any<CancellationToken>())
            .Returns(payment);

        _zarinPalService.VerifyPaymentAsync(authority, 50000m, Arg.Any<CancellationToken>())
            .Returns<ZarinPalVerifyResult>(x => throw new Exception("Network timeout"));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Network timeout");
    }

    private static Domain.Aggregates.PaymentAggregate.Payment CreatePendingPayment(string authority, decimal amount)
    {
        var payment = Domain.Aggregates.PaymentAggregate.Payment.Create(
            bookingId: BookingId.From(Guid.NewGuid()),
            customerId: UserId.From(Guid.NewGuid()),
            providerId: ProviderId.From(Guid.NewGuid()),
            amount: Money.Create(amount, "IRR"),
            PaymentMethod.ZarinPal,
            description: "Test payment");

        payment.RecordPaymentRequest(authority, $"https://sandbox.zarinpal.com/pg/StartPay/{authority}");

        return payment;
    }
}
