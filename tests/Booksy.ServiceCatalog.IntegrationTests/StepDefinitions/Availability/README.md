# Availability Step Definitions - Implementation Guide

## Current Status
The Availability feature tests have been scaffolded with:
- ✅ Feature file with 15 comprehensive scenarios
- ✅ Step definition structure (AvailabilityStepsSimple.cs)
- ✅ DI configuration properly set up
- ✅ Project compiles successfully

## Recommended Approach for Response Validation

When implementing API response validation steps, use the following pattern for better error detection:

### For Successful Responses:
```csharp
[Then(@"the response should be successful")]
public async Task ThenTheResponseShouldBeSuccessful()
{
    var httpResponse = _scenarioContext.Get<HttpResponseMessage>("LastResponse");

    // Read the API response
    var apiResponse = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<List<AvailableSlotResponse>>>();

    // Validate success - IMPORTANT: Use Error (singular), not Errors
    apiResponse.Should().NotBeNull();
    apiResponse!.Success.Should().BeTrue("API should return success");
    apiResponse.Error.Should().BeNull("There should be no errors in the response");

    // Also verify HTTP status
    httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Store for further assertions
    _scenarioContext.Set(apiResponse, "LastApiResponse");
}
```

### For Error Responses:
```csharp
[Then(@"the response should contain an error")]
public async Task ThenTheResponseShouldContainAnError()
{
    var httpResponse = _scenarioContext.Get<HttpResponseMessage>("LastResponse");
    var apiResponse = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();

    apiResponse.Should().NotBeNull();
    apiResponse!.Success.Should().BeFalse("API should return failure");
    apiResponse.Error.Should().NotBeNull("Error details should be present");
}
```

### Benefits of This Approach:
1. **Better Error Messages**: `response.Error.Should().BeNull()` provides clear failure messages
2. **Validation Details**: Access to `ErrorResponse.ValidationErrors` for detailed validation failures
3. **Structured Data**: Can access `ErrorResponse.Code`, `Message`, `Target`, etc.
4. **Consistency**: Matches the API's response structure (`ApiResponse<T>`)

## API Response Structure

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public ErrorResponse? Error { get; set; }
    public ResponseMetadata? Metadata { get; set; }
}

public class ErrorResponse
{
    public string Code { get; set; }
    public string Message { get; set; }
    public string? Target { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
    public List<ErrorDetail>? Details { get; set; }
    public string? StackTrace { get; set; }
}
```

## Example Implementation

Here's a complete example of implementing a When/Then pair:

```csharp
[When(@"I request available slots for (\d+) days from now")]
public async Task WhenIRequestAvailableSlotsForDaysFromNow(int daysFromNow)
{
    var provider = _scenarioContext.Get<Provider>("Provider:Current");
    var service = _scenarioContext.Get<Service>("Service:Current");

    var targetDate = DateTime.UtcNow.AddDays(daysFromNow).ToString("yyyy-MM-dd");
    var url = $"/api/v1/availability/slots?providerId={provider.Id.Value}&serviceId={service.Id.Value}&date={targetDate}";

    var response = await _testBase.Client.GetAsync(url);

    _scenarioContext.Set(response, "LastResponse");
    _scenarioContext.Set(response.StatusCode, "LastStatusCode");
}

[Then(@"the response should contain available time slots")]
public async Task ThenTheResponseShouldContainAvailableTimeSlots()
{
    var httpResponse = _scenarioContext.Get<HttpResponseMessage>("LastResponse");
    var apiResponse = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<List<AvailableSlotResponse>>>();

    // Validate the response structure
    apiResponse.Should().NotBeNull();
    apiResponse!.Success.Should().BeTrue("API call should succeed");
    apiResponse.Error.Should().BeNull("No errors should be present");

    // Validate the data
    apiResponse.Data.Should().NotBeNull("Slots data should be present");
    apiResponse.Data!.Should().NotBeEmpty("There should be available slots");

    // Additional validations
    apiResponse.Data.All(s => s.IsAvailable).Should().BeTrue("All returned slots should be available");
}
```

## Next Steps

1. Implement the When steps to make actual API calls
2. Implement Then steps with proper `ApiResponse<T>` validation
3. Use `response.Error.Should().BeNull()` for success cases
4. Use `response.Error.Should().NotBeNull()` for error cases
5. Access `response.Data` for validating returned data
6. Test both happy paths and error scenarios

## Reference

See existing step definitions for examples:
- `StepDefinitions/Bookings/BookingSteps.cs`
- `StepDefinitions/Providers/ProviderSteps.cs`
- `StepDefinitions/Payments/BehpardakhtSteps.cs`
