using Booksy.API;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetBusinessHours;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests;

/// <summary>
/// Integration tests for Working Hours Management APIs
/// Tests business hours, breaks, holidays, exceptions, and availability calculation
/// </summary>
[Collection("Integration Tests")]
public class WorkingHoursManagementTests : ServiceCatalogIntegrationTestBase
{
    public WorkingHoursManagementTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    #region Business Hours Tests

    [Fact]
    public async Task GetBusinessHours_WithValidProvider_ReturnsAllSevenDays()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/business-hours");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<BusinessHoursViewModel>(response);
        result.Should().NotBeNull();
        result!.BusinessHours.Should().HaveCount(7, "should return all 7 days of the week");
    }

    [Fact]
    public async Task GetBusinessHours_WithNonExistentProvider_ReturnsNotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/providers/{nonExistentId}/business-hours");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBusinessHours_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        var updateRequest = new UpdateBusinessHoursRequestDto
        {
            BusinessHours = new List<BusinessHoursDayDto>
            {
                new() { DayOfWeek = 1, IsOpen = true, OpenTime = "09:00", CloseTime = "18:00", Breaks = null },
                new() { DayOfWeek = 2, IsOpen = true, OpenTime = "09:00", CloseTime = "18:00", Breaks = null },
                new() { DayOfWeek = 3, IsOpen = true, OpenTime = "09:00", CloseTime = "18:00", Breaks = null },
                new() { DayOfWeek = 4, IsOpen = true, OpenTime = "09:00", CloseTime = "18:00", Breaks = null },
                new() { DayOfWeek = 5, IsOpen = true, OpenTime = "09:00", CloseTime = "17:00", Breaks = null },
                new() { DayOfWeek = 6, IsOpen = true, OpenTime = "10:00", CloseTime = "14:00", Breaks = null },
                new() { DayOfWeek = 0, IsOpen = false, OpenTime = null, CloseTime = null, Breaks = null }
            }
        };

        // Act
        var response = await PutAsJsonAsync<UpdateBusinessHoursRequestDto, UpdateBusinessHoursResultDto>(
            $"/api/v1/providers/{provider.Id.Value}/business-hours",
            updateRequest);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Error.Should().BeNull();
        response.Data.Should().NotBeNull();
        response.Data!.Success.Should().BeTrue();

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        updatedProvider.Should().NotBeNull();
        updatedProvider!.BusinessHours.Should().HaveCount(7);

        var friday = updatedProvider.BusinessHours.FirstOrDefault(h => h.DayOfWeek == Domain.Enums.DayOfWeek.Friday);
        friday.Should().NotBeNull();
        friday!.CloseTime.Should().Be(new TimeOnly(17, 0));
    }

    [Fact]
    public async Task UpdateBusinessHours_WithBreaks_SavesBreakPeriodsSuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        var updateRequest = new UpdateBusinessHoursRequestDto
        {
            BusinessHours = new List<BusinessHoursDayDto>
            {
                new()
                {
                    DayOfWeek = 1,
                    IsOpen = true,
                    OpenTime = "09:00",
                    CloseTime = "17:00",
                    Breaks = new List<BreakPeriodDto>
                    {
                        new() { StartTime = "12:00", EndTime = "13:00", Label = "Lunch Break" },
                        new() { StartTime = "15:00", EndTime = "15:15", Label = "Coffee Break" }
                    }
                },
                new() { DayOfWeek = 2, IsOpen = true, OpenTime = "09:00", CloseTime = "17:00", Breaks = null },
                new() { DayOfWeek = 3, IsOpen = true, OpenTime = "09:00", CloseTime = "17:00", Breaks = null },
                new() { DayOfWeek = 4, IsOpen = true, OpenTime = "09:00", CloseTime = "17:00", Breaks = null },
                new() { DayOfWeek = 5, IsOpen = true, OpenTime = "09:00", CloseTime = "17:00", Breaks = null },
                new() { DayOfWeek = 6, IsOpen = false, OpenTime = null, CloseTime = null, Breaks = null },
                new() { DayOfWeek = 0, IsOpen = false, OpenTime = null, CloseTime = null, Breaks = null }
            }
        };

        // Act
        var response = await PutAsJsonAsync<UpdateBusinessHoursRequestDto, UpdateBusinessHoursResultDto>(
            $"/api/v1/providers/{provider.Id.Value}/business-hours",
            updateRequest);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Error.Should().BeNull();

        // Verify breaks in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        var monday = updatedProvider!.BusinessHours.FirstOrDefault(h => h.DayOfWeek == Domain.Enums.DayOfWeek.Monday);
        monday.Should().NotBeNull();
        monday!.Breaks.Should().HaveCount(2);
        monday.Breaks.Should().Contain(b => b.Label == "Lunch Break");
        monday.Breaks.Should().Contain(b => b.Label == "Coffee Break");
    }

    [Fact]
    public async Task UpdateBusinessHours_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        ClearAuthenticationHeader();

        var updateRequest = new UpdateBusinessHoursRequestDto
        {
            BusinessHours = new List<BusinessHoursDayDto>
            {
                new() { DayOfWeek = 1, IsOpen = true, OpenTime = "09:00", CloseTime = "17:00", Breaks = null }
            }
        };

        // Act
        var response = await PutAsJsonAsync<UpdateBusinessHoursRequestDto, UpdateBusinessHoursResultDto>(
            $"/api/v1/providers/{provider.Id.Value}/business-hours",
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateBusinessHours_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var provider1 = await CreateProviderWithBusinessHoursAsync();
        var provider2 = await CreateAndAuthenticateAsProviderAsync("Other Provider", "other@test.com");

        // Authenticate as provider2
        AuthenticateAsProviderOwner(provider2);

        var updateRequest = new UpdateBusinessHoursRequestDto
        {
            BusinessHours = new List<BusinessHoursDayDto>
            {
                new() { DayOfWeek = 1, IsOpen = true, OpenTime = "09:00", CloseTime = "17:00", Breaks = null }
            }
        };

        // Act - trying to update provider1's hours
        var response = await PutAsJsonAsync<UpdateBusinessHoursRequestDto, UpdateBusinessHoursResultDto>(
            $"/api/v1/providers/{provider1.Id.Value}/business-hours",
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Holiday Tests

    [Fact]
    public async Task GetHolidays_WithValidProvider_ReturnsHolidayList()
    {
        // Arrange
        var provider = await CreateProviderWithHolidaysAsync();
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/holidays");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<HolidaysViewModel>(response);
        result.Should().NotBeNull();
        result!.Holidays.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddHoliday_WithValidData_CreatesHolidaySuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        var addRequest = new AddHolidayRequestDto
        {
            Date = new DateOnly(2025, 12, 25),
            Reason = "Christmas Day",
            IsRecurring = false,
            Pattern = null
        };

        // Act
        var response = await PostAsJsonAsync<AddHolidayRequestDto, AddHolidayResultDto>(
            $"/api/v1/providers/{provider.Id.Value}/holidays",
            addRequest);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Error.Should().BeNull();
        response.Data.Should().NotBeNull();
        response.Data!.Success.Should().BeTrue();
        response.Data.HolidayId.Should().NotBeEmpty();

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        updatedProvider!.Holidays.Should().Contain(h => h.Reason == "Christmas Day");
    }

    [Fact]
    public async Task AddHoliday_WithRecurringPattern_CreatesRecurringHoliday()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        var addRequest = new AddHolidayRequestDto
        {
            Date = new DateOnly(2025, 1, 1),
            Reason = "New Year's Day",
            IsRecurring = true,
            Pattern = "Yearly"
        };

        // Act
        var response = await PostAsJsonAsync<AddHolidayRequestDto, AddHolidayResultDto>(
            $"/api/v1/providers/{provider.Id.Value}/holidays",
            addRequest);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Error.Should().BeNull();

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        var holiday = updatedProvider!.Holidays.FirstOrDefault(h => h.Reason == "New Year's Day");
        holiday.Should().NotBeNull();
        holiday!.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteHoliday_WithValidId_RemovesHolidaySuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithHolidaysAsync();
        AuthenticateAsProviderOwner(provider);

        var holidayToDelete = provider.Holidays.First();

        // Act
        var response = await Client.DeleteAsync(
            $"/api/v1/providers/{provider.Id.Value}/holidays/{holidayToDelete.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        updatedProvider!.Holidays.Should().NotContain(h => h.Id == holidayToDelete.Id);
    }

    #endregion

    #region Exception Schedule Tests

    [Fact]
    public async Task GetExceptions_WithValidProvider_ReturnsExceptionList()
    {
        // Arrange
        var provider = await CreateProviderWithExceptionsAsync();
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/exceptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<ExceptionsViewModel>(response);
        result.Should().NotBeNull();
        result!.Exceptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddException_WithModifiedHours_CreatesExceptionSuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        var addRequest = new AddExceptionRequestDto
        {
            Date = new DateOnly(2025, 12, 24),
            OpenTime = new TimeOnly(10, 0),
            CloseTime = new TimeOnly(14, 0),
            Reason = "Christmas Eve - Early Closing"
        };

        // Act
        var response = await PostAsJsonAsync<AddExceptionRequestDto, AddExceptionResultDto>(
            $"/api/v1/providers/{provider.Id.Value}/exceptions",
            addRequest);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Error.Should().BeNull();
        response.Data.Should().NotBeNull();
        response.Data!.Success.Should().BeTrue();
        response.Data.ExceptionId.Should().NotBeEmpty();

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        updatedProvider!.Exceptions.Should().Contain(e => e.Reason == "Christmas Eve - Early Closing");
    }

    [Fact]
    public async Task AddException_WithClosedDay_CreatesClosedException()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        var addRequest = new AddExceptionRequestDto
        {
            Date = new DateOnly(2025, 11, 1),
            OpenTime = null,
            CloseTime = null,
            Reason = "Staff Training Day"
        };

        // Act
        var response = await PostAsJsonAsync<AddExceptionRequestDto, AddExceptionResultDto>(
            $"/api/v1/providers/{provider.Id.Value}/exceptions",
            addRequest);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Error.Should().BeNull();

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        var exception = updatedProvider!.Exceptions.FirstOrDefault(e => e.Reason == "Staff Training Day");
        exception.Should().NotBeNull();
        exception!.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteException_WithValidId_RemovesExceptionSuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithExceptionsAsync();
        AuthenticateAsProviderOwner(provider);

        var exceptionToDelete = provider.Exceptions.First();

        // Act
        var response = await Client.DeleteAsync(
            $"/api/v1/providers/{provider.Id.Value}/exceptions/{exceptionToDelete.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        updatedProvider!.Exceptions.Should().NotContain(e => e.Id == exceptionToDelete.Id);
    }

    #endregion

    #region Availability Calculation Tests

    [Fact]
    public async Task GetAvailability_OnRegularDay_ReturnsAvailableWithBusinessHours()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        var date = "2025-11-03"; // A Monday

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/availability?date={date}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<AvailabilityViewModel>(response);
        result.Should().NotBeNull();
        result!.IsAvailable.Should().BeTrue();
        result.Reason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetAvailability_OnHoliday_ReturnsUnavailableWithReason()
    {
        // Arrange
        var provider = await CreateProviderWithHolidaysAsync();
        AuthenticateAsProviderOwner(provider);

        var date = "2025-12-25"; // Christmas

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/availability?date={date}");

        // Assert
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<AvailabilityViewModel>(response);
        result.Should().NotBeNull();
        result!.IsAvailable.Should().BeFalse();
        result.Reason.Should().Contain("Holiday");
    }

    [Fact]
    public async Task GetAvailability_OnExceptionDay_ReturnsExceptionHours()
    {
        // Arrange
        var provider = await CreateProviderWithExceptionsAsync();
        AuthenticateAsProviderOwner(provider);

        var date = "2025-12-24"; // Christmas Eve with exception

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/availability?date={date}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<AvailabilityViewModel>(response);
        result.Should().NotBeNull();
        result!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailability_WithInvalidDateFormat_ReturnsBadRequest()
    {
        // Arrange
        var provider = await CreateProviderWithBusinessHoursAsync();
        AuthenticateAsProviderOwner(provider);

        var invalidDate = "not-a-date";

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/availability?date={invalidDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Methods

    private async Task<Provider> CreateProviderWithBusinessHoursAsync()
    {
        var createdProvider = await CreateAndAuthenticateAsProviderAsync();

        // Reload the provider from the database to ensure clean tracking
        var provider = await FindProviderAsync(createdProvider.Id.Value);
        if (provider == null)
            throw new InvalidOperationException("Provider not found after creation");

        // Set standard business hours (Mon-Fri 9-5, Sat 10-2, Sun closed)
        var hours = new Dictionary<Domain.Enums.DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
        {
            { Domain.Enums.DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { Domain.Enums.DayOfWeek.Tuesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { Domain.Enums.DayOfWeek.Wednesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { Domain.Enums.DayOfWeek.Thursday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { Domain.Enums.DayOfWeek.Friday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { Domain.Enums.DayOfWeek.Saturday, (new TimeOnly(10, 0), new TimeOnly(14, 0)) },
            { Domain.Enums.DayOfWeek.Sunday, (null, null) }
        };

        provider.SetBusinessHours(hours);

        await DbContext.SaveChangesAsync();

        // Detach provider and all child entities to avoid tracking conflicts when modifying later
        DbContext.Entry(provider).State = EntityState.Detached;

        // Detach all BusinessHours entities
        foreach (var businessHour in provider.BusinessHours)
        {
            DbContext.Entry(businessHour).State = EntityState.Detached;
        }

        return provider;
    }

    private async Task<Provider> CreateProviderWithHolidaysAsync()
    {
        var provider = await CreateProviderWithBusinessHoursAsync();

        // Re-attach the detached entity and all child entities
        DbContext.Attach(provider);
        foreach (var businessHour in provider.BusinessHours)
        {
            DbContext.Attach(businessHour);
        }

        provider.AddHoliday(new DateOnly(2025, 12, 25), "Christmas Day");
        provider.AddRecurringHoliday(new DateOnly(2025, 1, 1), "New Year's Day", RecurrencePattern.Yearly);

        await DbContext.SaveChangesAsync();

        // Detach again - provider and all child entities
        DbContext.Entry(provider).State = EntityState.Detached;
        foreach (var businessHour in provider.BusinessHours)
        {
            DbContext.Entry(businessHour).State = EntityState.Detached;
        }
        foreach (var holiday in provider.Holidays)
        {
            DbContext.Entry(holiday).State = EntityState.Detached;
        }

        return provider;
    }

    private async Task<Provider> CreateProviderWithExceptionsAsync()
    {
        var provider = await CreateProviderWithBusinessHoursAsync();

        // Re-attach the detached entity and all child entities
        DbContext.Attach(provider);
        foreach (var businessHour in provider.BusinessHours)
        {
            DbContext.Attach(businessHour);
        }

        provider.AddException(
            new DateOnly(2025, 12, 24),
            new TimeOnly(10, 0),
            new TimeOnly(14, 0),
            "Christmas Eve - Early Closing");

        provider.AddClosedException(
            new DateOnly(2025, 11, 1),
            "Staff Training Day");

        await DbContext.SaveChangesAsync();

        // Detach again - provider and all child entities
        DbContext.Entry(provider).State = EntityState.Detached;
        foreach (var businessHour in provider.BusinessHours)
        {
            DbContext.Entry(businessHour).State = EntityState.Detached;
        }
        foreach (var exception in provider.Exceptions)
        {
            DbContext.Entry(exception).State = EntityState.Detached;
        }

        return provider;
    }

    #endregion
}

#region DTOs for Tests

// Request DTOs
public record UpdateBusinessHoursRequestDto
{
    public List<BusinessHoursDayDto> BusinessHours { get; init; } = new();
}

public record BusinessHoursDayDto
{
    public int DayOfWeek { get; init; }
    public bool IsOpen { get; init; }
    public string? OpenTime { get; init; }
    public string? CloseTime { get; init; }
    public List<BreakPeriodDto>? Breaks { get; init; }
}

public record BreakPeriodDto
{
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? Label { get; init; }
}

public record AddHolidayRequestDto
{
    public DateOnly Date { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public string? Pattern { get; init; }
}

public record AddExceptionRequestDto
{
    public DateOnly Date { get; init; }
    public TimeOnly? OpenTime { get; init; }
    public TimeOnly? CloseTime { get; init; }
    public string Reason { get; init; } = string.Empty;
}

// Result DTOs
public record UpdateBusinessHoursResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record AddHolidayResultDto
{
    public Guid HolidayId { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record AddExceptionResultDto
{
    public Guid ExceptionId { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

// View Models (matching query response types)
public record HolidaysViewModel
{
    public List<HolidayViewModel> Holidays { get; init; } = new();
}

public record HolidayViewModel
{
    public Guid Id { get; init; }
    public string Date { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public string? Pattern { get; init; }
}

public record ExceptionsViewModel
{
    public List<ExceptionViewModel> Exceptions { get; init; } = new();
}

public record ExceptionViewModel
{
    public Guid Id { get; init; }
    public string Date { get; init; } = string.Empty;
    public TimeOnly? OpenTime { get; init; }
    public TimeOnly? CloseTime { get; init; }
    public bool IsClosed { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record AvailabilityViewModel
{
    public string Date { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public string? Reason { get; init; }
    public List<TimeSlotViewModel>? Slots { get; init; }
}

public record TimeSlotViewModel
{
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
}

#endregion
