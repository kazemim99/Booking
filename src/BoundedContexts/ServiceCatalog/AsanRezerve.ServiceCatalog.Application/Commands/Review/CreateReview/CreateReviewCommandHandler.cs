using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Services;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Review.CreateReview;

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
    private readonly IBookingCustomer _bookingCustomer;
    private readonly ILogger<CreateReviewCommandHandler> _logger;

    /// <summary>
    /// The notifications that exist only to ask for a review. Once one arrives, both are pointless.
    /// </summary>
    /// <remarks>
    /// Named explicitly rather than withdrawing everything unsent about the booking. That blunt form is
    /// right when the appointment itself is off; here the booking still happened, and a review says nothing
    /// about, say, a refund notice queued against the same booking.
    /// </remarks>
    /// <summary>The refusal when this customer already reviewed the salon, from this visit or another.</summary>
    public const string AlreadyReviewedThisSalon =
        "برای این سالن قبلاً نظر داده‌اید؛ می‌توانید همان را از «نظرهای من» ویرایش کنید.";

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
        IBookingCustomer bookingCustomer,
        ILogger<CreateReviewCommandHandler> logger)
    {
        _reviewWriteRepository = reviewWriteRepository;
        _reviewReadRepository = reviewReadRepository;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _bookingCustomer = bookingCustomer;
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
            throw new NotFoundException("این نوبت پیدا نشد.");
        }

        // 2. Only the person the booking is for — its own customer, or for a booking the salon entered, the person
        // with that client-book entry's verified mobile. Never the salon owner stored as a salon-entered booking's
        // customer: a salon does not review itself.
        if (!await _bookingCustomer.IsForAsync(booking, request.CustomerId, cancellationToken))
        {
            throw new ForbiddenException("فقط برای نوبت‌های خودتان می‌توانید نظر ثبت کنید.");
        }

        // 3. Verify booking is completed — refused with what the customer can do about it, or the state it is in.
        if (booking.ReviewRefusal() is { } refusal)
        {
            throw new ConflictException(refusal);
        }

        // 4. Check if review already exists for this booking
        var existingReview = await _reviewReadRepository.GetByBookingIdAsync(
            request.BookingId,
            cancellationToken);

        if (existingReview != null)
        {
            throw new ConflictException(
                "برای این نوبت قبلاً نظر ثبت کرده‌اید؛ می‌توانید همان را از «نظرهای من» ویرایش کنید.");
        }

        // 4b. One review per customer per salon, from whichever visit (openspec/changes/_inline/
        // reviews-and-reschedule-round2 D4). Enforced here, not by a unique index: reviews written before the rule
        // may already hold duplicates, and an index would fail the migration on deploy. The booking-level unique
        // index stays.
        var reviewForSalon = await _reviewReadRepository.GetLatestByCustomerAndProviderAsync(
            UserId.From(request.CustomerId),
            booking.ProviderId,
            cancellationToken);

        if (reviewForSalon != null)
        {
            throw new ConflictException(AlreadyReviewedThisSalon);
        }

        // 5. Create the review. No overall sent (the current forms): the four aspects' average, to the half star.
        var overall = Domain.Aggregates.Review.OverallFrom(request.Rating, request.Dimensions);
        var review = Domain.Aggregates.Review.Create(
            providerId: booking.ProviderId,
            customerId: UserId.From(request.CustomerId),
            bookingId: request.BookingId,
            ratingValue: overall,
            comment: request.Comment,
            isVerified: true, // Auto-verify reviews from actual bookings
            createdBy: $"Customer:{request.CustomerId}",
            dimensions: request.Dimensions,
            showName: request.ShowName);

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
            review.RatingValue);

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
                review.CleanlinessRating, review.SkillRating, review.PunctualityRating, review.ConductRating),
            ShowName: review.ShowName);
    }
}
