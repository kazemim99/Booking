using System.Net;
using System.Net.Http.Json;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.Tests.Common.Builders;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The salon is told when its account changes under it.
/// </summary>
/// <remarks>
/// Non-suppressible in the catalogue: activation decides whether the salon can trade at all, so it is not
/// something a preference toggle should be able to hide.
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class ProviderAccountNotificationTests : ServiceCatalogIntegrationTestBase
{
    public ProviderAccountNotificationTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Activating_a_provider_tells_its_owner()
    {
        var provider = await CreateInactiveProviderAsync();

        AuthenticateAsAdmin();
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/Providers/{provider.Id.Value}/activate", new { });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "activate failed: " + body);

        (await RaisedForAsync(provider.OwnerId.Value))
            .Should().Contain(NotificationEventCode.ProviderActivated);
    }

    [Fact]
    public async Task The_activation_notice_goes_to_the_owner_not_the_provider_id()
    {
        var provider = await CreateInactiveProviderAsync();

        AuthenticateAsAdmin();
        await Client.PostAsJsonAsync($"/api/v1/Providers/{provider.Id.Value}/activate", new { });

        (await RaisedForAsync(provider.Id.Value)).Should().BeEmpty(
            "a provider id addresses nobody — the inbox is keyed by user");
    }

    // ── arrange ──

    /// <summary>
    /// A provider awaiting activation, built in that state rather than demoted into it.
    /// </summary>
    /// <remarks>
    /// An earlier version created an active provider and dropped its status with raw SQL. That does not
    /// work: provider reads are cached (hence ProviderCacheInvalidationEventHandler), so the write landed
    /// in the table and the handler went on seeing an active provider — "Provider is already active",
    /// from a row that said otherwise. Building it in the right state avoids the question entirely.
    /// </remarks>
    private async Task<Domain.Aggregates.Provider> CreateInactiveProviderAsync()
    {
        var provider = new ProviderBuilder()
            .WithOwner(Guid.NewGuid())
            .WithBusinessName($"سالن {Guid.NewGuid():N}")
            .WithStatus(ProviderStatus.PendingVerification)
            .Build();

        await CreateEntityAsync(provider);
        return provider;
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
