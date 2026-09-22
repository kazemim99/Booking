using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Review.CreateReview;

/// <summary>
/// Handler for creating a new review for a completed booking
/// Validates booking exists, is completed, and doesn't already have a review
/// </summary>
public sealed class CreateReviewCommandHandler : ICommandHandler<CreateReviewCommand, CreateReviewResult>
{
    private readonly IReviewWriteRepository _reviewWriteRepository;
    private readonly IReviewReadRepository _reviewReadRepository;
    private readonly IBookingReadRepository _bookingRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly INotificationRaiser _notifications;
    private readonly ILogger<CreateReviewCommandHandler> _logger;

    /// <summary>
    /// The notifications that exist only to ask for a review. Once one arrives, both are pointless.
    /// </summary>
    /// <remarks>
    /// Named explicitly rather than withdrawing everything unsent about the booking. That blunt form is
    /// right when the appointment itself is off; here the booking still happened, and a review says nothing
    /// about, say, a refund notice queued against the same booking.
    /// </remarks>
    private static readonly NotificationEventCode[] TheAsk =
    {
        NotificationEventCode.ReviewRequest,
        NotificationEventCode.ReviewReminder,
    };

    public CreateReviewCommandHandler(
        IReviewWriteRepository reviewWriteRepository,
        IReviewReadRepository reviewReadRepository,
        IBookingReadRepository bookingRepository,
        IServiceCatalogUnitOfWork unitOfWork,
        INotificationRaiser notifications,
        ILogger<CreateReviewCommandHandler> logger)
    {
        _reviewWriteRepository = reviewWriteRepository;
        _reviewReadRepository = reviewReadRepository;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<CreateReviewResult> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating review for Booking {BookingId} by Customer {CustomerId}",
            request.BookingId,
            request.CustomerId);

        // 1. Validate booking exists
        var booking = await _bookingRepository.GetByIdAsync(
            Domain.ValueObjects.BookingId.From(request.BookingId),
            cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException($"Booking with ID {request.BookingId} not found");
        }

        // 2. Verify customer owns the booking
        if (booking.CustomerId.Value != request.CustomerId)
        {
            throw new ForbiddenException(
                "You can only review bookings that you have made");
        }

        // 3. Verify booking is completed
        if (booking.Status != BookingStatus.Completed)
        {
            throw new ConflictException(
                $"Cannot review booking with status '{booking.Status}'. " +
                "Only completed bookings can be reviewed.");
        }

        // 4. Check if review already exists for this booking
        var existingReview = await _reviewReadRepository.GetByBookingIdAsync(
            request.BookingId,
            cancellationToken);

        if (existingReview != null)
        {
            throw new ConflictException(
                "A review already exists for this booking. " +
                "Please update the existing review instead.");
        }

        // 5. Create the review
        var review = Domain.Aggregates.Review.Create(
            providerId: booking.ProviderId,
            customerId: UserId.From(request.CustomerId),
            bookingId: request.BookingId,
            ratingValue: request.Rating,
            comment: request.Comment,
            isVerified: true, // Auto-verify reviews from actual bookings
            createdBy: $"Customer:{request.CustomerId}",
            dimensions: request.Dimensions);

        // 6. Save the review
        await _reviewWriteRepository.SaveAsync(review, cancellationToken);

        // 7. Stop asking. This is keyed on SUBMISSION, not on publication: reviews are admin-moderated, so a
        // review can exist while still invisible, and a publication-shaped hook would keep nagging the one
        // group who must never be nagged — people who did leave a review and are waiting on a moderator.
        //
        // Accepted consequence: a REJECTED review means this customer is never asked again. Nothing
        // re-raises the ask. The alternative is worse, because the notification system cannot see WHY a
        // review was rejected, so re-asking would either invite the same refused content back or read as
        // "we ignored you, try again".
        await _notifications.WithdrawPendingForSubjectAsync(
            BookingReminderScheduler.BookingSubject,
            request.BookingId,
            TheAsk,
            cancellationToken);


        _logger.LogInformation(
            "Review {ReviewId} created successfully for Booking {BookingId} with rating {Rating}★",
            review.Id,
            request.BookingId,
            request.Rating);

        // 8. Return result
        return new CreateReviewResult(
            ReviewId: review.Id,
            ProviderId: review.ProviderId.Value,
            CustomerId: review.CustomerId.Value,
            BookingId: review.BookingId,
            Rating: review.RatingValue,
            Comment: review.Comment,
            IsVerified: review.IsVerified,
            CreatedAt: review.CreatedAt,
            ModerationStatus: review.ModerationStatus,
            Dimensions: new Domain.ValueObjects.ReviewDimensionRatings(
                review.CleanlinessRating, review.SkillRating, review.PunctualityRating, review.ConductRating));
    }
}
