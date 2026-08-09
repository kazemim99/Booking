using Booksy.Infrastructure.External.Payment.ZarinPal;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Unit;

/// <summary>
/// Safeguards on the test-only fake ZarinPal gateway. A fake payment gateway is the last thing that should ever be
/// reachable in Production, so these tests pin the guard's paranoid behaviour: off by default, permitted only in a
/// non-production environment, and a hard startup failure — never a silent fallback — if requested in Production or
/// in an environment that cannot be identified.
/// </summary>
public class FakeZarinPalGuardTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Is_off_when_not_configured()
    {
        FakeZarinPalGuard.ShouldUseFake(Config(), "Development").Should().BeFalse();
    }

    [Fact]
    public void Is_off_when_explicitly_false()
    {
        FakeZarinPalGuard.ShouldUseFake(Config((FakeZarinPalGuard.ConfigKey, "false")), "Development")
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Staging")]
    public void Is_permitted_in_non_production_environments_when_requested(string environment)
    {
        FakeZarinPalGuard.ShouldUseFake(Config((FakeZarinPalGuard.ConfigKey, "true")), environment)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("production")]
    [InlineData("PRODUCTION")]
    public void Throws_rather_than_falling_back_when_requested_in_production(string environment)
    {
        var act = () => FakeZarinPalGuard.ShouldUseFake(Config((FakeZarinPalGuard.ConfigKey, "true")), environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*never be active in Production*",
                "a production deployment must refuse to boot rather than serve payments through a fake gateway");
    }

    [Fact]
    public void Treats_an_undeterminable_environment_as_production_and_throws()
    {
        // No environment argument and nothing in configuration ⇒ assume the most dangerous case.
        var act = () => FakeZarinPalGuard.ShouldUseFake(Config((FakeZarinPalGuard.ConfigKey, "true")), null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Falls_back_to_the_environment_name_in_configuration_when_none_is_supplied()
    {
        var config = Config(
            (FakeZarinPalGuard.ConfigKey, "true"),
            ("ASPNETCORE_ENVIRONMENT", "Development"));

        FakeZarinPalGuard.ShouldUseFake(config, null).Should().BeTrue();
    }

    [Fact]
    public void A_production_environment_in_configuration_still_throws_when_no_name_is_supplied()
    {
        var config = Config(
            (FakeZarinPalGuard.ConfigKey, "true"),
            ("ASPNETCORE_ENVIRONMENT", "Production"));

        var act = () => FakeZarinPalGuard.ShouldUseFake(config, null);

        act.Should().Throw<InvalidOperationException>();
    }
}
