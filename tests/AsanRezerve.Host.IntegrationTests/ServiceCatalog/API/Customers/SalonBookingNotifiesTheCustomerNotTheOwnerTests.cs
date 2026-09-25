using System.Net.Http.Json;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// A salon booking someone must not send the salon's owner a "your booking is confirmed" notice:
/// the aggregate's customer IS the owner for those bookings, so the customer notice was addressed
/// to the salon about its own work (openspec/changes/_inline/walk-in-customer-name-sms).
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class SalonBookingNotifiesTheCustomerNotTheOwnerTests : ServiceCatalogIntegrationTestBase
{
    public SalonBookingNotifiesTheCustomerNotTheOwnerTests(AsanRezerveHostFactory factory) : base(factory) { }

    [Fact]
    public async Task The_owner_is_told_about_the_new_booking_but_never_as_its_customer()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);
        var day = DateTime.UtcNow.Date.AddDays(2);
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday) day = day.AddDays(1);

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = day.AddHours(10),
            walkInFirstName = "تست",
            walkInPhone = "09350009911",
            notifyCustomer = false,
        });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        DbContext.ChangeTracker.Clear();
        var owner = UserId.From(provider.OwnerId.Value);
        var forOwner = await DbContext.Notifications.AsNoTracking()
            .Where(n => n.RecipientId == owner)
            .Select(n => n.Type)
            .ToListAsync();

        forOwner.Should().NotContain(NotificationType.BookingConfirmation,
            "the salon entered this booking; telling them their booking is confirmed is telling them about themselves");
    }
}
