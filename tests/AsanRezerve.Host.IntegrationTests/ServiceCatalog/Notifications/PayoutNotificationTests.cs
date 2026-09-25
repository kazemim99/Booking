using System.Net;
using System.Net.Http.Json;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The salon is told when it is paid.
/// </summary>
/// <remarks>
/// <para>Addressed to the provider's OWNER, not to the provider id — the inbox, preferences and device
/// registry are all keyed by user, and this is the third place in this change where getting that wrong
/// would have produced a notification nobody could ever see.</para>
///
/// <para>Non-suppressible in the catalogue: money arriving is a record the salon is entitled to.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class PayoutNotificationTests : ServiceCatalogIntegrationTestBase
{
    public PayoutNotificationTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Executing_a_payout_tells_the_salon()
    {
        var (payoutId, ownerId, _) = await ArrangePendingPayoutAsync();

        AuthenticateAsAdmin();
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/Payouts/{payoutId}/execute",
            new { connectedAccountId = "acct_test" });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "execute failed: " + body);

        (await RaisedForAsync(ownerId)).Should().Contain(NotificationEventCode.PayoutCompleted);
    }

    [Fact]
    public async Task The_payout_notice_goes_to_the_owner_not_the_provider_id()
    {
        var (payoutId, ownerId, providerId) = await ArrangePendingPayoutAsync();

        AuthenticateAsAdmin();
        await Client.PostAsJsonAsync(
            $"/api/v1/Payouts/{payoutId}/execute",
            new { connectedAccountId = "acct_test" });

        (await RaisedForAsync(ownerId)).Should().NotBeEmpty();
        (await RaisedForAsync(providerId)).Should().BeEmpty(
            "a provider id addresses nobody — the inbox is keyed by user");
    }

    [Fact]
    public async Task An_executed_payout_produces_exactly_one_notification()
    {
        var (payoutId, ownerId, _) = await ArrangePendingPayoutAsync();

        AuthenticateAsAdmin();
        await Client.PostAsJsonAsync(
            $"/api/v1/Payouts/{payoutId}/execute",
            new { connectedAccountId = "acct_test" });

        (await RaisedForAsync(ownerId))
            .Count(c => c == NotificationEventCode.PayoutCompleted)
            .Should().Be(1);

        (await NonOutboxNotificationCountAsync(ownerId)).Should().Be(
            0, "the legacy payout handler must go in the same step that replaces it");
    }

    // ── arrange ──

    private async Task<(Guid PayoutId, Guid OwnerId, Guid ProviderId)> ArrangePendingPayoutAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();

        var payout = Payout.Create(
            provider.Id,
            Core.Domain.ValueObjects.Money.Create(1_000_000, "IRT"),
            Core.Domain.ValueObjects.Money.Create(100_000, "IRT"),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            new List<PaymentId>());

        await CreateEntityAsync(payout);

        return (payout.Id.Value, provider.OwnerId.Value, provider.Id.Value);
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

    private async Task<int> NonOutboxNotificationCountAsync(Guid recipientId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var typed = Core.Domain.ValueObjects.UserId.From(recipientId);
        return await db.Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientId == typed && n.EventCode == null);
    }
}
