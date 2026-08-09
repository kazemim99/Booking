using Booksy.Infrastructure.External.Payment.ZarinPal;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Unit;

/// <summary>
/// The deterministic fake gateway used by tier-T2 end-to-end tests. These tests pin the properties T2 relies on:
/// stable authorities/RefIds (so a test can assert them), a ZarinPal-shaped authority and StartPay URL (so the
/// application and client code under test are the production ones), and the three scenarios T2 must drive —
/// success, refusal at create, and "not verified" at verify (the NOK/cancel path).
/// </summary>
public class FakeZarinPalServiceTests
{
    private static FakeZarinPalService Sut() => new(NullLogger<FakeZarinPalService>.Instance);

    [Fact]
    public async Task Create_issues_a_zarinpal_shaped_authority_and_startpay_url()
    {
        var result = await Sut().CreatePaymentRequestAsync(200000, "deposit for booking");

        result.IsSuccessful.Should().BeTrue();
        result.Authority.Should().StartWith("A").And.HaveLength(36, "real ZarinPal authorities are 36 characters");
        result.PaymentUrl.Should().Be($"https://sandbox.zarinpal.com/pg/StartPay/{result.Authority}");
    }

    [Fact]
    public async Task Create_is_deterministic_for_the_same_payment()
    {
        var first = await Sut().CreatePaymentRequestAsync(200000, "deposit for booking");
        var second = await Sut().CreatePaymentRequestAsync(200000, "deposit for booking");

        second.Authority.Should().Be(first.Authority, "T2 assertions depend on stable authorities");
    }

    [Fact]
    public async Task Create_differs_for_a_different_amount()
    {
        var a = await Sut().CreatePaymentRequestAsync(200000, "deposit");
        var b = await Sut().CreatePaymentRequestAsync(300000, "deposit");

        b.Authority.Should().NotBe(a.Authority);
    }

    [Fact]
    public async Task Create_refuses_when_the_refusal_scenario_is_requested()
    {
        var result = await Sut().CreatePaymentRequestAsync(200000, $"deposit {FakeZarinPalService.RefuseTrigger}");

        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().NotBe(0);
        result.Authority.Should().BeEmpty("nothing was created, so there is no authority to verify");
    }

    [Fact]
    public async Task Create_rejects_a_non_positive_amount_like_the_real_gateway()
    {
        (await Sut().CreatePaymentRequestAsync(0, "deposit")).IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_succeeds_with_a_deterministic_ref_id_for_a_normal_authority()
    {
        var sut = Sut();
        var created = await sut.CreatePaymentRequestAsync(200000, "deposit for booking");

        var first = await sut.VerifyPaymentAsync(created.Authority, 200000);
        var second = await sut.VerifyPaymentAsync(created.Authority, 200000);

        first.IsSuccessful.Should().BeTrue();
        first.RefId.Should().BeGreaterThan(0);
        first.CardPan.Should().NotBeNullOrEmpty();
        second.RefId.Should().Be(first.RefId, "a repeated verify must report the same reference");
    }

    [Fact]
    public async Task Verify_reports_not_paid_when_the_unverified_scenario_was_requested_at_create()
    {
        var sut = Sut();
        var created = await sut.CreatePaymentRequestAsync(200000, $"deposit {FakeZarinPalService.UnverifiedTrigger}");
        created.IsSuccessful.Should().BeTrue("the customer did reach the bank page");

        var verify = await sut.VerifyPaymentAsync(created.Authority, 200000);

        verify.IsSuccessful.Should().BeFalse("the customer abandoned or cancelled the payment");
        verify.ErrorCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Verify_rejects_an_empty_authority()
    {
        (await Sut().VerifyPaymentAsync(string.Empty, 200000)).IsSuccessful.Should().BeFalse();
    }
}
