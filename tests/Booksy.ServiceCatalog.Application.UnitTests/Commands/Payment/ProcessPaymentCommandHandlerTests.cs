using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Payment;
using Booksy.Infrastructure.External.Payment.DTOs;
using Booksy.ServiceCatalog.Application.Commands.Payment.ProcessPayment;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.Payment;

public class ProcessPaymentCommandHandlerTests
{
    private readonly IPaymentWriteRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ProcessPaymentCommandHandler _handler;

    public ProcessPaymentCommandHandlerTests()
    {
        _paymentRepository = Substitute.For<IPaymentWriteRepository>();
        _paymentGateway = Substitute.For<IPaymentGateway>();
        _handler = new ProcessPaymentCommandHandler(_paymentRepository, _paymentGateway);
    }

    [Fact]
    public async Task Handle_Should_Process_Payment_Successfully_With_Immediate_Capture()
    {
        // Arrange
        var command = new ProcessPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            PaymentMethodId: "pm_test_123",
            CaptureImmediately: true,
            Description: "Test payment",
            Metadata: null);

        var paymentResult = new PaymentResult
        {
            IsSuccessful = true,
            PaymentId = "pi_test_123",
            Status = "succeeded"
        };

        _paymentGateway.ProcessPaymentAsync(
            Arg.Any<PaymentRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(paymentResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.PaymentId.Should().NotBeEmpty();
        result.Status.Should().Be("Paid");
        result.PaymentIntentId.Should().Be("pi_test_123");

        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.Status == PaymentStatus.Paid &&
                p.Amount.Amount == 100m &&
                p.Amount.Currency == "USD"),
            Arg.Any<CancellationToken>());

        await _paymentGateway.Received(1).ProcessPaymentAsync(
            Arg.Is<PaymentRequest>(r =>
                r.Amount == 100m &&
                r.Currency == "USD" &&
                r.CaptureMethod == "automatic"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Authorize_Payment_When_Not_Capturing_Immediately()
    {
        // Arrange
        var command = new ProcessPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            PaymentMethodId: "pm_test_123",
            CaptureImmediately: false,
            Description: "Test authorization",
            Metadata: null);

        var paymentResult = new PaymentResult
        {
            IsSuccessful = true,
            PaymentId = "pi_test_123",
            Status = "requires_capture"
        };

        _paymentGateway.ProcessPaymentAsync(
            Arg.Any<PaymentRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(paymentResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Status.Should().Be("Authorized");

        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.Status == PaymentStatus.Authorized),
            Arg.Any<CancellationToken>());

        await _paymentGateway.Received(1).ProcessPaymentAsync(
            Arg.Is<PaymentRequest>(r => r.CaptureMethod == "manual"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Mark_Payment_As_Failed_When_Gateway_Fails()
    {
        // Arrange
        var command = new ProcessPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            PaymentMethodId: "pm_test_123",
            CaptureImmediately: true,
            Description: "Test payment",
            Metadata: null);

        var paymentResult = new PaymentResult
        {
            IsSuccessful = false,
            ErrorMessage = "Card declined",
            ErrorCode = "card_declined"
        };

        _paymentGateway.ProcessPaymentAsync(
            Arg.Any<PaymentRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(paymentResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Be("Card declined");

        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.Status == PaymentStatus.Failed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Pass_Metadata_To_Payment_Gateway()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "ip_address", "192.168.1.1" },
            { "device", "mobile" }
        };

        var command = new ProcessPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            PaymentMethodId: "pm_test_123",
            CaptureImmediately: true,
            Description: "Test payment",
            Metadata: metadata);

        var paymentResult = new PaymentResult
        {
            IsSuccessful = true,
            PaymentId = "pi_test_123",
            Status = "succeeded"
        };

        _paymentGateway.ProcessPaymentAsync(
            Arg.Any<PaymentRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(paymentResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _paymentGateway.Received(1).ProcessPaymentAsync(
            Arg.Is<PaymentRequest>(r =>
                r.Metadata != null &&
                r.Metadata.ContainsKey("ip_address") &&
                r.Metadata["ip_address"] == "192.168.1.1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Use_Provided_Payment_Method_ID()
    {
        // Arrange
        var paymentMethodId = "pm_test_saved_card";
        var command = new ProcessPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            PaymentMethodId: paymentMethodId,
            CaptureImmediately: true,
            Description: "Test payment",
            Metadata: null);

        var paymentResult = new PaymentResult
        {
            IsSuccessful = true,
            PaymentId = "pi_test_123",
            Status = "succeeded"
        };

        _paymentGateway.ProcessPaymentAsync(
            Arg.Any<PaymentRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(paymentResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _paymentGateway.Received(1).ProcessPaymentAsync(
            Arg.Is<PaymentRequest>(r => r.PaymentMethodId == paymentMethodId),
            Arg.Any<CancellationToken>());
    }
}
