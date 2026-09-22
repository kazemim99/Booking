using System.Net;
using System.Net.Http.Json;
using Booksy.Core.Application.Services.Notifications;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Booksy.Host.IntegrationTests.Infrastructure.Fakes;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// What the customer hears about a booking the salon entered for them
/// (openspec/changes/_inline/walk-in-customer-name-sms). The salon's own owner must never be the
/// one notified about their own walk-in — which is what happened while the aggregate's "customer"
/// was the owner.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class BookingCustomerSmsTests : ServiceCatalogIntegrationTestBase
{
    public BookingCustomerSmsTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private FakeSmsNotificationService Sms =>
        (FakeSmsNotificationService)Factory.Services.GetRequiredService<ISmsNotificationService>();

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    private async Task<HttpResponseMessage> Book(
        Guid providerId, Guid serviceId, DateTime at, string phone, bool? notifyCustomer = null) =>
        await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId,
            serviceId,
            staffProviderId = providerId,
            startTime = at,
            walkInFirstName = "مرتضی",
            walkInLastName = "کاظمی",
            walkInPhone = phone,
            notifyCustomer,
        });

    [Fact]
    public async Task The_customer_is_told_when_where_and_at_what_time()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);
        var at = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(2), 10);

        var booking = await Book(provider.Id.Value, service.Id.Value, at, "09123135143");
        booking.StatusCode.Should().Be(HttpStatusCode.Created, await booking.Content.ReadAsStringAsync());

        var message = Sms.LastMessageTo("+989123135143");
        message.Should().NotBeNull("the salon's customer is told about the booking by SMS");
        message.Should().Contain("مرتضی").And.Contain(provider.Profile.BusinessName);
        message.Should().Contain("ساعت ۱۰:۰۰", "the salon's wall-clock time, never shifted");
    }

    [Fact]
    public async Task The_salon_can_book_someone_without_texting_them()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);
        var at = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(3), 11);

        var booking = await Book(provider.Id.Value, service.Id.Value, at, "09351112233", notifyCustomer: false);
        booking.StatusCode.Should().Be(HttpStatusCode.Created, await booking.Content.ReadAsStringAsync());

        Sms.LastMessageTo("+989351112233").Should().BeNull(
            "the customer was standing at the counter; the salon said not to text them");
    }
}
