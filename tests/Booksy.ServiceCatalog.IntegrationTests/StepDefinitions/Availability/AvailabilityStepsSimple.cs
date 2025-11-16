using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Reqnroll;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Availability;

/// <summary>
/// Step definitions for availability-related scenarios - Minimal implementation
/// </summary>
[Binding]
public class AvailabilityStepsSimple
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;

    public AvailabilityStepsSimple(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
    }

    [Given(@"the provider has business hours configured for (\d+) (AM|PM) to (\d+) (AM|PM)")]
    public void GivenTheProviderHasBusinessHoursConfiguredFor(int openHour, string openPeriod, int closeHour, string closePeriod)
    {
        // Implementation pending - business hours are set during provider creation in TestDataSteps
        // This step is for more specific control which we can add later
    }

    [Given(@"the provider has (\d+) staff members")]
    public void GivenTheProviderHasStaffMembers(int staffCount)
    {
        _scenarioContext.Pending();
    }

    [Given(@"there is a booking at (.*) with staff member (\d+)")]
    public void GivenThereIsABookingAtWithStaffMember(string time, int staffNumber)
    {
        _scenarioContext.Pending();
    }

    [Given(@"there is a booking at (.*)")]
    public void GivenThereIsABookingAt(string time)
    {
        _scenarioContext.Pending();
    }

    [Given(@"the provider has a break scheduled from (.*) to (.*)")]
    public void GivenTheProviderHasABreakScheduledFromTo(string startTime, string endTime)
    {
        _scenarioContext.Pending();
    }

    [Given(@"the provider has marked tomorrow as a holiday")]
    public void GivenTheProviderHasMarkedTomorrowAsAHoliday()
    {
        _scenarioContext.Pending();
    }

    [Given(@"the provider has an exception for tomorrow: (.*) to (.*) only")]
    public void GivenTheProviderHasAnExceptionForTomorrow(string startTime, string endTime)
    {
        _scenarioContext.Pending();
    }

    [Given(@"the service duration is (\d+) minutes")]
    public void GivenTheServiceDurationIsMinutes(int durationMinutes)
    {
        _scenarioContext.Pending();
    }

    [Given(@"the service has a lead time of (\d+) hours")]
    public void GivenTheServiceHasALeadTimeOfHours(int leadTimeHours)
    {
        _scenarioContext.Pending();
    }

    [Given(@"the service has a buffer time of (\d+) minutes")]
    public void GivenTheServiceHasABufferTimeOfMinutes(int bufferMinutes)
    {
        _scenarioContext.Pending();
    }

    [Given(@"the provider is closed on (.*)")]
    public void GivenTheProviderIsClosedOn(string day)
    {
        _scenarioContext.Pending();
    }

    [When(@"I send a GET request to check availability for (\d+) days from now")]
    public void WhenISendAGetRequestToCheckAvailabilityForDaysFromNow(int daysFromNow)
    {
        _scenarioContext.Pending();
    }

    [When(@"I check availability for (.*) tomorrow")]
    public void WhenICheckAvailabilityForTomorrow(string time)
    {
        _scenarioContext.Pending();
    }

    [When(@"I check availability for yesterday")]
    public void WhenICheckAvailabilityForYesterday()
    {
        _scenarioContext.Pending();
    }

    [When(@"I check availability for a non-existent provider")]
    public void WhenICheckAvailabilityForANonExistentProvider()
    {
        _scenarioContext.Pending();
    }

    [When(@"I check availability for a non-existent service")]
    public void WhenICheckAvailabilityForANonExistentService()
    {
        _scenarioContext.Pending();
    }

    [When(@"I check availability for tomorrow")]
    public void WhenICheckAvailabilityForTomorrow()
    {
        _scenarioContext.Pending();
    }

    [Then(@"the response should contain available time slots")]
    public void ThenTheResponseShouldContainAvailableTimeSlots()
    {
        _scenarioContext.Pending();
    }

    [Then(@"all slots should be within business hours")]
    public void ThenAllSlotsShouldBeWithinBusinessHours()
    {
        _scenarioContext.Pending();
    }

    [Then(@"the (.*) slot should not be available")]
    public void ThenTheSlotShouldNotBeAvailable(string slot)
    {
        _scenarioContext.Pending();
    }

    [Then(@"other slots should remain available")]
    public void ThenOtherSlotsShouldRemainAvailable()
    {
        _scenarioContext.Pending();
    }

    [Then(@"the (.*) slot should still be available with other staff")]
    public void ThenTheSlotShouldStillBeAvailableWithOtherStaff(string slot)
    {
        _scenarioContext.Pending();
    }

    [Then(@"the error should indicate past dates not allowed")]
    public void ThenTheErrorShouldIndicatePastDatesNotAllowed()
    {
        _scenarioContext.Pending();
    }

    [Then(@"the response should contain no available slots")]
    public void ThenTheResponseShouldContainNoAvailableSlots()
    {
        _scenarioContext.Pending();
    }

    [Then(@"slots between (.*) and (.*) should not be available")]
    public void ThenSlotsBetweenAndShouldNotBeAvailable(string start, string end)
    {
        _scenarioContext.Pending();
    }

    [Then(@"only slots between (.*) and (.*) should be available")]
    public void ThenOnlySlotsBetweenAndShouldBeAvailable(string start, string end)
    {
        _scenarioContext.Pending();
    }

    [Then(@"only slots ending before (.*) should be available")]
    public void ThenOnlySlotsEndingBeforeShouldBeAvailable(string time)
    {
        _scenarioContext.Pending();
    }

    [Then(@"slots within the next (\d+) hours should not be available")]
    public void ThenSlotsWithinTheNextHoursShouldNotBeAvailable(int hours)
    {
        _scenarioContext.Pending();
    }

    [Then(@"the (.*) slot should be available")]
    public void ThenTheSlotShouldBeAvailable(string slot)
    {
        _scenarioContext.Pending();
    }
}
