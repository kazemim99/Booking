using Booksy.Tests.Common.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Notifications;

/// <summary>
/// C1 SignalR NotificationHub end-to-end auth validation (mandatory pre-close). Proves the hub requires an
/// authenticated connection (the <c>[Authorize]</c> added in C1 — previously it accepted anonymous connections),
/// that an authenticated client connects and can re-establish (reconnect), using a real SignalR HubConnection over
/// the in-memory test server. (The real-JWT <c>access_token</c> query-string negotiation is covered at the unit
/// level by <see cref="SignalRAccessTokenExtractorTests"/>, since the integration harness replaces JWT auth with a
/// test scheme driven by TestUserContext.)
/// </summary>
public class NotificationHubAuthTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public NotificationHubAuthTests(Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private HubConnection BuildConnection() =>
        new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                // The in-memory test server has no WebSocket; use long-polling over its handler.
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();

    [Fact]
    public async Task Anonymous_connection_is_rejected()
    {
        ClearAuthentication(); // no authenticated test user → negotiate must 401

        await using var conn = BuildConnection();
        var act = async () => await conn.StartAsync();

        await act.Should().ThrowAsync<Exception>("the hub is [Authorize]d — an anonymous connection must be refused");
        conn.State.Should().Be(HubConnectionState.Disconnected);
    }

    [Fact]
    public async Task Authenticated_client_connects_and_can_reconnect()
    {
        AuthenticateAsCustomer(Guid.NewGuid(), "hub-user@test.com");

        await using var conn = BuildConnection();

        await conn.StartAsync();
        conn.State.Should().Be(HubConnectionState.Connected, "an authenticated client is admitted");

        // Reconnect: stop then re-establish the connection (the client survives a drop).
        await conn.StopAsync();
        conn.State.Should().Be(HubConnectionState.Disconnected);

        await conn.StartAsync();
        conn.State.Should().Be(HubConnectionState.Connected, "the client can re-establish after a disconnect");
    }
}
