using System.Net;
using System.Text.Json;
using AsanRezerve.Infrastructure.External.Payment.ZarinPal;
using AsanRezerve.Infrastructure.External.Payment.ZarinPal.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AsanRezerve.Infrastructure.External.UnitTests.Payment;

/// <summary>
/// <see cref="ZarinPalService"/> against an in-process HTTP stub: request/verify/refund success and failure
/// shapes, the rial-to-toman fee conversion, sandbox URL selection, and network failure handling.
///
/// <para>History: this file lived in <c>tests/AsanRezerve.ServiceCatalog.UnitTests</c>, a directory that had no
/// project file, so it was never compiled or run (docs/TEST_ARCHITECTURE_AUDIT.md §B #7). Ported from Moq's
/// <c>Protected().Setup("SendAsync")</c> to a plain <see cref="HttpMessageHandler"/> stub when it was revived.</para>
/// </summary>
public class ZarinPalServiceTests
{
    /// <summary>Answers every request with the configured response (or throws), and remembers the last request.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_respond(request));
        }
    }

    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ZarinPalSettings _settings;
    private readonly ZarinPalService _sut;

    public ZarinPalServiceTests()
    {
        _settings = new ZarinPalSettings
        {
            MerchantId = "test-merchant-id",
            IsSandbox = true,
            CallbackUrl = "https://test.com/callback"
        };

        _sut = new ZarinPalService(Options.Create(_settings), _httpClientFactory, NullLogger<ZarinPalService>.Instance);
    }

    private static HttpResponseMessage Json(object dto) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(dto))
    };

    private StubHandler GatewayAnswers(object dto) => GatewayAnswers(_ => Json(dto));

    private StubHandler GatewayAnswers(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        _httpClientFactory.CreateClient("ZarinPal").Returns(new HttpClient(handler));
        return handler;
    }

    [Fact]
    public async Task CreatePaymentRequestAsync_Should_Return_Success_With_Authority_And_PaymentUrl()
    {
        GatewayAnswers(new ZarinPalPaymentResponseDto
        {
            Data = new ZarinPalPaymentData
            {
                Code = 100,
                Message = "Success",
                Authority = "A00000000000000000000000000000123456",
                Fee = 5000
            }
        });

        var result = await _sut.CreatePaymentRequestAsync(50000m, "Test payment", "09123456789", "test@example.com");

        result.IsSuccessful.Should().BeTrue();
        result.Authority.Should().Be("A00000000000000000000000000000123456");
        result.PaymentUrl.Should().Contain("StartPay");
        result.Fee.Should().Be(500m, "the gateway reports the fee in rials; the result is in tomans (5000 / 10)");
    }

    [Fact]
    public async Task CreatePaymentRequestAsync_Should_Return_Failure_When_API_Returns_Error()
    {
        GatewayAnswers(new ZarinPalPaymentResponseDto
        {
            Errors = new List<ZarinPalError>
            {
                new() { Code = -11, Message = "Invalid merchant credentials" }
            }
        });

        var result = await _sut.CreatePaymentRequestAsync(50000m, "Test payment");

        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be(-11);
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyPaymentAsync_Should_Return_Success_With_RefId_And_CardPan()
    {
        GatewayAnswers(new ZarinPalVerifyResponseDto
        {
            Data = new ZarinPalVerifyData
            {
                Code = 100,
                Message = "Verified",
                RefId = 123456789,
                CardPan = "6274****1234",
                Fee = 5000
            }
        });

        var result = await _sut.VerifyPaymentAsync("A00000000000000000000000000000123456", 50000m);

        result.IsSuccessful.Should().BeTrue();
        result.RefId.Should().Be(123456789);
        result.CardPan.Should().Be("6274****1234");
        result.Fee.Should().Be(500m);
    }

    [Fact]
    public async Task VerifyPaymentAsync_Should_Return_Success_For_Already_Verified_Payment_Code_101()
    {
        GatewayAnswers(new ZarinPalVerifyResponseDto
        {
            Data = new ZarinPalVerifyData
            {
                Code = 101, // Already verified
                Message = "Already verified",
                RefId = 123456789,
                CardPan = "6274****1234"
            }
        });

        var result = await _sut.VerifyPaymentAsync("A00000000000000000000000000000123456", 50000m);

        result.IsSuccessful.Should().BeTrue();
        result.RefId.Should().Be(123456789);
    }

    [Fact]
    public async Task VerifyPaymentAsync_Should_Return_Failure_When_Verification_Fails()
    {
        GatewayAnswers(new ZarinPalVerifyResponseDto
        {
            Data = new ZarinPalVerifyData
            {
                Code = -53,
                Message = "Transaction verification unsuccessful"
            }
        });

        var result = await _sut.VerifyPaymentAsync("A00000000000000000000000000000123456", 50000m);

        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be(-53);
    }

    [Fact]
    public async Task RefundPaymentAsync_Should_Return_Success_When_Refund_Succeeds()
    {
        GatewayAnswers(new ZarinPalRefundResponseDto
        {
            Data = new ZarinPalRefundData { Code = 100, Message = "Refund successful" }
        });

        var result = await _sut.RefundPaymentAsync("A00000000000000000000000000000123456", 25000m, "Customer requested refund");

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task RefundPaymentAsync_Should_Return_Failure_When_Refund_Fails()
    {
        GatewayAnswers(new ZarinPalRefundResponseDto
        {
            Errors = new List<ZarinPalError>
            {
                new() { Code = -52, Message = "Transaction not found for refund" }
            }
        });

        var result = await _sut.RefundPaymentAsync("A00000000000000000000000000000123456", 25000m);

        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be(-52);
    }

    [Fact]
    public async Task CreatePaymentRequestAsync_Should_Handle_Network_Exception_Gracefully()
    {
        GatewayAnswers(_ => throw new HttpRequestException("Network error"));

        var result = await _sut.CreatePaymentRequestAsync(50000m, "Test");

        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().ContainEquivalentOf("failed");
    }

    [Fact]
    public async Task CreatePaymentRequestAsync_Should_Use_Sandbox_URL_When_IsSandbox_Is_True()
    {
        _settings.IsSandbox = true;
        var handler = GatewayAnswers(new ZarinPalPaymentResponseDto
        {
            Data = new ZarinPalPaymentData { Code = 100, Authority = "A123", Message = "Success" }
        });

        var result = await _sut.CreatePaymentRequestAsync(50000m, "Test payment");

        result.PaymentUrl.Should().Contain("sandbox.zarinpal.com");
        handler.LastRequest.Should().NotBeNull("the request must have reached the (stubbed) gateway");
    }
}
