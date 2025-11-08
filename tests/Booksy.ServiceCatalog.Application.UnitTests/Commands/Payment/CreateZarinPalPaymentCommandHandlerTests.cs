using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.Infrastructure.External.Payment.ZarinPal.Models;
using Booksy.ServiceCatalog.Application.Commands.Payment.CreateZarinPalPayment;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.Payment;

public class CreateZarinPalPaymentCommandHandlerTests
{
    private readonly IPaymentWriteRepository _paymentRepository;
    private readonly IZarinPalService _zarinPalService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateZarinPalPaymentCommandHandler> _logger;
    private readonly CreateZarinPalPaymentCommandHandler _handler;

    public CreateZarinPalPaymentCommandHandlerTests()
    {
        _paymentRepository = Substitute.For<IPaymentWriteRepository>();
        _zarinPalService = Substitute.For<IZarinPalService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<CreateZarinPalPaymentCommandHandler>>();
        _handler = new CreateZarinPalPaymentCommandHandler(
            _paymentRepository,
            _zarinPalService,
            _unitOfWork,
            _logger);
    }

    [Fact]
    public async Task Handle_Should_Create_Payment_Request_Successfully_With_Authority_And_PaymentUrl()
    {
        // Arrange
        var command = new CreateZarinPalPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 50000m,
            Currency: "IRR",
            Description: "Test payment",
            Mobile: "09123456789",
            Email: "test@example.com",
            Metadata: null,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = "A00000000000000000000000000000123456",
            PaymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/A00000000000000000000000000000123456",
            Fee = 500m
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.PaymentId.Should().NotBeEmpty();
        result.Authority.Should().Be("A00000000000000000000000000000123456");
        result.PaymentUrl.Should().Be("https://sandbox.zarinpal.com/pg/StartPay/A00000000000000000000000000000123456");
        result.Amount.Should().Be(50000m);
        result.Currency.Should().Be("IRR");

        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.Status == PaymentStatus.Pending &&
                p.Method == PaymentMethod.ZarinPal &&
                p.Authority == "A00000000000000000000000000000123456" &&
                p.PaymentUrl == zarinPalResult.PaymentUrl &&
                p.Amount.Amount == 50000m &&
                p.Amount.Currency == "IRR"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Create_Payment_With_Mobile_And_Email()
    {
        // Arrange
        var mobile = "09123456789";
        var email = "customer@example.com";
        var command = new CreateZarinPalPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 100000m,
            Currency: "IRR",
            Description: "Service payment",
            Mobile: mobile,
            Email: email,
            Metadata: null,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = "A123",
            PaymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/A123"
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _zarinPalService.Received(1).CreatePaymentRequestAsync(
            100000m,
            "Service payment",
            mobile,
            email,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Store_Fee_When_Provided_By_ZarinPal()
    {
        // Arrange
        var command = new CreateZarinPalPaymentCommand(
            BookingId: null,
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 200000m,
            Currency: "IRR",
            Description: "Direct payment",
            Mobile: null,
            Email: null,
            Metadata: null,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = "A456",
            PaymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/A456",
            Fee = 2000m
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.Fee != null &&
                p.Fee.Amount == 2000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_ZarinPal_Returns_Error()
    {
        // Arrange
        var command = new CreateZarinPalPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 50000m,
            Currency: "IRR",
            Description: "Test payment",
            Mobile: null,
            Email: null,
            Metadata: null,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = false,
            ErrorCode = -11,
            ErrorMessage = "Invalid merchant credentials"
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be(-11);
        result.ErrorMessage.Should().Be("Invalid merchant credentials");
        result.PaymentId.Should().BeEmpty();

        await _paymentRepository.DidNotReceive().AddAsync(
            Arg.Any<Domain.Aggregates.PaymentAggregate.Payment>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Create_Direct_Payment_When_No_BookingId()
    {
        // Arrange
        var command = new CreateZarinPalPaymentCommand(
            BookingId: null,
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 75000m,
            Currency: "IRR",
            Description: "Direct service payment",
            Mobile: "09121234567",
            Email: null,
            Metadata: null,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = "A789",
            PaymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/A789"
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.BookingId == null &&
                p.Status == PaymentStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Store_Metadata_When_Provided()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "ip_address", "192.168.1.1" },
            { "user_agent", "Mozilla/5.0" },
            { "device_type", "mobile" }
        };

        var command = new CreateZarinPalPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 150000m,
            Currency: "IRR",
            Description: "Payment with metadata",
            Mobile: null,
            Email: null,
            Metadata: metadata,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = "A999",
            PaymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/A999"
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.Metadata != null &&
                p.Metadata.ContainsKey("ip_address") &&
                p.Metadata["ip_address"].ToString() == "192.168.1.1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Create_Payment_For_Booking()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var command = new CreateZarinPalPaymentCommand(
            BookingId: bookingId,
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 250000m,
            Currency: "IRR",
            Description: "Booking payment",
            Mobile: "09123456789",
            Email: null,
            Metadata: null,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = "A111",
            PaymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/A111"
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.BookingId != null &&
                p.BookingId.Value == bookingId &&
                p.Status == PaymentStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Publish_PaymentRequestCreatedEvent_When_Successful()
    {
        // Arrange
        var command = new CreateZarinPalPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 50000m,
            Currency: "IRR",
            Description: "Test payment",
            Mobile: null,
            Email: null,
            Metadata: null,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = "A222",
            PaymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/A222"
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verify domain event was published through aggregate
        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Domain.Aggregates.PaymentAggregate.Payment>(p =>
                p.DomainEvents.Any(e =>
                    e.GetType().Name == "PaymentRequestCreatedEvent")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Handle_Exception_From_ZarinPal_Service_Gracefully()
    {
        // Arrange
        var command = new CreateZarinPalPaymentCommand(
            BookingId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 50000m,
            Currency: "IRR",
            Description: "Test payment",
            Mobile: null,
            Email: null,
            Metadata: null,
            IdempotencyKey: null);

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns<ZarinPalPaymentResult>(x => throw new Exception("Network error"));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Network error");

        await _paymentRepository.DidNotReceive().AddAsync(
            Arg.Any<Domain.Aggregates.PaymentAggregate.Payment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Pass_Correct_Amount_To_ZarinPal_Service()
    {
        // Arrange
        var amount = 123450m;
        var command = new CreateZarinPalPaymentCommand(
            BookingId: null,
            CustomerId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: amount,
            Currency: "IRR",
            Description: "Amount test",
            Mobile: null,
            Email: null,
            Metadata: null,
            IdempotencyKey: null);

        var zarinPalResult = new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = "A333",
            PaymentUrl = "https://sandbox.zarinpal.com/pg/StartPay/A333"
        };

        _zarinPalService.CreatePaymentRequestAsync(
            Arg.Any<decimal>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(zarinPalResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _zarinPalService.Received(1).CreatePaymentRequestAsync(
            amount,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
