using System.Net;
using System.Net.Http.Json;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The salon learns when someone it invited actually joins.
/// </summary>
/// <remarks>
/// The invitation itself already notifies the invitee (<c>InvitationSent</c>, the one legacy notification
/// path that was always reachable and correct). What was missing is the other direction: the owner sends an
/// invitation and then has no way to know it was taken up except by looking.
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class InvitationAcceptedNotificationTests : ServiceCatalogIntegrationTestBase
{
    private const string SandboxOtpCode = "123456";

    public InvitationAcceptedNotificationTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Accepting_an_invitation_tells_the_salon_owner()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var phone = NewPhone();
        var invitationId = await SeedPendingInvitationAsync(provider.Id, phone);

        await Client.PostAsync($"/api/v1/memberships/invitations/{invitationId}/send-otp", null);

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/memberships/invitations/{invitationId}/register-and-accept",
            new { firstName = "زهرا", lastName = "محمدی", otpCode = SandboxOtpCode });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "accept failed: " + body);

        var raised = await RaisedForAsync(provider.OwnerId.Value);
        raised.Should().Contain(NotificationEventCode.InvitationAccepted);
    }

    [Fact]
    public async Task The_acceptance_notice_goes_to_the_owner_not_the_organisation_id()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var phone = NewPhone();
        var invitationId = await SeedPendingInvitationAsync(provider.Id, phone);

        await Client.PostAsync($"/api/v1/memberships/invitations/{invitationId}/send-otp", null);
        await Client.PostAsJsonAsync(
            $"/api/v1/memberships/invitations/{invitationId}/register-and-accept",
            new { firstName = "زهرا", lastName = "محمدی", otpCode = SandboxOtpCode });

        (await RaisedForAsync(provider.Id.Value)).Should().BeEmpty(
            "an organisation id addresses nobody — the inbox is keyed by user");
    }

    // ── arrange ──

    private static string NewPhone() => $"+98912{Random.Shared.Next(1000000, 9999999)}";

    private async Task<Guid> SeedPendingInvitationAsync(ProviderId organizationId, string phone)
    {
        var invitation = ProviderInvitation.Create(
            organizationId, PhoneNumber.From(phone), inviteeName: "دعوت‌شده");

        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProviderInvitationWriteRepository>();
        var uow = scope.ServiceProvider
            .GetRequiredService<Application.Abstractions.Persistence.IServiceCatalogUnitOfWork>();

        await repo.SaveAsync(invitation);
        await uow.CommitAsync();

        return invitation.Id;
    }

    private async Task<List<NotificationEventCode>> RaisedForAsync(Guid recipientId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await db.NotificationOutbox.AsNoTracking()
            .Where(e => e.RecipientId == recipientId)
            .Select(e => e.EventCode)
            .ToListAsync();
    }
}
