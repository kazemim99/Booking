using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Infrastructure.External.Payment.ZarinPal;
using AsanRezerve.Infrastructure.External.Payment.ZarinPal.Models;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Application.Commands.Payment.VerifyZarinPalPayment;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// C4 Phase 0 / gap B2 — the substance, not just the wiring: against a real database (gateway mocked), a reported
/// cancellation must actually <b>settle</b> the payment as Failed instead of being treated as "OK" and left Pending,
/// and — critically for money safety — an already-<c>Paid</c> payment must never be flipped to Failed by a later or
/// malicious "NOK".
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class VerifyZarinPalStatusHandlingTests : ServiceCatalogIntegrationTestBase
{
    public VerifyZarinPalStatusHandlingTests(AsanRezerveHostFactory factory)
        : base(factory) { }

    private static Payment NewPayment(out string authority, decimal amount = 100m)
    {
        authority = $"auth-{Guid.NewGuid():N}";
        var p = Payment.CreateForBooking(
            BookingId.From(Guid.NewGuid()), UserId.From(Guid.NewGuid()), ProviderId.From(Guid.NewGuid()),
            Money.Create(amount, "USD"), PaymentMethod.ZarinPal);
        p.RecordPaymentRequest(authority, "https://pay.test/authority");
        return p;
    }

    private async Task<string> SeedPendingAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
        var p = NewPayment(out var authority);
        await repo.AddAsync(p);
        await uow.CommitAsync();
        return authority;
    }

    private async Task<string> SeedPaidAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
        var p = NewPayment(out var authority);
        p.VerifyPayment("REF-ALREADY-PAID", "6274********1234", 0);
        await repo.AddAsync(p);
        await uow.CommitAsync();
        return authority;
    }

    /// Builds the real handler over the real DB with a substituted gateway, so we can assert whether the gateway was called.
    private (VerifyZarinPalPaymentCommandHandler Handler, IZarinPalService Gateway, IServiceScope Scope) BuildHandler()
    {
        var scope = Factory.Services.CreateScope();
        var gateway = Substitute.For<IZarinPalService>();
        var handler = new VerifyZarinPalPaymentCommandHandler(
            scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>(),
            gateway,
            scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>(),
            NullLogger<VerifyZarinPalPaymentCommandHandler>.Instance);
        return (handler, gateway, scope);
    }

    private async Task<PaymentStatus?> StatusOfAsync(string authority)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
        return (await repo.GetByAuthorityAsync(authority))?.Status;
    }

    [Fact]
    public async Task A_reported_cancellation_settles_the_payment_as_failed_without_calling_the_gateway()
    {
        var authority = await SeedPendingAsync();
        var (handler, gateway, scope) = BuildHandler();
        using (scope)
        {
            var result = await handler.Handle(
                new VerifyZarinPalPaymentCommand(authority, "NOK"), CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.PaymentStatus.Should().Be("Failed");
            // A cancellation needs no verification round-trip.
            await gateway.DidNotReceive().VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
        }

        (await StatusOfAsync(authority)).Should().Be(PaymentStatus.Failed,
            "the cancellation is settled now instead of leaving the payment Pending until reconciliation");
    }

    [Fact]
    public async Task An_already_paid_payment_is_never_flipped_to_failed_by_a_later_cancellation()
    {
        var authority = await SeedPaidAsync();
        var (handler, gateway, scope) = BuildHandler();
        using (scope)
        {
            var result = await handler.Handle(
                new VerifyZarinPalPaymentCommand(authority, "NOK"), CancellationToken.None);

            result.IsSuccessful.Should().BeTrue("an already-verified payment returns idempotent success");
            result.PaymentStatus.Should().Be("Paid");
            await gateway.DidNotReceive().VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
        }

        (await StatusOfAsync(authority)).Should().Be(PaymentStatus.Paid,
            "captured money must never be un-recorded by a stray or malicious NOK");
    }

    [Fact]
    public async Task An_OK_status_still_verifies_against_the_gateway_before_recording_payment()
    {
        // The client's "OK" is not proof of payment — ZarinPal remains the authority.
        var authority = await SeedPendingAsync();
        var (handler, gateway, scope) = BuildHandler();
        using (scope)
        {
            gateway.VerifyPaymentAsync(authority, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
                   .Returns(new ZarinPalVerifyResult { IsSuccessful = true, RefId = 987654, CardPan = "6274********1234", Fee = 0 });

            var result = await handler.Handle(
                new VerifyZarinPalPaymentCommand(authority, "OK"), CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            // An OK must be confirmed with the gateway, never trusted blindly.
            await gateway.Received(1).VerifyPaymentAsync(authority, Arg.Any<decimal>(), Arg.Any<CancellationToken>());
        }

        (await StatusOfAsync(authority)).Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task An_OK_that_the_gateway_rejects_is_recorded_as_failed()
    {
        var authority = await SeedPendingAsync();
        var (handler, gateway, scope) = BuildHandler();
        using (scope)
        {
            gateway.VerifyPaymentAsync(authority, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
                   .Returns(new ZarinPalVerifyResult { IsSuccessful = false, ErrorCode = -51, ErrorMessage = "Not paid" });

            var result = await handler.Handle(
                new VerifyZarinPalPaymentCommand(authority, "OK"), CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
        }

        (await StatusOfAsync(authority)).Should().Be(PaymentStatus.Failed,
            "a claimed-but-unverified payment must not be recorded as paid");
    }
}
