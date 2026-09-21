using System.Net;
using System.Net.Http.Json;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// Telling someone they have joined a salon, or stopped belonging to one.
/// </summary>
/// <remarks>
/// <para><b>Addressed to the STAFF MEMBER, not the salon</b> — the rule this change has applied everywhere
/// else: tell the party who did not act. The owner adds and removes people; they already know. The person
/// whose situation changed is the member, and for a removal that is the only notice they get that their
/// access to the salon has ended.</para>
///
/// <para>The catalogue listed both as <c>Provider</c> audience with third-person wording ("X was added to
/// Y"), which only makes sense read by the salon. Both were re-pointed at the member and the wording made
/// second-person to match; a message about you, written about you in the third person, reads as a leak from
/// somebody else's inbox.</para>
///
/// <para><b>A member with no account is not notified</b>, and cannot be: the outbox is keyed by user id, and
/// a salon may add a person who has no app account at all (<c>CreateUnclaimed</c>). That is a skip, not a
/// failure — the same constraint that keeps <c>InvitationSent</c> out of the outbox.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class StaffMembershipNotificationTests : ServiceCatalogIntegrationTestBase
{
    public StaffMembershipNotificationTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Adding_someone_who_has_an_account_tells_them()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var (personId, phone) = await CreateRealPersonAsync();

        AuthenticateAsProviderOwner(provider);
        var response = await AddStaffAsync(provider.Id.Value, phone);

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue("add staff failed: " + body);

        (await RaisedForAsync(personId)).Should().Contain(NotificationEventCode.StaffAdded);
    }

    [Fact]
    public async Task Adding_someone_with_no_account_notifies_nobody()
    {
        // A salon can add a person who has no app account; there is then no user id to address, and the
        // outbox cannot reach a name. Skipping is the only honest option.
        var provider = await CreateTestProviderWithServicesAsync();

        AuthenticateAsProviderOwner(provider);
        var response = await AddStaffAsync(provider.Id.Value, phone: null);

        response.IsSuccessStatusCode.Should().BeTrue();

        (await AllStaffNotificationsAsync()).Should().NotContain(
            r => r.Code == NotificationEventCode.StaffAdded && r.RecipientId == Guid.Empty,
            "an empty recipient would be a notification addressed to nobody, written down as if it were sent");
    }

    [Fact]
    public async Task The_notice_goes_to_the_person_not_the_membership()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var (personId, phone) = await CreateRealPersonAsync();

        AuthenticateAsProviderOwner(provider);
        (await AddStaffAsync(provider.Id.Value, phone)).IsSuccessStatusCode.Should().BeTrue();
        var membershipId = await MembershipIdAsync(personId, provider.Id.Value);

        // A membership id addresses nobody: the inbox, preferences and device registry are keyed by user.
        // This mistake has already been made four times in this change.
        (await RaisedForAsync(membershipId)).Should().BeEmpty();
        (await RaisedForAsync(personId)).Should().Contain(NotificationEventCode.StaffAdded);
    }

    [Fact]
    public async Task Removing_someone_tells_them_their_time_at_the_salon_ended()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var (personId, phone) = await CreateRealPersonAsync();

        AuthenticateAsProviderOwner(provider);
        (await AddStaffAsync(provider.Id.Value, phone)).IsSuccessStatusCode.Should().BeTrue();
        var membershipId = await MembershipIdAsync(personId, provider.Id.Value);

        var response = await Client.DeleteAsync(
            $"/api/v1/Providers/{provider.Id.Value}/staff/{membershipId}");

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue("remove staff failed: " + body);

        (await RaisedForAsync(personId)).Should().Contain(NotificationEventCode.StaffRemoved);
    }

    // ── arrange ──

    private async Task<HttpResponseMessage> AddStaffAsync(Guid providerId, string? phone) =>
        await Client.PostAsJsonAsync($"/api/v1/Providers/{providerId}/staff", new
        {
            firstName = "زهرا",
            lastName = "محمدی",
            phoneNumber = phone,
            role = "ServiceProvider",
        });

    /// <summary>
    /// A real UserManagement user with a known phone, because the handler links a new member to a person
    /// by looking the phone up in the directory — no user, no person id, nothing to address.
    /// </summary>
    private async Task<(Guid PersonId, string Phone)> CreateRealPersonAsync()
    {
        // Digits only — a GUID's hex would not parse as a phone number. Derived from a GUID rather than
        // Random, which this project bans in tests for determinism.
        var phone = $"+98912{Math.Abs(Guid.NewGuid().GetHashCode() % 10_000_000):D7}";

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<global::Booksy.UserManagement.Infrastructure.Persistence.Context.UserManagementDbContext>();

        var user = global::Booksy.UserManagement.Domain.Aggregates.User.RegisterWithPhone(
            Core.Domain.ValueObjects.Email.Create($"staff-{Guid.NewGuid():N}@test.com"),
            Core.Domain.ValueObjects.PhoneNumber.From(phone),
            global::Booksy.UserManagement.Domain.Entities.UserProfile.Create("زهرا", "محمدی"),
            global::Booksy.UserManagement.Domain.Enums.UserType.Provider);

        db.Add(user);
        await db.SaveChangesAsync();

        return (user.Id.Value, phone);
    }

    /// <summary>
    /// Read from the database rather than the response body: the endpoint's field names are not what this
    /// test is about, and a rename there should not break an assertion about addressing.
    /// </summary>
    private async Task<Guid> MembershipIdAsync(Guid personId, Guid providerId)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<Domain.Repositories.IOrganizationMembershipRepository>();

        var membership = await repo.GetActiveByPersonAndOrganizationAsync(
            Core.Domain.ValueObjects.UserId.From(personId),
            Domain.ValueObjects.ProviderId.From(providerId));

        membership.Should().NotBeNull("the staff member should have been added");
        return membership!.Id;
    }

    private sealed record Raised(NotificationEventCode Code, Guid RecipientId);

    private async Task<List<NotificationEventCode>> RaisedForAsync(Guid recipientId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.RecipientId == recipientId)
            .Select(e => e.EventCode)
            .ToListAsync();
    }

    private async Task<List<Raised>> AllStaffNotificationsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.EventCode == NotificationEventCode.StaffAdded
                        || e.EventCode == NotificationEventCode.StaffRemoved)
            .Select(e => new Raised(e.EventCode, e.RecipientId))
            .ToListAsync();
    }
}
