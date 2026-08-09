using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// C4 Phase 0 / gap B1 — deposit gating for the create-then-pay flow. A deposit-required booking is created holding
/// the slot in <c>Requested</c> and MUST NOT confirm until the deposit is verified; once the gateway verifies the
/// payment, the booking MUST become <c>Confirmed</c>. Events are dispatched through the <b>real</b>
/// <see cref="IDomainEventDispatcher"/> — exactly as production does — so this proves the whole wiring
/// (DI registration → handler → aggregate gate → persistence), not just the handler in isolation.
/// </summary>
public class DepositGatingTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public DepositGatingTests(Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private static BookingPolicy Policy(bool requireDeposit, decimal depositPercentage = 20m) =>
        BookingPolicy.Create(
            minAdvanceBookingHours: 1,
            maxAdvanceBookingDays: 30,
            cancellationWindowHours: 24,
            cancellationFeePercentage: 0m,
            allowRescheduling: true,
            rescheduleWindowHours: 24,
            requireDeposit: requireDeposit,
            depositPercentage: depositPercentage);

    /// Persists a booking in Requested state (create-then-pay: the slot is held before payment).
    private async Task<Booking> SeedBookingAsync(bool requireDeposit, decimal total = 100m)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

        var booking = Booking.CreateBookingRequest(
            customerId: UserId.From(Guid.NewGuid()),
            providerId: ProviderId.From(Guid.NewGuid()),
            serviceId: ServiceId.From(Guid.NewGuid()),
            staffId: Guid.NewGuid(),                    // unique staff → no slot-exclusion clash with other tests
            startTime: DateTime.UtcNow.AddDays(2),      // inside the booking window
            duration: Duration.FromMinutes(60),
            totalPrice: Price.Create(total, "USD"),
            policy: Policy(requireDeposit));

        await repo.SaveBookingAsync(booking);
        await uow.CommitAsync();
        return booking;
    }

    /// Dispatches a PaymentVerifiedEvent for the booking through the production dispatcher.
    private async Task DispatchPaymentVerifiedAsync(Booking booking, string refNumber)
    {
        using var scope = Factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var depositAmount = booking.PaymentInfo.DepositAmount.Amount;
        await dispatcher.DispatchEventAsync(new PaymentVerifiedEvent(
            PaymentId.New(),
            booking.Id,
            booking.CustomerId,
            booking.ProviderId,
            Money.Create(depositAmount > 0 ? depositAmount : booking.TotalPrice.Amount, "USD"),
            refNumber,
            CardPan: "6274********1234",
            VerifiedAt: DateTime.UtcNow));
    }

    private async Task<Booking> ReloadAsync(BookingId id)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
        var booking = await repo.GetByIdAsync(id);
        booking.Should().NotBeNull();
        return booking!;
    }

    [Fact]
    public async Task A_deposit_required_booking_is_not_confirmed_until_the_deposit_is_paid()
    {
        var booking = await SeedBookingAsync(requireDeposit: true);

        var persisted = await ReloadAsync(booking.Id);
        persisted.Status.Should().Be(BookingStatus.Requested, "the slot is held but payment has not happened yet");
        persisted.PaymentInfo.DepositAmount.Amount.Should().Be(20m, "20% of 100");
        persisted.PaymentInfo.PaidAmount.Amount.Should().Be(0m);

        // The domain gate must refuse confirmation while the deposit is unpaid.
        var act = () => persisted.Confirm();
        act.Should().Throw<Exception>("Booking.Confirm is gated by DepositMustBePaidBeforeConfirmationRule");
    }

    [Fact]
    public async Task A_verified_deposit_payment_records_the_deposit_and_confirms_the_booking()
    {
        var booking = await SeedBookingAsync(requireDeposit: true);

        await DispatchPaymentVerifiedAsync(booking, "REF-DEPOSIT-1");

        var confirmed = await ReloadAsync(booking.Id);
        confirmed.Status.Should().Be(BookingStatus.Confirmed, "a verified deposit satisfies the confirmation gate");
        confirmed.PaymentInfo.IsDepositPaid().Should().BeTrue();
        confirmed.PaymentInfo.PaidAmount.Amount.Should().Be(20m, "the deposit amount is recorded as paid");
        confirmed.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Replaying_the_payment_verified_event_is_idempotent()
    {
        var booking = await SeedBookingAsync(requireDeposit: true);

        await DispatchPaymentVerifiedAsync(booking, "REF-DEPOSIT-1");
        var afterFirst = await ReloadAsync(booking.Id);
        var confirmedAt = afterFirst.ConfirmedAt;

        // Redelivery of the same event (at-least-once transport / manual replay) must change nothing.
        await DispatchPaymentVerifiedAsync(booking, "REF-DEPOSIT-1");
        await DispatchPaymentVerifiedAsync(booking, "REF-DEPOSIT-1");

        var afterReplays = await ReloadAsync(booking.Id);
        afterReplays.Status.Should().Be(BookingStatus.Confirmed);
        afterReplays.PaymentInfo.PaidAmount.Amount.Should().Be(20m, "the deposit is never counted twice");
        afterReplays.ConfirmedAt.Should().Be(confirmedAt, "the original confirmation timestamp is preserved");
    }

    [Fact]
    public async Task A_booking_that_does_not_require_a_deposit_is_left_untouched()
    {
        // Regression guard: this handler must not change the flow for non-deposit bookings (they confirm via their
        // own path), so a verified payment must not silently auto-confirm them.
        var booking = await SeedBookingAsync(requireDeposit: false);

        await DispatchPaymentVerifiedAsync(booking, "REF-NO-DEPOSIT");

        var after = await ReloadAsync(booking.Id);
        after.Status.Should().Be(BookingStatus.Requested, "non-deposit bookings keep their existing confirmation flow");
    }
}
