// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/IntegrationEventHandlers/BookingCancelledRefundIntegrationEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.IntegrationEventHandlers
{
    /// <summary>
    /// Handles booking cancellation events to process automatic refunds based on refund policy
    /// </summary>
    public sealed class BookingCancelledRefundIntegrationEventHandler : IIntegrationEventHandler<BookingCancelledIntegrationEvent>
    {
        private readonly IPaymentReadRepository _paymentRepository;
        private readonly ISender _mediator;
        private readonly ILogger<BookingCancelledRefundIntegrationEventHandler> _logger;

        public BookingCancelledRefundIntegrationEventHandler(
            IPaymentReadRepository paymentRepository,
            ISender mediator,
            ILogger<BookingCancelledRefundIntegrationEventHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(BookingCancelledIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing booking cancellation for refund: BookingId={BookingId}, CancelledBy={CancelledBy}, Reason={Reason}",
                integrationEvent.BookingId,
                integrationEvent.CancelledBy,
                integrationEvent.CancellationReason);

            // Find payment for this booking
            var bookingId = Core.Domain.ValueObjects.BookingId.From(integrationEvent.BookingId);
            var payments = await _paymentRepository.GetByBookingIdAsync(bookingId, cancellationToken);

            var payment = payments.FirstOrDefault(p => p.Status == PaymentStatus.Paid);
            if (payment == null)
            {
                _logger.LogInformation(
                    "No paid payment found for cancelled booking {BookingId}. No refund needed.",
                    integrationEvent.BookingId);
                return;
            }

            // Calculate refund amount based on refund policy
            var refundPolicy = integrationEvent.RefundPolicyType switch
            {
                "Flexible" => RefundPolicy.Flexible,
                "Moderate" => RefundPolicy.Moderate,
                "Strict" => RefundPolicy.Strict,
                "NoRefunds" => RefundPolicy.NoRefunds,
                _ => RefundPolicy.Moderate
            };

            var refundAmount = refundPolicy.CalculateRefundAmount(
                payment.Amount,
                integrationEvent.BookingStartTime,
                integrationEvent.CancelledAt);

            if (refundAmount.Amount <= 0)
            {
                _logger.LogInformation(
                    "No refund applicable for booking {BookingId} based on {Policy} policy",
                    integrationEvent.BookingId,
                    integrationEvent.RefundPolicyType);
                return;
            }

            // Process refund
            var refundCommand = new RefundPaymentCommand(
                PaymentId: payment.Id.Value,
                RefundAmount: refundAmount.Amount,
                Reason: integrationEvent.CancellationReason ?? "Booking Cancelled",
                Notes: $"Automatic refund due to booking cancellation. Cancelled by: {integrationEvent.CancelledBy}");

            try
            {
                var result = await _mediator.Send(refundCommand, cancellationToken);

                if (result.IsSuccessful)
                {
                    _logger.LogInformation(
                        "Refund processed successfully for booking {BookingId}. Amount: {Amount} {Currency}",
                        integrationEvent.BookingId,
                        refundAmount.Amount,
                        refundAmount.Currency);
                }
                else
                {
                    _logger.LogError(
                        "Refund failed for booking {BookingId}. Error: {Error}",
                        integrationEvent.BookingId,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception occurred while processing refund for booking {BookingId}",
                    integrationEvent.BookingId);
                throw;
            }
        }
    }

    /// <summary>
    /// Integration event published when a booking is cancelled
    /// </summary>
    public sealed record BookingCancelledIntegrationEvent(
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        string CancelledBy,
        string? CancellationReason,
        DateTime BookingStartTime,
        DateTime CancelledAt,
        string RefundPolicyType) : IntegrationEvent;
}
