using System.Net;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// A staff member is told when a booking is put on them.
/// </summary>
/// <remarks>
/// <para>This is the one notification in the catalogue addressed to a StaffMember rather than to a customer
/// or a salon, and it is the case most easily got wrong: the obvious recipient is the salon that made the
/// assignment, but the salon already knows — it just did it. The person whose day changed is the staff
/// member.</para>
///
/// <para>It is also the only notification whose recipient is not derivable from the booking alone, since
/// the staff id on a booking is a membership, not a person.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class StaffAssignmentNotificationTests : ServiceCatalogIntegrationTestBase
{
    public StaffAssignmentNotificationTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Assigning_a_booking_tells_the_staff_member()
    {
        var b = await ArrangeAsync();

        AuthenticateAsProviderOwner(b.Provider);
        var response = await Client.PutAsync(
            $"/api/v1/bookings/{b.BookingId}/assign-staff/{b.StaffId}", null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "assign failed: " + body);

        var raised = await RaisedAsync(b.BookingId);
        raised.Should().Contain(r => r.Code == NotificationEventCode.StaffAssignedToBooking);
    }

    [Fact]
    public async Task It_is_addressed_to_the_assigned_person_not_to_the_membership_id()
    {
        // The rule is "tell the person whose day changed", and the recipient must be a PERSON: the staff id
        // on a booking is a membership, and addressing that would reach nobody.
        //
        // In this fixture the bookable member is the salon owner — a solo salon where the owner does the
        // work — so the owner legitimately receives it. That is not the salon being told about its own
        // action; it is the practitioner being told about theirs, and they happen to be the same human.
        var b = await ArrangeAsync();

        AuthenticateAsProviderOwner(b.Provider);
        await Client.PutAsync($"/api/v1/bookings/{b.BookingId}/assign-staff/{b.StaffId}", null);

        var assignment = (await RaisedAsync(b.BookingId))
            .Single(r => r.Code == NotificationEventCode.StaffAssignedToBooking);

        assignment.RecipientId.Should().NotBe(b.StaffId, "a membership id addresses nobody");
        assignment.RecipientId.Should().Be(
            await PersonBehindMembershipAsync(b.StaffId),
            "the notification goes to the person that membership belongs to");
    }

    [Fact]
    public async Task Assigning_twice_tells_them_once()
    {
        var b = await ArrangeAsync();

        AuthenticateAsProviderOwner(b.Provider);
        await Client.PutAsync($"/api/v1/bookings/{b.BookingId}/assign-staff/{b.StaffId}", null);
        await Client.PutAsync($"/api/v1/bookings/{b.BookingId}/assign-staff/{b.StaffId}", null);

        var raised = await RaisedAsync(b.BookingId);
        raised.Count(r => r.Code == NotificationEventCode.StaffAssignedToBooking).Should().Be(1);
    }

    // ── arrange ──

    private sealed record Arranged(
        Guid BookingId,
        Guid StaffId,
        Guid OwnerId,
        Domain.Aggregates.Provider Provider);

    private async Task<Arranged> ArrangeAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = (await DbContext.Services
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync()).First();

        var staffId = await GetBookableMemberIdAsync(provider);

        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(Guid.NewGuid()),
            provider.Id,
            service.Id,
            provider.Id.Value,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            service.BookingPolicy ?? BookingPolicy.Default,
            "staff assignment");

        await CreateEntityAsync(booking);

        return new Arranged(booking.Id.Value, staffId, provider.OwnerId.Value, provider);
    }

    private async Task<Guid> PersonBehindMembershipAsync(Guid membershipId)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<Domain.Repositories.IOrganizationMembershipRepository>();
        var membership = await repo.GetByIdAsync(membershipId);
        return membership!.PersonId;
    }

    private sealed record Raised(NotificationEventCode Code, Guid RecipientId);

    private async Task<List<Raised>> RaisedAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == bookingId)
            .Select(e => new Raised(e.EventCode, e.RecipientId))
            .ToListAsync();
    }
}
