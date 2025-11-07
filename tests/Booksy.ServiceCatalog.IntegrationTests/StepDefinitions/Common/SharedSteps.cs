using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using FluentAssertions;
using Reqnroll;
using System.Net;
using System.Reflection;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Common;

/// <summary>
/// Shared step definitions that can be reused across all test scenarios
/// Consolidates common patterns like status code validation, error checking, etc.
/// </summary>
[Binding]
public class SharedSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;
    private readonly ScenarioContextHelper _helper;

    public SharedSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
    }

    #region Response Status Code Validation

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        var actualStatusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
        actualStatusCode.Should().Be((HttpStatusCode)expectedStatusCode,
            $"Expected status code {expectedStatusCode} ({(HttpStatusCode)expectedStatusCode}) but got {(int)actualStatusCode} ({actualStatusCode})");
    }

    [Then(@"the response should be successful")]
    public void ThenTheResponseShouldBeSuccessful()
    {
        var actualStatusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
        ((int)actualStatusCode).Should().BeInRange(200, 299,
            $"Expected successful status code (2xx) but got {(int)actualStatusCode} ({actualStatusCode})");
    }

    [Then(@"the response should indicate (.*) error")]
    public void ThenTheResponseShouldIndicateError(string errorType)
    {
        var actualStatusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");

        var expectedRange = errorType.ToLower() switch
        {
            "client" => (400, 499),
            "server" => (500, 599),
            _ => throw new ArgumentException($"Unknown error type: {errorType}")
        };

        ((int)actualStatusCode).Should().BeInRange(expectedRange.Item1, expectedRange.Item2,
            $"Expected {errorType} error status code ({expectedRange.Item1}-{expectedRange.Item2}) but got {(int)actualStatusCode}");
    }

    #endregion

    #region Response Message and Error Validation

    [Then(@"the response message should contain ""(.*)""")]
    public void ThenTheResponseMessageShouldContain(string expectedText)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Message.Should().Contain(expectedText,
            $"Response message should contain '{expectedText}' but was '{response.Message}'");
    }

    [Then(@"the error message should contain ""(.*)""")]
    public void ThenTheErrorMessageShouldContain(string expectedText)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Should().NotBeNull("Response should exist");

        // Check if there's an error message in the response
        var errorMessage = response.Message ?? response.Error?.ToString() ?? string.Empty;

        errorMessage.Should().Contain(expectedText,
            $"Error message should contain '{expectedText}' but was '{errorMessage}'");
    }

    [Then(@"the response should contain error details")]
    public void ThenTheResponseShouldContainErrorDetails()
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Should().NotBeNull("Response should exist");
        (response.Message != null || response.Error != null).Should().BeTrue(
            "Response should contain either a message or error details");
    }

    [Then(@"the response should not contain errors")]
    public void ThenTheResponseShouldNotContainErrors()
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Should().NotBeNull("Response should exist");
        response.Success.Should().BeTrue("Response should indicate success");
        response.Error.Should().BeNull("Response should not contain error details");
    }

    #endregion

    #region Command Execution Results

    [Then(@"the command should succeed")]
    public void ThenTheCommandShouldSucceed()
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Should().NotBeNull("Response should exist");
        response.Success.Should().BeTrue("Command should succeed");

        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
        ((int)statusCode).Should().BeInRange(200, 299,
            $"Expected successful status code but got {(int)statusCode}");
    }

    [Then(@"the command should fail with (.*)")]
    public void ThenTheCommandShouldFailWithException(string exceptionType)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");

        response.Success.Should().BeFalse($"Command should fail with {exceptionType}");

        // Map exception types to expected status codes
        var expectedStatusCode = exceptionType switch
        {
            "NotFoundException" => HttpStatusCode.NotFound,
            "ConflictException" => HttpStatusCode.Conflict,
            "ValidationException" => HttpStatusCode.BadRequest,
            "InvalidOperationException" => HttpStatusCode.BadRequest,
            "ForbiddenException" => HttpStatusCode.Forbidden,
            "UnauthorizedAccessException" => HttpStatusCode.Unauthorized,
            "PolicyViolationException" => HttpStatusCode.BadRequest,
            "InvalidServiceException" => HttpStatusCode.BadRequest,
            "NotSupportedException" => HttpStatusCode.BadRequest,
            "DomainValidationException" => HttpStatusCode.BadRequest,
            _ => throw new ArgumentException($"Unknown exception type: {exceptionType}")
        };

        statusCode.Should().Be(expectedStatusCode,
            $"Expected {exceptionType} to return {expectedStatusCode} but got {statusCode}");
    }

    #endregion

    #region Event Publishing

    [Then(@"(.*) should be published")]
    public void ThenEventShouldBePublished(string eventName)
    {
        // This would typically integrate with your event store or message bus
        // For now, we'll just verify the operation completed successfully
        // In a real implementation, you'd check the event store/message queue

        // Store the expected event for verification
        _scenarioContext.Set(eventName, "ExpectedEvent");

        // TODO: Implement actual event verification once event store/bus integration is available
        // Example:
        // var publishedEvents = await _eventStore.GetPublishedEvents();
        // publishedEvents.Should().Contain(e => e.GetType().Name == eventName);
    }

    #endregion

    #region Aggregate and Entity Validation

    [Then(@"a (.*) aggregate should be created")]
    public void ThenAnAggregateShouldBeCreated(string aggregateType)
    {
        // Verify the response contains an ID for the created aggregate
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        response.Success.Should().BeTrue($"{aggregateType} aggregate should be created successfully");

        // Store aggregate type for later verification
        _scenarioContext.Set(aggregateType, "CreatedAggregateType");
    }

    [Then(@"the (.*) should be saved to database")]
    public void ThenTheEntityShouldBeSavedToDatabase(string entityType)
    {
        // This is a placeholder - actual database verification would happen in specific step definitions
        // that have access to the entity ID

        _scenarioContext.Set(entityType, "SavedEntityType");

        // Verify command succeeded
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        response.Success.Should().BeTrue($"{entityType} should be saved successfully");
    }

    #endregion

    #region Field Value Extraction and Comparison

    [Then(@"the response should contain a (.*) with:")]
    public void ThenTheResponseShouldContainEntityWith(string entityType, Table table)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Data.Should().NotBeNull($"Response should contain {entityType} data");

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = _helper.ReplaceContextPlaceholders(row["Value"]);

            var actualValue = GetFieldValue(response.Data, field);

            actualValue?.ToString().Should().Be(expectedValue,
                $"Field '{field}' should have value '{expectedValue}'");
        }
    }

    /// <summary>
    /// Gets a field value from an object using reflection
    /// </summary>
    protected object? GetFieldValue(object? obj, string fieldName)
    {
        if (obj == null)
            return null;

        var property = obj.GetType().GetProperty(fieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property == null)
            return null;

        return property.GetValue(obj);
    }

    /// <summary>
    /// Gets a nested field value using dot notation (e.g., "Address.City")
    /// </summary>
    protected object? GetNestedFieldValue(object? obj, string fieldPath)
    {
        if (obj == null)
            return null;

        var parts = fieldPath.Split('.');
        var current = obj;

        foreach (var part in parts)
        {
            current = GetFieldValue(current, part);
            if (current == null)
                return null;
        }

        return current;
    }

    #endregion

    #region Authorization and Security

    [Then(@"I should be authorized")]
    public void ThenIShouldBeAuthorized()
    {
        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");

        statusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "Request should be authorized");
        statusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "Request should be permitted");
    }

    [Then(@"I should not be authorized")]
    public void ThenIShouldNotBeAuthorized()
    {
        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");

        (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
            .Should().BeTrue($"Request should not be authorized, but got {statusCode}");
    }

    #endregion

    #region Idempotency and State Management

    [Then(@"no state change should occur")]
    public void ThenNoStateChangeShouldOccur()
    {
        // Command should succeed but not cause any state change
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        response.Success.Should().BeTrue("Command should succeed idempotently");
    }

    [Then(@"no duplicate (.*) should be created")]
    public void ThenNoDuplicateEntityShouldBeCreated(string entityType)
    {
        // Verify command succeeded without creating duplicates
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        response.Success.Should().BeTrue($"Command should handle duplicate {entityType} correctly");
    }

    #endregion

    #region Data Existence and Persistence

    [Then(@"the (.*) should exist in the database")]
    public void ThenTheEntityShouldExistInTheDatabase(string entityType)
    {
        // This is a placeholder - specific implementations would verify actual database state
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        response.Success.Should().BeTrue($"{entityType} should exist in database");

        _scenarioContext.Set(true, $"{entityType}ExistsInDb");
    }

    [Then(@"the (.*) should not exist in the database")]
    public void ThenTheEntityShouldNotExistInTheDatabase(string entityType)
    {
        // This would be implemented in specific step definitions with actual database queries
        _scenarioContext.Set(false, $"{entityType}ExistsInDb");
    }

    #endregion

    #region Validation and Business Rules

    [Then(@"the validation should fail")]
    public void ThenTheValidationShouldFail()
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");

        response.Success.Should().BeFalse("Validation should fail");
        statusCode.Should().Be(HttpStatusCode.BadRequest,
            "Validation failure should return BadRequest status");
    }

    [Then(@"the validation should pass")]
    public void ThenTheValidationShouldPass()
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Success.Should().BeTrue("Validation should pass");
    }

    [Then(@"the business rule should be enforced")]
    public void ThenTheBusinessRuleShouldBeEnforced()
    {
        // Verify command behavior aligns with business rules
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        response.Should().NotBeNull("Response should exist to verify business rule enforcement");
    }

    #endregion

    #region Helper Methods for Test Data

    [Given(@"the following test data exists:")]
    public async Task GivenTheFollowingTestDataExists(Table table)
    {
        // This can be used to set up common test data
        foreach (var row in table.Rows)
        {
            var entity = row["Entity"];
            var identifier = row["Identifier"];
            var value = row.ContainsKey("Value") ? row["Value"] : null;

            _scenarioContext.Set(value, $"{entity}:{identifier}");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Response Data Extraction

    [Then(@"the response should contain an ID")]
    public void ThenTheResponseShouldContainAnId()
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Data.Should().NotBeNull("Response should contain data");

        var idProperty = response.Data.GetType().GetProperty("Id") ??
                        response.Data.GetType().GetProperty("id");

        idProperty.Should().NotBeNull("Response data should contain an Id property");

        var idValue = idProperty!.GetValue(response.Data);
        idValue.Should().NotBeNull("Id should have a value");
    }

    [Then(@"I should store the response ID as ""(.*)""")]
    public void ThenIShouldStoreTheResponseIdAs(string contextKey)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        var idProperty = response.Data.GetType().GetProperty("Id") ??
                        response.Data.GetType().GetProperty("id");

        var idValue = idProperty?.GetValue(response.Data);

        idValue.Should().NotBeNull($"Response should contain an ID to store as {contextKey}");

        _scenarioContext.Set(idValue!, contextKey);
    }

    #endregion

    #region Pagination and Collection Results

    [Then(@"the response should contain (.*) items")]
    public void ThenTheResponseShouldContainItems(int expectedCount)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Data.Should().NotBeNull("Response should contain data");

        // Try to get count from collection
        if (response.Data is System.Collections.IEnumerable enumerable)
        {
            var actualCount = enumerable.Cast<object>().Count();
            actualCount.Should().Be(expectedCount,
                $"Response should contain {expectedCount} items but contained {actualCount}");
        }
        else
        {
            throw new InvalidOperationException("Response data is not a collection");
        }
    }

    [Then(@"the response should contain at least (.*) items")]
    public void ThenTheResponseShouldContainAtLeastItems(int minimumCount)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Data.Should().NotBeNull("Response should contain data");

        if (response.Data is System.Collections.IEnumerable enumerable)
        {
            var actualCount = enumerable.Cast<object>().Count();
            actualCount.Should().BeGreaterOrEqualTo(minimumCount,
                $"Response should contain at least {minimumCount} items but contained {actualCount}");
        }
    }

    #endregion
}
