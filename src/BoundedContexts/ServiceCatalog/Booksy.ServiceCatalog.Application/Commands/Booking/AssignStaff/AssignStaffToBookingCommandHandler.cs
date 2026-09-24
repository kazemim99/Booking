// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/AssignStaff/AssignStaffToBookingCommandHandler.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.AssignStaff
{
    public sealed class AssignStaffToBookingCommandHandler : ICommandHandler<AssignStaffToBookingCommand, AssignStaffToBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly INotificationRaiser _notifications;
        private readonly Domain.Repositories.IOrganizationMembershipRepository _memberships;
        private readonly Domain.Repositories.IProviderReadRepository _providers;
        private readonly ILogger<AssignStaffToBookingCommandHandler> _logger;

        private readonly IBookingNotificationParameters _bookingParameters;

        public AssignStaffToBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IServiceCatalogUnitOfWork unitOfWork,
            INotificationRaiser notifications,
            Domain.Repositories.IOrganizationMembershipRepository memberships,
            Domain.Repositories.IProviderReadRepository providers,
            ILogger<AssignStaffToBookingCommandHandler> logger,
            IBookingNotificationParameters bookingParameters)
        {
            _bookingParameters = bookingParameters;
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _notifications = notifications;
            _memberships = memberships;
            _providers = providers;
            _logger = logger;
        }

        public async Task<AssignStaffToBookingResult> Handle(AssignStaffToBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Assigning staff {StaffId} to booking {BookingId}",
                request.StaffId, request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException("این نوبت پیدا نشد.");

            var previousStaffId = booking.StaffId;

            // Assign staff
            booking.AssignStaff(request.StaffId);

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // Tell the person whose day just changed. Not the salon: it made this assignment and is
            // already looking at the screen it made it on.
            //
            // The staff id on a booking is a MEMBERSHIP, not a person, so the recipient has to be
            // resolved — addressing the membership id would reach nobody, the same way a provider id
            // would. A membership that cannot be resolved is skipped rather than guessed at.
            var membership = await _memberships.GetByIdAsync(request.StaffId, cancellationToken);
            if (membership is not null)
            {
                var assigningProvider = await _providers.GetByIdAsync(booking.ProviderId, cancellationToken);
                await _notifications.RaiseAsync(
                    Domain.Enums.NotificationEventCode.StaffAssignedToBooking,
                    membership.PersonId,
                    dedupKey: booking.Id.Value,
                    parameters: await _bookingParameters.ForAsync(
                        booking, assigningProvider?.Profile.BusinessName, cancellationToken),
                    subjectType: "Booking",
                    subjectId: booking.Id.Value,
                    cancellationToken: cancellationToken);
            }

            // Commit and publish the StaffAssignedToBookingEvent (matches Cancel/MarkNoShow).
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Staff {StaffId} assigned to booking {BookingId} successfully",
                request.StaffId, booking.Id);

            return new AssignStaffToBookingResult(
                BookingId: booking.Id.Value,
                PreviousStaffId: previousStaffId,
                NewStaffId: booking.StaffId,
                AssignedAt: DateTime.UtcNow);
        }
    }
}
