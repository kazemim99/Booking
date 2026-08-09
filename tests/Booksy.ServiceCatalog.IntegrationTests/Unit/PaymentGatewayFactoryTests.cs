using Booksy.Core.Domain.Enums;
using Booksy.Infrastructure.External.Payment;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Unit;

/// <summary>
/// C2 money-safety: the factory must fail closed for non-functional stub gateways (Behpardakht returns mock
/// success without charging → would record phantom-paid bookings; Parsian/Saman are unimplemented). They are
/// refused unless <c>Payments:AllowStubGateways=true</c>.
///
/// Plain unit test (no database / no fixture) hosted in the integration project because it is the test project
/// that references <c>Booksy.Infrastructure.External</c>.
/// </summary>
public class PaymentGatewayFactoryTests
{
    private static PaymentGatewayFactory BuildFactory(bool allowStubs)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Payment:Behpardakht:TerminalId"] = "12345",   // makes IsProviderSupported(Behpardakht) true
            ["Payment:Parsian:Enabled"] = "true",           // section must exist for placeholder to be "supported"
            ["Payment:Saman:Enabled"] = "true",
            ["Payments:AllowStubGateways"] = allowStubs ? "true" : "false",
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var emptyProvider = new ServiceCollection().BuildServiceProvider();
        return new PaymentGatewayFactory(emptyProvider, config, NullLogger<PaymentGatewayFactory>.Instance);
    }

    [Fact]
    public void Behpardakht_is_refused_when_stub_gateways_disabled()
    {
        var factory = BuildFactory(allowStubs: false);

        var act = () => factory.CreatePaymentGateway(PaymentProvider.Behpardakht);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*stub gateway*", "a mock gateway must never be handed back in production (fail-closed)");
    }

    [Theory]
    [InlineData(PaymentProvider.Parsian)]
    [InlineData(PaymentProvider.Saman)]
    public void Unimplemented_placeholder_gateways_are_refused_when_stub_gateways_disabled(PaymentProvider provider)
    {
        var factory = BuildFactory(allowStubs: false);

        var act = () => factory.CreatePaymentGateway(provider);

        act.Should().Throw<InvalidOperationException>().WithMessage("*stub gateway*");
    }

    [Fact]
    public void Enabling_stub_gateways_bypasses_the_fail_closed_guard()
    {
        // With the opt-in flag on, the stub guard no longer throws; the call proceeds to DI resolution (which
        // fails here only because the gateway is not registered in this empty provider) — proving the guard is bypassed.
        var factory = BuildFactory(allowStubs: true);

        var act = () => factory.CreatePaymentGateway(PaymentProvider.Behpardakht);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not registered*", "past the stub guard, it reaches DI resolution rather than the fail-closed refusal");
    }
}
