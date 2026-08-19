using Booksy.Core.Application.Services.Notifications;
using Booksy.Infrastructure.External.Notifications.Sms;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.Host.CompositionTests;

/// <summary>
/// Pins the SMS port to a single implementation in the composed host.
///
/// <para>The codebase used to declare <c>ISmsNotificationService</c> three times — in
/// <c>ServiceCatalog.Application.Services</c>, in <c>ServiceCatalog.Application.Services.Notifications</c>, and in
/// <c>UserManagement.Application.Services.Interfaces</c> — each with its own implementation and its own DI
/// registration. In the monolith that meant two live SMS gateways in one process (Rahyab for notifications,
/// Kavenegar for staff invitations) and a caller's gateway being decided by which <c>using</c> it happened to
/// import. Neither per-context test suite could see it, because each boots a single context's Startup.</para>
///
/// <para>Both contexts now register through the same idempotent extension, so the invariant to protect is
/// "exactly one registration, resolving one implementation" — a second registration would silently win at
/// resolution time and re-split the gateway.</para>
/// </summary>
public sealed class SmsNotificationServiceCompositionTests : IClassFixture<HostCompositionFactory>
{
    private readonly HostCompositionFactory _factory;

    public SmsNotificationServiceCompositionTests(HostCompositionFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Exactly_One_Sms_Notification_Service_Is_Registered()
    {
        // GetServices returns one instance per registration, so a duplicate shows up here even though a plain
        // resolve would silently hand back only the last one.
        var resolved = _factory.Services.GetServices<ISmsNotificationService>().ToList();

        resolved.Should().ContainSingle(
            "two registrations would mean two SMS gateways live in one process, with the winner decided by "
            + "registration order rather than configuration");
    }

    [Fact]
    public void The_Configured_Gateway_Is_The_One_Resolved()
    {
        var resolved = _factory.Services.GetRequiredService<ISmsNotificationService>();

        // appsettings.json selects Rahyab via Notifications:SMS:Provider.
        resolved.Should().BeOfType<RahyabSmsNotificationService>();
    }
}
