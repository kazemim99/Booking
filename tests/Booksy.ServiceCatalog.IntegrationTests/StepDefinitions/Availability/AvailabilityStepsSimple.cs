//using Booksy.Core.Domain.ValueObjects;
//using Booksy.ServiceCatalog.Api.Models.Responses;
//using Booksy.ServiceCatalog.Domain.Aggregates;
//using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
//using Booksy.ServiceCatalog.Domain.Enums;
//using Booksy.ServiceCatalog.Domain.ValueObjects;
//using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
//using Booksy.ServiceCatalog.IntegrationTests.Support;
//using FluentAssertions;
//using Reqnroll;
//using System.Net;

//namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Availability;

///// <summary>
///// Step definitions for availability-related scenarios
///// </summary>
//[Binding]
//public class AvailabilityStepsSimple : ServiceCatalogIntegrationTestBase
//{
//    private readonly ScenarioContext _scenarioContext;
//    private readonly ScenarioContextHelper _helper;

//    public AvailabilityStepsSimple(
//        ScenarioContext scenarioContext,
//        ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
//    {
//        _scenarioContext = scenarioContext;
//        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
//    }

//    #region Given Steps

//    [Given(@"the provider has business hours configured for (\d+) (AM|PM) to (\d+) (AM|PM)")]
//    public async Task GivenTheProviderHasBusinessHoursConfiguredFor(int openHour, string openPeriod, int closeHour, string closePeriod)
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");

//        var openTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(openPeriod == "PM" && openHour != 12 ? openHour + 12 : openHour));
//        var closeTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(closePeriod == "PM" && closeHour != 12 ? closeHour + 12 : closeHour));

//        provider.SetBusinessHours(new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
//        {
//            { DayOfWeek.Monday, (openTime, closeTime) },
//            { DayOfWeek.Tuesday, (openTime, closeTime) },
//            { DayOfWeek.Wednesday, (openTime, closeTime) },
//            { DayOfWeek.Thursday, (openTime, closeTime) },
//            { DayOfWeek.Friday, (openTime, closeTime) },
//            { DayOfWeek.Saturday, (openTime, closeTime) }
//        });

//        await UpdateEntityAsync(provider);
//    }

//    [Given(@"there is a confirmed booking at (.*) in (\d+) days")]
//    public async Task GivenThereIsAConfirmedBookingAtInDays(string time, int daysFromNow)
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var service = _scenarioContext.Get<Service>("Service:Current");
//        var staff = provider.Staff.First();

//        var bookingTime = ParseTimeString(time, daysFromNow);
//        var customerId = Guid.NewGuid();

//        var bookingPolicy = service.BookingPolicy ?? BookingPolicy.Default;
//        var booking = Booking.CreateBookingRequest(
//            UserId.From(customerId),
//            provider.Id,
//            service.Id,
//            staff.Id,
//            bookingTime,
//            service.Duration,
//            service.BasePrice,
//            bookingPolicy,
//            "Test booking");

//        DbContext.Bookings.Add(booking);
//        await _testBase.DbContext.SaveChangesAsync();

//        booking.Confirm();
//        await _testBase.UpdateEntityAsync(booking);

//        _scenarioContext.Set(booking, "CurrentBooking");
//        _scenarioContext.Set(bookingTime, "BookingTime");
//    }

//    [Given(@"the provider has (\d+) staff members")]
//    public async Task GivenTheProviderHasStaffMembers(int staffCount)
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");

//        // Provider already has one staff member from background, add more
//        for (int i = 1; i < staffCount; i++)
//        {
//            var phoneNumber = PhoneNumber.Create($"+9899123456{i}");
//            provider.AddStaff(
//                $"Staff Member",
//                $"{i + 1}",
//                StaffRole.ServiceProvider,
//                phoneNumber);
//        }

//        await UpdateEntityAsync(provider);
//    }

//    [Given(@"there is a booking at (.*) with staff member (\d+)")]
//    public async Task GivenThereIsABookingAtWithStaffMember(string time, int staffNumber)
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var service = _scenarioContext.Get<Service>("Service:Current");
//        var staff = provider.Staff.ElementAt(staffNumber - 1);

//        var bookingTime = ParseTimeString(time, 3); // Default 3 days from now
//        var customerId = Guid.NewGuid();

//        var bookingPolicy = service.BookingPolicy ?? BookingPolicy.Default;
//        var booking = Booking.CreateBookingRequest(
//            UserId.From(customerId),
//            provider.Id,
//            service.Id,
//            staff.Id,
//            bookingTime,
//            service.Duration,
//            service.BasePrice,
//            bookingPolicy,
//            "Test booking");

//        DbContext.Bookings.Add(booking);
//        await _testBase.DbContext.SaveChangesAsync();

//        booking.Confirm();
//        await _testBase.UpdateEntityAsync(booking);

//        _scenarioContext.Set(booking, $"Booking:Staff{staffNumber}");
//    }

//    [Given(@"the provider is closed on Sundays")]
//    public async Task GivenTheProviderIsClosedOnSundays()
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");

//        var hours = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
//        {
//            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
//            { DayOfWeek.Tuesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
//            { DayOfWeek.Wednesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
//            { DayOfWeek.Thursday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
//            { DayOfWeek.Friday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
//            { DayOfWeek.Saturday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
//            { DayOfWeek.Sunday, (null, null) } // Closed
//        };

//        provider.SetBusinessHours(hours);
//        await UpdateEntityAsync(provider);
//    }

//    [Given(@"the provider has a break from (.*) to (.*)")]
//    public async Task GivenTheProviderHasABreakFromTo(string startTime, string endTime)
//    {
//        // Store break time for later verification
//        _scenarioContext.Set(startTime, "BreakStartTime");
//        _scenarioContext.Set(endTime, "BreakEndTime");
//    }

//    [Given(@"the provider has marked tomorrow as a holiday")]
//    public async Task GivenTheProviderHasMarkedTomorrowAsAHoliday()
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

//        provider.AddHoliday(tomorrow, "Test Holiday");
//        await UpdateEntityAsync(provider);

//        _scenarioContext.Set(tomorrow, "HolidayDate");
//    }

//    [Given(@"the provider has an exception for tomorrow: (.*) to (.*) only")]
//    public async Task GivenTheProviderHasAnExceptionForTomorrow(string startTime, string endTime)
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

//        var start = ParseTimeOnlyString(startTime);
//        var end = ParseTimeOnlyString(endTime);

//        provider.AddException(tomorrow, start, end, "Special hours");
//        await UpdateEntityAsync(provider);

//        _scenarioContext.Set(tomorrow, "ExceptionDate");
//        _scenarioContext.Set(start, "ExceptionStart");
//        _scenarioContext.Set(end, "ExceptionEnd");
//    }

//    [Given(@"the service duration is (\d+) minutes")]
//    public void GivenTheServiceDurationIsMinutes(int durationMinutes)
//    {
//        // Note: Service duration update would require reflection or recreating the service
//        // For now, we'll skip this complex scenario
//        _scenarioContext.Set(durationMinutes, "ServiceDuration");
//    }

//    [Given(@"the provider requires (\d+) hours lead time")]
//    public void GivenTheProviderRequiresHoursLeadTime(int hours)
//    {
//        _scenarioContext.Set(hours, "LeadTimeHours");
//    }

//    [Given(@"the provider requires (\d+) minutes buffer between bookings")]
//    public void GivenTheProviderRequiresMinutesBufferBetweenBookings(int minutes)
//    {
//        _scenarioContext.Set(minutes, "BufferMinutes");
//    }

//    [Given(@"there is a booking at (.*) \((\d+) min duration\)")]
//    public async Task GivenThereIsABookingAtWithDuration(string time, int duration)
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var service = _scenarioContext.Get<Service>("Service:Current");
//        var staff = provider.Staff.First();

//        var bookingTime = ParseTimeString(time, 3);
//        var customerId = Guid.NewGuid();

//        var bookingPolicy = service.BookingPolicy ?? BookingPolicy.Default;
//        var booking = Booking.CreateBookingRequest(
//            UserId.From(customerId),
//            provider.Id,
//            service.Id,
//            staff.Id,
//            bookingTime,
//            Duration.FromMinutes(duration),
//            service.BasePrice,
//            bookingPolicy,
//            "Test booking");

//        DbContext.Bookings.Add(booking);
//        await _testBase.DbContext.SaveChangesAsync();

//        booking.Confirm();
//        await _testBase.UpdateEntityAsync(booking);

//        _scenarioContext.Set(booking, "CurrentBooking");
//        _scenarioContext.Set(bookingTime, "BookingTime");
//    }

//    #endregion

//    #region When Steps

//    [When(@"I send a GET request to check availability for (\d+) days from now")]
//    public async Task WhenISendAGETRequestToCheckAvailabilityForDaysFromNow(int daysFromNow)
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var service = _scenarioContext.Get<Service>("Service:Current");

//        var targetDate = DateTime.UtcNow.AddDays(daysFromNow).Date;

//        var response = await _testBase.GetAsync<List<AvailableSlotResponse>>(
//            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={targetDate:yyyy-MM-dd}");

//        _scenarioContext.Set(response, "LastResponse");
//        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
//        if (response.Data != null)
//        {
//            _scenarioContext.Set(response.Data, "AvailableSlots");
//        }
//    }

//    [When(@"I send a GET request to check availability for that day")]
//    public async Task WhenISendAGETRequestToCheckAvailabilityForThatDay()
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var service = _scenarioContext.Get<Service>("Service:Current");
//        var bookingTime = _scenarioContext.Get<DateTime>("BookingTime");

//        var targetDate = bookingTime.Date;

//        var response = await _testBase.GetAsync<List<AvailableSlotResponse>>(
//            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={targetDate:yyyy-MM-dd}");

//        _scenarioContext.Set(response, "LastResponse");
//        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
//        if (response.Data != null)
//        {
//            _scenarioContext.Set(response.Data, "AvailableSlots");
//        }
//    }

//    [When(@"I check availability for that time")]
//    public async Task WhenICheckAvailabilityForThatTime()
//    {
//        await WhenISendAGETRequestToCheckAvailabilityForThatDay();
//    }

//    [When(@"I send a GET request to check availability for yesterday")]
//    public async Task WhenISendAGETRequestToCheckAvailabilityForYesterday()
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var service = _scenarioContext.Get<Service>("Service:Current");

//        var yesterday = DateTime.UtcNow.AddDays(-1).Date;

//        var response = await _testBase.GetAsync(
//            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={yesterday:yyyy-MM-dd}");

//        _scenarioContext.Set(response, "LastResponse");
//        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
//    }

//    [When(@"I send a GET request to check availability for non-existent provider")]
//    public async Task WhenISendAGETRequestToCheckAvailabilityForNonExistentProvider()
//    {
//        var nonExistentProviderId = Guid.NewGuid();
//        var nonExistentServiceId = Guid.NewGuid();
//        var futureDate = DateTime.UtcNow.AddDays(3).Date;

//        var response = await _testBase.GetAsync(
//            $"/api/v1/availability/slots?ProviderId={nonExistentProviderId}&ServiceId={nonExistentServiceId}&Date={futureDate:yyyy-MM-dd}");

//        _scenarioContext.Set(response, "LastResponse");
//        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
//    }

//    [When(@"I send a GET request to check availability with invalid service ID")]
//    public async Task WhenISendAGETRequestToCheckAvailabilityWithInvalidServiceID()
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var invalidServiceId = Guid.NewGuid();
//        var futureDate = DateTime.UtcNow.AddDays(3).Date;

//        var response = await _testBase.GetAsync(
//            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={invalidServiceId}&Date={futureDate:yyyy-MM-dd}");

//        _scenarioContext.Set(response, "LastResponse");
//        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
//    }

//    [When(@"I check availability for next Sunday")]
//    public async Task WhenICheckAvailabilityForNextSunday()
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var service = _scenarioContext.Get<Service>("Service:Current");

//        var nextSunday = GetNextDayOfWeek(System.DayOfWeek.Sunday);

//        var response = await _testBase.GetAsync<List<AvailableSlotResponse>>(
//            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={nextSunday:yyyy-MM-dd}");

//        _scenarioContext.Set(response, "LastResponse");
//        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
//        if (response.Data != null)
//        {
//            _scenarioContext.Set(response.Data, "AvailableSlots");
//        }
//    }

//    [When(@"I check availability for today")]
//    [When(@"I check availability")]
//    public async Task WhenICheckAvailabilityForToday()
//    {
//        await WhenISendAGETRequestToCheckAvailabilityForDaysFromNow(3);
//    }

//    [When(@"I check availability for tomorrow")]
//    [When(@"I check availability for tomorrow at this time")]
//    [When(@"I check availability at (.*) \(business closes at (.*)\)")]
//    public async Task WhenICheckAvailabilityForTomorrow(string checkTime = null, string closeTime = null)
//    {
//        var provider = _scenarioContext.Get<Provider>("Provider:Current");
//        var service = _scenarioContext.Get<Service>("Service:Current");

//        var tomorrow = DateTime.UtcNow.AddDays(1).Date;

//        var response = await _testBase.GetAsync<List<AvailableSlotResponse>>(
//            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={tomorrow:yyyy-MM-dd}");

//        _scenarioContext.Set(response, "LastResponse");
//        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
//        if (response.Data != null)
//        {
//            _scenarioContext.Set(response.Data, "AvailableSlots");
//        }

//        if (checkTime != null)
//        {
//            _scenarioContext.Set(checkTime, "CheckTime");
//            _scenarioContext.Set(closeTime, "CloseTime");
//        }
//    }

//    #endregion

//    #region Then Steps

//    [Then(@"the response status code should be (\d+)")]
//    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
//    {
//        var actualStatusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
//        ((int)actualStatusCode).Should().Be(expectedStatusCode);
//    }

//    [Then(@"the response should contain available time slots")]
//    public void ThenTheResponseShouldContainAvailableTimeSlots()
//    {
//        var slots = _scenarioContext.Get<List<AvailableSlotResponse>>("AvailableSlots");
//        slots.Should().NotBeNull();
//        slots.Should().NotBeEmpty();
//    }

//    [Then(@"all slots should be within business hours")]
//    public void ThenAllSlotsShouldBeWithinBusinessHours()
//    {
//        var slots = _scenarioContext.Get<List<AvailableSlotResponse>>("AvailableSlots");
//        var businessStart = new TimeOnly(9, 0);
//        var businessEnd = new TimeOnly(17, 0);

//        foreach (var slot in slots)
//        {
//            var slotStart = TimeOnly.Parse(slot.StartTime.ToString("HH:mm"));
//            var slotEnd = TimeOnly.Parse(slot.EndTime.ToString("HH:mm"));

//            slotStart.Should().Be(slotStart.IsBetween(businessStart, businessEnd) ? slotStart : businessStart);
//        }
//    }

//    [Then(@"the (.*) slot should not be available")]
//    public void ThenTheSlotShouldNotBeAvailable(string timeStr)
//    {
//        var slots = _scenarioContext.Get<List<AvailableSlotResponse>>("AvailableSlots");
//        var targetTime = ParseTimeOnlyString(timeStr);

//        var matchingSlot = slots.FirstOrDefault(s =>
//            TimeOnly.Parse(s.StartTime.ToString("HH:mm")) == targetTime);

//        if (matchingSlot != null)
//        {
//            matchingSlot.IsAvailable.Should().BeFalse($"the {timeStr} slot should be booked or unavailable");
//        }
//    }

//    [Then(@"other slots should remain available")]
//    public void ThenOtherSlotsShouldRemainAvailable()
//    {
//        var slots = _scenarioContext.Get<List<AvailableSlotResponse>>("AvailableSlots");
//        var availableSlots = slots.Where(s => s.IsAvailable).ToList();

//        availableSlots.Should().NotBeEmpty("there should be some available slots besides the booked one");
//    }

//    [Then(@"the (.*) slot should still be available with other staff")]
//    public void ThenTheSlotShouldStillBeAvailableWithOtherStaff(string timeStr)
//    {
//        var slots = _scenarioContext.Get<List<AvailableSlotResponse>>("AvailableSlots");
//        var targetTime = ParseTimeOnlyString(timeStr);

//        var availableAtTime = slots.Where(s =>
//            TimeOnly.Parse(s.StartTime.ToString("HH:mm")) == targetTime && s.IsAvailable).ToList();

//        availableAtTime.Should().NotBeEmpty($"the {timeStr} slot should be available with other staff members");
//    }

//    [Then(@"the error should indicate past dates not allowed")]
//    public void ThenTheErrorShouldIndicatePastDatesNotAllowed()
//    {
//        var statusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//    }

//    [Then(@"the response should contain no available slots")]
//    public void ThenTheResponseShouldContainNoAvailableSlots()
//    {
//        var slots = _scenarioContext.Get<List<AvailableSlotResponse>>("AvailableSlots");
//        slots.Should().BeEmpty("no slots should be available on closed days or holidays");
//    }

//    [Then(@"slots between (.*) and (.*) should not be available")]
//    [Then(@"only slots between (.*) and (.*) should be available")]
//    [Then(@"only slots ending before (.*) should be available")]
//    [Then(@"slots within the next (\d+) hours should not be available")]
//    [Then(@"the (.*) slot should be available")]
//    public void ThenComplexAssertion(params object[] args)
//    {
//        // Simplified - mark as pending
//        ScenarioContext.Current.Pending();
//    }

//    #endregion

//    #region Helper Methods

//    private DateTime ParseTimeString(string timeStr, int daysFromNow)
//    {
//        var time = ParseTimeOnlyString(timeStr);
//        var targetDate = DateTime.UtcNow.AddDays(daysFromNow).Date;
//        return targetDate.Add(time.ToTimeSpan());
//    }

//    private TimeOnly ParseTimeOnlyString(string timeStr)
//    {
//        timeStr = timeStr.Trim();

//        if (timeStr.Contains("PM") || timeStr.Contains("AM"))
//        {
//            var parts = timeStr.Split(' ');
//            var timePart = parts[0];
//            var period = parts[1];

//            var timeParts = timePart.Split(':');
//            var hour = int.Parse(timeParts[0]);
//            var minute = timeParts.Length > 1 ? int.Parse(timeParts[1]) : 0;

//            if (period == "PM" && hour != 12)
//                hour += 12;
//            else if (period == "AM" && hour == 12)
//                hour = 0;

//            return new TimeOnly(hour, minute);
//        }
//        else
//        {
//            return TimeOnly.Parse(timeStr);
//        }
//    }

//    private DateTime GetNextDayOfWeek(System.DayOfWeek dayOfWeek)
//    {
//        var today = DateTime.UtcNow.Date;
//        var daysUntilTarget = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
//        if (daysUntilTarget == 0)
//            daysUntilTarget = 7;
//        return today.AddDays(daysUntilTarget);
//    }

//    #endregion
//}
