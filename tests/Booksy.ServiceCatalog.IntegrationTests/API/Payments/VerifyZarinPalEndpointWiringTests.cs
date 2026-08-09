using Booksy.ServiceCatalog.API.Controllers.V1;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Application.Commands.Payment.VerifyZarinPalPayment;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// C4 Phase 0 / gaps B2 + B3 on <c>POST /payments/zarinpal/verify</c>.
/// <b>B2:</b> the endpoint used to hardcode <c>Status = "OK"</c>, so a customer cancellation could not be reported
/// and settled — it must now forward the reported outcome (defaulting to "OK" for existing callers).
/// <b>B3:</b> the endpoint did not forward the <c>Idempotency-Key</c> header (the four other money endpoints do), so
/// a client retry was not deduplicated by the C2 §2 reservation — it must now pass the key through.
/// The controller is exercised directly with a mocked mediator so the exact command it dispatches can be asserted.
/// </summary>
public class VerifyZarinPalEndpointWiringTests
{
    private static VerifyZarinPalPaymentResult Ok() =>
        new(Guid.NewGuid(), null, IsSuccessful: true, PaymentStatus: "Paid", RefNumber: 12345);

    /// Builds the controller with a mocked mediator, optionally supplying an Idempotency-Key request header.
    private static (PaymentsController Controller, Mock<IMediator> Mediator) Build(string? idempotencyKey)
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<VerifyZarinPalPaymentCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Ok());

        var controller = new PaymentsController(
            mediator.Object,
            NullLogger<PaymentsController>.Instance,
            new ConfigurationBuilder().Build());

        var httpContext = new DefaultHttpContext();
        if (idempotencyKey is not null)
            httpContext.Request.Headers["Idempotency-Key"] = idempotencyKey;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return (controller, mediator);
    }

    private static VerifyZarinPalPaymentCommand Captured(Mock<IMediator> mediator)
    {
        var invocation = mediator.Invocations.Single(i => i.Arguments[0] is VerifyZarinPalPaymentCommand);
        return (VerifyZarinPalPaymentCommand)invocation.Arguments[0];
    }

    // ---------------------------------------------------------------- B2

    [Fact]
    public async Task Forwards_a_reported_cancellation_instead_of_assuming_OK()
    {
        var (controller, mediator) = Build(idempotencyKey: null);

        await controller.VerifyZarinPalPayment(
            new VerifyZarinPalPaymentRequest { Authority = "auth-1", Status = "NOK" });

        Captured(mediator).Status.Should().Be("NOK",
            "a customer cancellation must reach the handler so the payment is settled, not left Pending");
    }

    [Fact]
    public async Task Defaults_to_OK_when_no_status_is_supplied_by_an_existing_caller()
    {
        var (controller, mediator) = Build(idempotencyKey: null);

        // An existing client that only sends Authority must behave exactly as before (backward compatible).
        await controller.VerifyZarinPalPayment(new VerifyZarinPalPaymentRequest { Authority = "auth-1" });

        Captured(mediator).Status.Should().Be("OK");
    }

    [Fact]
    public async Task Treats_a_blank_status_as_OK()
    {
        var (controller, mediator) = Build(idempotencyKey: null);

        await controller.VerifyZarinPalPayment(
            new VerifyZarinPalPaymentRequest { Authority = "auth-1", Status = "   " });

        Captured(mediator).Status.Should().Be("OK");
    }

    // ---------------------------------------------------------------- B3

    [Fact]
    public async Task Forwards_the_idempotency_key_header_to_the_command()
    {
        var key = Guid.NewGuid();
        var (controller, mediator) = Build(idempotencyKey: key.ToString());

        await controller.VerifyZarinPalPayment(new VerifyZarinPalPaymentRequest { Authority = "auth-1" });

        Captured(mediator).IdempotencyKey.Should().Be(key,
            "the client's Idempotency-Key must reach the C2 §2 reservation so a retry replays the stored result");
    }

    [Fact]
    public async Task Sends_a_null_key_when_the_header_is_absent_or_malformed()
    {
        var (c1, m1) = Build(idempotencyKey: null);
        await c1.VerifyZarinPalPayment(new VerifyZarinPalPaymentRequest { Authority = "auth-1" });
        Captured(m1).IdempotencyKey.Should().BeNull();

        var (c2, m2) = Build(idempotencyKey: "not-a-guid");
        await c2.VerifyZarinPalPayment(new VerifyZarinPalPaymentRequest { Authority = "auth-1" });
        Captured(m2).IdempotencyKey.Should().BeNull("a malformed header must not throw; the behavior generates a key");
    }
}
