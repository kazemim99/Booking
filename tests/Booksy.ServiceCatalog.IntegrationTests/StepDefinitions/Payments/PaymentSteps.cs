using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using System.Net;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Payments;

/// <summary>
/// Step definitions for payment-related scenarios
/// </summary>
[Binding]
public class PaymentSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;
    private readonly ScenarioContextHelper _helper;

    public PaymentSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
    }

    [When(@"I send a POST request to process a payment with:")]
    public async Task WhenISendAPostRequestToProcessAPaymentWith(Table table)
    {
        var booking = _scenarioContext.Get<Domain.Aggregates.BookingAggregate.Booking>("Booking:Current");
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");

        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new ProcessPaymentRequest
        {
            BookingId = booking.Id.Value,
            ProviderId = provider.Id.Value,
            Amount = (decimal)requestData["Amount"],
            Currency = (string)requestData["Currency"],
            PaymentMethod = (string)requestData["PaymentMethod"],
            PaymentMethodId = (string)requestData["PaymentMethodId"],
            CaptureImmediately = requestData.ContainsKey("CaptureImmediately")
                ? (bool)requestData["CaptureImmediately"]
                : true,
            Description = "Test payment"
        };

        var response = await _testBase.PostAsJsonAsync<ProcessPaymentRequest, PaymentResponse>(
            "/api/v1/payments", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");

        if (response.Data != null)
        {
            _scenarioContext.Set(response.Data, "LastPaymentResponse");
            _scenarioContext.Set(response.Data.PaymentId, "LastPaymentId");
        }
    }

    [When(@"I send a POST request to refund the payment with:")]
    public async Task WhenISendAPostRequestToRefundThePaymentWith(Table table)
    {
        var paymentId = _scenarioContext.Get<Guid>("CurrentPaymentId");

        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new RefundPaymentRequest
        {
            Amount = (decimal)requestData["Amount"],
            Reason = (string)requestData["Reason"],
            Notes = requestData.ContainsKey("Notes") ? (string)requestData["Notes"] : null
        };

        var response = await _testBase.PostAsJsonAsync<RefundPaymentRequest, PaymentResponse>(
            $"/api/v1/payments/{paymentId}/refund", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");

        if (response.Data != null)
        {
            _scenarioContext.Set(response.Data, "LastPaymentResponse");
        }
    }

    [When(@"I send a POST request to refund payment ""(.*)"" with:")]
    public async Task WhenISendAPostRequestToRefundSpecificPaymentWith(Guid paymentId, Table table)
    {
        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new RefundPaymentRequest
        {
            Amount = (decimal)requestData["Amount"],
            Reason = (string)requestData["Reason"]
        };

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/payments/{paymentId}/refund", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [Then(@"the response should contain a payment with:")]
    public void ThenTheResponseShouldContainAPaymentWith(Table table)
    {
        var response = _scenarioContext.Get<ApiResponse<PaymentResponse>>("LastResponse");

        response.Data.Should().NotBeNull("Response should contain payment data");

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = _helper.ReplaceContextPlaceholders(row["Value"]);

            var actualValue = GetFieldValue(response.Data, field);

            actualValue?.ToString().Should().Be(expectedValue,
                $"Field '{field}' should have value '{expectedValue}'");
        }
    }

    [Then(@"a payment should exist in the database with status ""(.*)""")]
    public async Task ThenAPaymentShouldExistInTheDatabaseWithStatus(string expectedStatus)
    {
        var paymentId = _scenarioContext.Get<Guid>("LastPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull($"Payment with ID {paymentId} should exist in database");
        payment!.Status.ToString().Should().Be(expectedStatus,
            $"Payment status should be {expectedStatus}");
    }

    [Then(@"the payment should have status ""(.*)"" in the database")]
    public async Task ThenThePaymentShouldHaveStatusInTheDatabase(string expectedStatus)
    {
        var paymentId = _scenarioContext.Get<Guid>("CurrentPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull($"Payment with ID {paymentId} should exist in database");
        payment!.Status.ToString().Should().Be(expectedStatus,
            $"Payment status should be {expectedStatus}");
    }

    [Then(@"the refunded amount should be (.*)")]
    public async Task ThenTheRefundedAmountShouldBe(decimal expectedAmount)
    {
        var paymentId = _scenarioContext.Get<Guid>("CurrentPaymentId");

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();
        payment!.RefundedAmount.Amount.Should().Be(expectedAmount,
            $"Refunded amount should be {expectedAmount}");
    }

    private object? GetFieldValue(object obj, string fieldName)
    {
        if (obj == null)
            return null;

        var property = obj.GetType().GetProperty(fieldName);
        if (property == null)
            return null;

        return property.GetValue(obj);
    }
}
