using System.Net;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.BackgroundJobs;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.IntegrationTests.API.Reviews;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// A salon's notification about one of its bookings opens that booking (QA walkthrough 2026-09-22: tapping a
/// "new booking request" did nothing). The resolver used to count the reader as a party only when the booking's
/// ProviderId equalled the reader's user id — but a booking belongs to the ORGANISATION, and its owner reads with a
/// user id, so no salon notification was ever tappable.
/// </summary>
/// <remarks>Built on <see cref="ReviewTestBase"/> only for its real-endpoint booking helper.</remarks>
[Collection(BooksyHostTestCollection.Name)]
public class SalonInboxActionabilityTests : ReviewTestBase
{
    public SalonInboxActionabilityTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private async Task RaiseAndSweepAsync(Guid recipientId, Guid bookingId)
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();
            await raiser.RaiseAsync(
                NotificationEventCode.NewBookingRequest,
                recipientId,
                Guid.NewGuid(),
                new Dictionary<string, string> { ["businessName"] = "سالن نهال" },
                "Booking",
                bookingId);
            await context.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>().ExecuteAsync();
    }

    private async Task<JToken> FirstInboxItemAsync()
    {
        var response = await Client.GetAsync("/api/v1/Notifications/inbox");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var parsed = JObject.Parse(body);
        return ((parsed["data"] as JObject ?? parsed)["items"]!)[0]!;
    }

    [Fact]
    public async Task The_salon_owner_can_open_a_notification_about_their_own_booking()
    {
        var visit = await UncompletedVisitAsync();
        await RaiseAndSweepAsync(visit.Provider.OwnerId.Value, visit.BookingId);

        AuthenticateAsProviderOwner(visit.Provider);
        var item = await FirstInboxItemAsync();

        item["isActionable"]!.Value<bool>().Should().BeTrue();
        item["destinationId"]!.Value<string>().Should().Be(visit.BookingId.ToString());
    }

    [Fact]
    public async Task Another_salons_owner_cannot_open_it()
    {
        var visit = await UncompletedVisitAsync();
        var other = await CreateAndAuthenticateAsProviderAsync("Other Salon", $"{Guid.NewGuid():N}@test.com");
        // Addressed to the wrong person on purpose: being told about a booking must not make it openable.
        await RaiseAndSweepAsync(other.OwnerId.Value, visit.BookingId);

        AuthenticateAsProviderOwner(other);
        var item = await FirstInboxItemAsync();

        item["isActionable"]!.Value<bool>().Should().BeFalse();
    }
}
