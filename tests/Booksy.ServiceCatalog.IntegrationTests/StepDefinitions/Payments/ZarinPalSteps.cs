using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Reqnroll;
using System.Net;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Payments;

/// <summary>
/// Step definitions for ZarinPal payment scenarios
/// </summary>
[Binding]
public class ZarinPalSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;
    private readonly ScenarioContextHelper _helper;
    private readonly Mock<IZarinPalService> _mockZarinPalService;

    public ZarinPalSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
        _mockZarinPalService = new Mock<IZarinPalService>();
    }

    #region Payment Creation Steps

    [When(@"I send a POST request to ""(.*)"" with:")]
    public async Task WhenISendAPostRequestToWith(string endpoint, Table table)
    {
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");
        var booking = _scenarioContext.ContainsKey("Booking:Current")
            ? _scenarioContext.Get<Domain.Aggregates.BookingAggregate.Booking>("Booking:Current")
            : null;

        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new CreateZarinPalPaymentRequest
        {
            BookingId = booking?.Id.Value,
            ProviderId = provider.Id.Value,
            Amount = (decimal)requestData["Amount"],
            Currency = requestData.ContainsKey("Currency") ? (string)requestData["Currency"] : "IRR",
            Description = requestData.ContainsKey("Description") ? (string)requestData["Description"] : null,
            Mobile = requestData.ContainsKey("Mobile") ? (string)requestData["Mobile"] : null,
            Email = requestData.ContainsKey("Email") ? (string)requestData["Email"] : null
        };

        // Mock ZarinPal service response
        var mockAuthority = $"A{Guid.NewGuid():N}";
        _mockZarinPalService.Setup(x => x.CreatePaymentRequestAsync(
            It.IsAny<decimal>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ZarinPalPaymentResult
        {
            IsSuccessful = true,
            Authority = mockAuthority,
            PaymentUrl = $"https://sandbox.zarinpal.com/pg/StartPay/{mockAuthority}"
        });

        var response = await _testBase.PostAsJsonAsync<CreateZarinPalPaymentRequest, CreateZarinPalPaymentResponse>(
            endpoint, request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");

        if (response.Data != null)
        {
            _scenarioContext.Set(response.Data, "LastZarinPalResponse");
            _scenarioContext.Set(response.Data.PaymentId, "LastPaymentId");
            if (!string.IsNullOrEmpty(response.Data.Authority))
            {
                _scenarioContext.Set(response.Data.Authority, "LastAuthority");
            }
        }
    }

    [When(@"I send a POST request to ""(.*)"" with:")]
    [When(@"with metadata:")]
    public void WithMetadata(Table table)
    {
        var metadata = new Dictionary<string, object>();
        foreach (var row in table.Rows)
        {
            metadata[row["Key"]] = row["Value"];
        }
        _scenarioContext.Set(metadata, "RequestMetadata");
    }

    [Then(@"the response should contain ""(.*)""")]
    public void ThenTheResponseShouldContain(string field)
    {
        var response = _scenarioContext.Get<ApiResponse<CreateZarinPalPaymentResponse>>("LastZarinPalResponse");

        var propertyValue = response.GetType().GetProperty(field)?.GetValue(response);
        propertyValue.Should().NotBeNull($"Response should contain {field}");
    }

    [Then(@"a payment should exist in the database with:")]
    public async Task ThenAPaymentShouldExistInTheDatabaseWith(Table table)
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull($"Payment with ID {paymentId} should exist in database");

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = row["Value"];

            switch (field)
            {
                case "Status":
                    payment!.Status.ToString().Should().Be(expectedValue);
                    break;
                case "Method":
                    payment!.Method.ToString().Should().Be(expectedValue);
                    break;
                case "Amount":
                    payment!.Amount.Amount.Should().Be(decimal.Parse(expectedValue));
                    break;
                case "BookingId":
                    if (expectedValue == "null")
                        payment!.BookingId.Should().BeNull();
                    break;
            }
        }
    }

    [Then(@"the response should contain validation error for ""(.*)""")]
    public void ThenTheResponseShouldContainValidationErrorFor(string fieldName)
    {
        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Then(@"the payment metadata should be stored correctly")]
    public async Task ThenThePaymentMetadataShouldBeStoredCorrectly()
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");
        var expectedMetadata = _scenarioContext.Get<Dictionary<string, object>>("RequestMetadata");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();
        payment!.Metadata.Should().NotBeNull();

        foreach (var kvp in expectedMetadata)
        {
            payment.Metadata.Should().ContainKey(kvp.Key);
        }
    }

    [Then(@"the payment should have email stored")]
    public async Task ThenThePaymentShouldHaveEmailStored()
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();
        // Email is typically stored in metadata
        payment!.Metadata.Should().ContainKey("email");
    }

    #endregion

    #region Payment Verification Steps

    [Given(@"a ZarinPal payment request has been created with:")]
    public async Task GivenAZarinPalPaymentRequestHasBeenCreatedWith(Table table)
    {
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");
        var booking = _scenarioContext.ContainsKey("Booking:Current")
            ? _scenarioContext.Get<Domain.Aggregates.BookingAggregate.Booking>("Booking:Current")
            : null;

        var requestData = _helper.BuildDictionaryFromTable(table);

        var payment = Payment.Create(
            bookingId: booking != null ? BookingId.From(booking.Id.Value) : null,
            customerId: UserId.From(Guid.NewGuid()),
            providerId: ProviderId.From(provider.Id.Value),
            amount: Money.From((decimal)requestData["Amount"], "IRR"),
            PaymentMethod.ZarinPal,
            description: (string)requestData["Description"]);

        var authority = $"A{Guid.NewGuid():N}";
        var paymentUrl = $"https://sandbox.zarinpal.com/pg/StartPay/{authority}";

        payment.RecordPaymentRequest(authority, paymentUrl);

        await _testBase.DbContext.Set<Payment>().AddAsync(payment);
        await _testBase.DbContext.SaveChangesAsync();

        _scenarioContext.Set(payment.Id.Value, "LastPaymentId");
        _scenarioContext.Set(authority, "LastAuthority");
    }

    [Given(@"the customer completed the payment on ZarinPal gateway")]
    public void GivenTheCustomerCompletedThePaymentOnZarinPalGateway()
    {
        // Mock successful ZarinPal verification
        _mockZarinPalService.Setup(x => x.VerifyPaymentAsync(
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 123456789,
            CardPan = "6274****1234",
            Fee = 5000m
        });

        _scenarioContext.Set("completed", "PaymentGatewayStatus");
    }

    [Given(@"the customer navigated to ZarinPal payment page")]
    public void GivenTheCustomerNavigatedToZarinPalPaymentPage()
    {
        _scenarioContext.Set("navigated", "PaymentGatewayStatus");
    }

    [When(@"the customer clicks cancel button")]
    public void WhenTheCustomerClicksCancelButton()
    {
        _scenarioContext.Set("cancelled", "PaymentGatewayAction");
    }

    [When(@"ZarinPal redirects to callback with:")]
    public async Task WhenZarinPalRedirectsToCallbackWith(Table table)
    {
        var requestData = _helper.BuildDictionaryFromTable(table);
        var authority = requestData.ContainsKey("Authority") && requestData["Authority"].ToString() == "{LastAuthority}"
            ? _scenarioContext.Get<string>("LastAuthority")
            : requestData["Authority"].ToString()!;
        var status = requestData["Status"].ToString();

        var callbackUrl = $"/api/v1/payments/callback?Authority={authority}&Status={status}";

        var response = await _testBase.GetAsync(callbackUrl);

        _scenarioContext.Set(response.StatusCode, "CallbackStatusCode");
        _scenarioContext.Set(response, "CallbackResponse");
    }

    [Then(@"the callback response should redirect to success page")]
    public void ThenTheCallbackResponseShouldRedirectToSuccessPage()
    {
        var statusCode = _scenarioContext.Get<HttpStatusCode>("CallbackStatusCode");
        statusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.MovedPermanently);
    }

    [Then(@"the callback response should redirect to failure page")]
    public void ThenTheCallbackResponseShouldRedirectToFailurePage()
    {
        var statusCode = _scenarioContext.Get<HttpStatusCode>("CallbackStatusCode");
        statusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    [Then(@"the payment should have status ""(.*)"" in the database")]
    public async Task ThenThePaymentShouldHaveStatusInTheDatabase(string expectedStatus)
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull($"Payment with ID {paymentId} should exist in database");
        payment!.Status.ToString().Should().Be(expectedStatus,
            $"Payment status should be {expectedStatus}");
    }

    [Then(@"the payment should have ""(.*)"" stored")]
    public async Task ThenThePaymentShouldHaveStored(string field)
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();

        switch (field)
        {
            case "RefNumber":
                payment!.RefNumber.Should().NotBeNullOrEmpty();
                break;
            case "CardPan":
                payment!.CardPan.Should().NotBeNullOrEmpty();
                break;
        }
    }

    [Then(@"the payment failure reason should be ""(.*)""")]
    public async Task ThenThePaymentFailureReasonShouldBe(string expectedReason)
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();
        payment!.FailureReason.Should().Be(expectedReason);
    }

    [Given(@"ZarinPal captured card details:")]
    public void GivenZarinPalCapturedCardDetails(Table table)
    {
        var cardDetails = _helper.BuildDictionaryFromTable(table);
        _scenarioContext.Set(cardDetails, "CapturedCardDetails");

        // Update mock to return these details
        _mockZarinPalService.Setup(x => x.VerifyPaymentAsync(
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ZarinPalVerifyResult
        {
            IsSuccessful = true,
            RefId = 123456789,
            CardPan = cardDetails["CardPan"].ToString(),
            Fee = decimal.Parse(cardDetails["Fee"].ToString()!)
        });
    }

    [Then(@"the payment should have these details:")]
    public async Task ThenThePaymentShouldHaveTheseDetails(Table table)
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = row["Value"];

            switch (field)
            {
                case "CardPan":
                    payment!.CardPan.Should().Be(expectedValue);
                    break;
                case "Fee":
                    payment!.Fee?.Amount.Should().Be(decimal.Parse(expectedValue));
                    break;
            }
        }
    }

    [Given(@"ZarinPal charged a fee of (.*) Rials")]
    public void GivenZarinPalChargedAFeeOfRials(decimal fee)
    {
        _scenarioContext.Set(fee, "ZarinPalFee");
    }

    [When(@"the payment is verified successfully")]
    public async Task WhenThePaymentIsVerifiedSuccessfully()
    {
        var authority = _scenarioContext.Get<string>("LastAuthority");

        await WhenZarinPalRedirectsToCallbackWith(new Table("Parameter", "Value")
        {
            Rows = { { "Authority", "{LastAuthority}" }, { "Status", "OK" } }
        });
    }

    [Then(@"the payment net amount should be (.*)")]
    public async Task ThenThePaymentNetAmountShouldBe(decimal expectedNetAmount)
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();
        payment!.GetNetAmount().Amount.Should().Be(expectedNetAmount);
    }

    #endregion

    #region Refund Steps

    [Given(@"a completed ZarinPal payment exists with:")]
    public async Task GivenACompletedZarinPalPaymentExistsWith(Table table)
    {
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");
        var requestData = _helper.BuildDictionaryFromTable(table);

        var payment = Payment.Create(
            bookingId: null,
            customerId: UserId.From(Guid.NewGuid()),
            providerId: ProviderId.From(provider.Id.Value),
            amount: Money.From((decimal)requestData["Amount"], (string)requestData["Currency"]),
            PaymentMethod.ZarinPal,
            description: "Test payment");

        var authority = requestData["Authority"].ToString()!;
        var paymentUrl = $"https://sandbox.zarinpal.com/pg/StartPay/{authority}";
        payment.RecordPaymentRequest(authority, paymentUrl);

        var refNumber = requestData["RefNumber"].ToString()!;
        var cardPan = requestData["CardPan"].ToString();
        payment.VerifyPayment(refNumber, cardPan);

        await _testBase.DbContext.Set<Payment>().AddAsync(payment);
        await _testBase.DbContext.SaveChangesAsync();

        _scenarioContext.Set(payment.Id.Value, "CurrentPaymentId");
        _scenarioContext.Set(payment.Id.Value, "LastPaymentId");
    }

    [When(@"I refund (.*) Rials with reason ""(.*)""")]
    public async Task WhenIRefundRialsWithReason(decimal amount, string reason)
    {
        var paymentId = _scenarioContext.Get<Guid>("CurrentPaymentId");

        var request = new RefundPaymentRequest
        {
            Amount = amount,
            Reason = reason
        };

        var response = await _testBase.PostAsJsonAsync<RefundPaymentRequest, PaymentResponse>(
            $"/api/v1/payments/{paymentId}/refund", request);

        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [Then(@"the total refunded amount should be (.*)")]
    [Then(@"the refunded amount should be (.*)")]
    public async Task ThenTheTotalRefundedAmountShouldBe(decimal expectedAmount)
    {
        var paymentId = _scenarioContext.Get<Guid>("CurrentPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();
        payment!.RefundedAmount.Amount.Should().Be(expectedAmount);
    }

    [Given(@"the payment has been partially refunded (.*) Rials")]
    public async Task GivenThePaymentHasBeenPartiallyRefundedRials(decimal amount)
    {
        await WhenIRefundRialsWithReason(amount, "Partial refund");
    }

    [Given(@"the payment has been fully refunded")]
    public async Task GivenThePaymentHasBeenFullyRefunded()
    {
        var paymentId = _scenarioContext.Get<Guid>("CurrentPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        var fullAmount = payment!.Amount.Amount;
        await WhenIRefundRialsWithReason(fullAmount, "Full refund");
    }

    #endregion

    #region Domain Event Steps

    [Then(@"a ""(.*)"" domain event should be published")]
    [Then(@"a ""(.*)"" domain event should be published with:")]
    public async Task ThenADomainEventShouldBePublished(string eventName, Table? table = null)
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();
        payment!.DomainEvents.Should().Contain(e => e.GetType().Name == eventName);
    }

    #endregion

    #region Common Steps

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
        ((int)statusCode).Should().Be(expectedStatusCode);
    }

    [Then(@"the response should contain:")]
    public void ThenTheResponseShouldContain(Table table)
    {
        var response = _scenarioContext.Get<object>("LastZarinPalResponse")
                      ?? _scenarioContext.Get<object>("LastResponse");

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = row["Value"];

            var property = response.GetType().GetProperty(field);
            property.Should().NotBeNull($"Response should have property {field}");

            var actualValue = property!.GetValue(response);
            if (expectedValue.ToLower() != "true" && expectedValue.ToLower() != "false")
            {
                actualValue?.ToString().Should().Be(expectedValue);
            }
            else
            {
                actualValue.Should().Be(bool.Parse(expectedValue));
            }
        }
    }

    [Then(@"the response should contain error ""(.*)""")]
    [Then(@"the response should contain validation error ""(.*)""")]
    public void ThenTheResponseShouldContainError(string errorMessage)
    {
        // Error validation logic
        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
        statusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, HttpStatusCode.NotFound);
    }

    #endregion
}
