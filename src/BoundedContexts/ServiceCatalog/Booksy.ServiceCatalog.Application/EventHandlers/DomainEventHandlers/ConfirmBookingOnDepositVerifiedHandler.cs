using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    /// <summary>
    /// C4 (create-then-pay) — closes gap B1. A deposit-required booking is created in <c>Requested</c> holding the
    /// slot, and the domain already refuses <c>Booking.Confirm()</c> until the deposit is paid
    /// (<c>DepositMustBePaidBeforeConfirmationRule</c>). Nothing, however, ever recorded the deposit — so such a
    /// booking could never reach <c>Confirmed</c>. This handler closes the loop: when a payment is verified by the
    /// gateway, it records the deposit on the booking and then confirms it through the existing domain gate.
    /// <para>
    /// Uses only existing aggregate methods and gates (no domain redesign) and preserves current behavior for
    /// bookings whose policy does not require a deposit — those are untouched and confirm via their normal path.
    /// </para>
    /// <para>
    /// <b>Idempotent on replay.</b> <c>RecordDepositPaid</c> no-ops when the deposit is already covered, and
    /// <c>Confirm()</c> is only attempted from <c>Requested</c>, so a redelivered event changes nothing.
    /// </para>
    /// <para>
    /// <b>Never fails the payment.</b> The money has already moved by the time this runs, so a booking that cannot
    /// be confirmed (e.g. its booking window has since passed) is logged for operator follow-up while the recorded
    /// deposit is still committed — the payment verification itself must not be rolled back.
    /// </para>
    /// </summary>
    public sealed class ConfirmBookingOnDepositVerifiedHandler : IDomainEventHandler<PaymentVerifiedEvent>
    {
        private readonly IBookingWriteRepository _bookings;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<ConfirmBookingOnDepositVerifiedHandler> _logger;

        public ConfirmBookingOnDepositVerifiedHandler(
            IBookingWriteRepository bookings,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<ConfirmBookingOnDepositVerifiedHandler> logger)
        {
            _bookings = bookings;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentVerifiedEvent e, CancellationToken cancellationToken)
        {
            if (e.BookingId is null)
                return; // direct (non-booking) payment — nothing to confirm

            var booking = await _bookings.GetByIdAsync(e.BookingId, cancellationToken);
            if (booking is null)
            {
                _logger.LogWarning(
                    "Payment {PaymentId} verified for booking {BookingId}, but the booking was not found",
                    e.PaymentId.Value, e.BookingId.Value);
                return;
            }

            // Only deposit-required bookings are gated on payment; leave every other booking's flow unchanged.
            if (!booking.Policy.RequireDeposit)
                return;

            var alreadyDepositPaid = booking.PaymentInfo.IsDepositPaid();
            booking.RecordDepositPaid(e.RefNumber); // idempotent: no-op when already covered

            // Confirm through the existing domain gate. Only from Requested, so a replay (already Confirmed) is a no-op.
            var confirmed = false;
            if (booking.Status == BookingStatus.Requested && booking.PaymentInfo.IsDepositPaid())
            {
                try
                {
                    booking.Confirm();
                    confirmed = true;
                }
                catch (Exception ex)
                {
                    // The deposit is real and stays recorded; the booking needs operator attention (e.g. the
                    // booking window elapsed while the customer was paying).
                    _logger.LogError(ex,
                        "Deposit recorded for booking {BookingId} (payment {PaymentId}) but confirmation was refused; " +
                        "the booking remains {Status} and requires follow-up",
                        booking.Id.Value, e.PaymentId.Value, booking.Status);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            if (!alreadyDepositPaid || confirmed)
            {
                _logger.LogInformation(
                    "Booking {BookingId}: deposit recorded from payment {PaymentId} (ref {RefNumber}); confirmed={Confirmed}",
                    booking.Id.Value, e.PaymentId.Value, e.RefNumber, confirmed);
            }
        }
    }
}
