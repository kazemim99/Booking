using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.Infrastructure.External.Payment.ZarinPal.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Booksy.ServiceCatalog.UnitTests.Infrastructure.Payment;

public class ZarinPalServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ZarinPalService>> _loggerMock;
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

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ZarinPalService>>();

        var optionsMock = new Mock<IOptions<ZarinPalSettings>>();
        optionsMock.Setup(x => x.Value).Returns(_settings);

        _sut = new ZarinPalService(optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePaymentRequestAsync_Should_Return_Success_With_Authority_And_PaymentUrl()
    {
        // Arrange
        var amount = 50000m;
        var description = "Test payment";
        var mobile = "09123456789";
        var email = "test@example.com";

        var responseDto = new ZarinPalPaymentResponseDto
        {
            Data = new ZarinPalPaymentData
            {
                Code = 100,
                Message = "Success",
                Authority = "A00000000000000000000000000000123456",
                Fee = 5000
            }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseDto))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.CreatePaymentRequestAsync(amount, description, mobile, email);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("A00000000000000000000000000000123456", result.Authority);
        Assert.Contains("StartPay", result.PaymentUrl);
        Assert.Equal(500m, result.Fee); // 5000 / 10
    }

    [Fact]
    public async Task CreatePaymentRequestAsync_Should_Return_Failure_When_API_Returns_Error()
    {
        // Arrange
        var amount = 50000m;
        var description = "Test payment";

        var responseDto = new ZarinPalPaymentResponseDto
        {
            Errors = new List<ZarinPalError>
            {
                new ZarinPalError
                {
                    Code = -11,
                    Message = "Invalid merchant credentials"
                }
            }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseDto))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.CreatePaymentRequestAsync(amount, description);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(-11, result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyPaymentAsync_Should_Return_Success_With_RefId_And_CardPan()
    {
        // Arrange
        var authority = "A00000000000000000000000000000123456";
        var amount = 50000m;

        var responseDto = new ZarinPalVerifyResponseDto
        {
            Data = new ZarinPalVerifyData
            {
                Code = 100,
                Message = "Verified",
                RefId = 123456789,
                CardPan = "6274****1234",
                Fee = 5000
            }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseDto))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.VerifyPaymentAsync(authority, amount);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(123456789, result.RefId);
        Assert.Equal("6274****1234", result.CardPan);
        Assert.Equal(500m, result.Fee);
    }

    [Fact]
    public async Task VerifyPaymentAsync_Should_Return_Success_For_Already_Verified_Payment_Code_101()
    {
        // Arrange
        var authority = "A00000000000000000000000000000123456";
        var amount = 50000m;

        var responseDto = new ZarinPalVerifyResponseDto
        {
            Data = new ZarinPalVerifyData
            {
                Code = 101, // Already verified
                Message = "Already verified",
                RefId = 123456789,
                CardPan = "6274****1234"
            }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseDto))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.VerifyPaymentAsync(authority, amount);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(123456789, result.RefId);
    }

    [Fact]
    public async Task VerifyPaymentAsync_Should_Return_Failure_When_Verification_Fails()
    {
        // Arrange
        var authority = "A00000000000000000000000000000123456";
        var amount = 50000m;

        var responseDto = new ZarinPalVerifyResponseDto
        {
            Data = new ZarinPalVerifyData
            {
                Code = -53,
                Message = "Transaction verification unsuccessful"
            }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseDto))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.VerifyPaymentAsync(authority, amount);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(-53, result.ErrorCode);
    }

    [Fact]
    public async Task RefundPaymentAsync_Should_Return_Success_When_Refund_Succeeds()
    {
        // Arrange
        var authority = "A00000000000000000000000000000123456";
        var amount = 25000m;
        var description = "Customer requested refund";

        var responseDto = new ZarinPalRefundResponseDto
        {
            Data = new ZarinPalRefundData
            {
                Code = 100,
                Message = "Refund successful"
            }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseDto))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.RefundPaymentAsync(authority, amount, description);

        // Assert
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task RefundPaymentAsync_Should_Return_Failure_When_Refund_Fails()
    {
        // Arrange
        var authority = "A00000000000000000000000000000123456";
        var amount = 25000m;

        var responseDto = new ZarinPalRefundResponseDto
        {
            Errors = new List<ZarinPalError>
            {
                new ZarinPalError
                {
                    Code = -52,
                    Message = "Transaction not found for refund"
                }
            }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseDto))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.RefundPaymentAsync(authority, amount);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(-52, result.ErrorCode);
    }

    [Fact]
    public async Task CreatePaymentRequestAsync_Should_Handle_Network_Exception_Gracefully()
    {
        // Arrange
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.CreatePaymentRequestAsync(50000m, "Test");

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("failed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePaymentRequestAsync_Should_Use_Sandbox_URL_When_IsSandbox_Is_True()
    {
        // Arrange
        _settings.IsSandbox = true;
        var amount = 50000m;
        var description = "Test payment";

        var responseDto = new ZarinPalPaymentResponseDto
        {
            Data = new ZarinPalPaymentData
            {
                Code = 100,
                Authority = "A123",
                Message = "Success"
            }
        };

        HttpRequestMessage? capturedRequest = null;
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseDto))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("ZarinPal")).Returns(httpClient);

        // Act
        var result = await _sut.CreatePaymentRequestAsync(amount, description);

        // Assert
        Assert.Contains("sandbox.zarinpal.com", result.PaymentUrl);
    }
}
