import 'package:equatable/equatable.dart';

import '../../../reviews/domain/entities/review.dart';

/// Booking summary for the appointments list, mapped from the backend's
/// CustomerBookingDto. Carries the ids the reschedule flow needs so no
/// extra detail fetch is required.
class BookingSummary extends Equatable {
  final String id;
  final String providerId;
  final String providerName;
  final String? providerImageUrl;
  final String serviceId;
  final String serviceName;
  final String? staffId;

  /// Who does the work, when the booking names someone — null when nobody was named, or when all the API has is
  /// an OTP placeholder or a phone number (QA recording 2026-09-23 #8).
  final String? staffName;
  final DateTime startTime;
  final int durationMinutes;
  final double price;
  final String currency;
  final String status;
  final bool canCancel;
  final bool canReschedule;

  /// The customer may review this visit now: it is on record as done and has no review yet. Said by the server
  /// (openspec/changes/_inline/customer-reviews-and-nahal-seed); an older server's copy falls back to the status.
  final bool canReview;

  /// Why it cannot be reviewed YET, in Persian — the visit is over and the salon has not marked it done, which only
  /// the salon can do. Null when it can be, when it never will be, or while the visit is still ahead.
  final String? reviewBlockedReason;

  /// The customer's review of this SALON — one per salon, so it may have been written from another visit — and
  /// where moderation stands on it; null when there is none (reviews-and-reschedule-round2 item 8).
  final String? reviewId;
  final ReviewModerationStatus? reviewStatus;

  /// The author may still edit that review: the card offers «ویرایش نظر» instead of «ثبت نظر».
  final bool reviewEditable;

  /// The visit the review was written from, when the server says (optional, additive). Another visit's review is
  /// said as «برای این سالن قبلاً نظر داده‌اید».
  final String? reviewBookingId;

  final String? cancellationReason;

  /// Why an active booking cannot be moved right now (the salon's rule, in Persian), or null when it can. The
  /// screens show «تغییر زمان» disabled with this instead of letting the customer choose a slot and only then
  /// learn the window has closed (QA 2026-09-24).
  final String? rescheduleBlockedReason;

  /// The discount the booking was made with (add-discounts-and-campaigns); [price] is already net of it. Zero and
  /// null for a booking without one, and from a server that predates discounts.
  final double discountAmount;
  final String? discountTitle;

  const BookingSummary({
    required this.id,
    required this.providerId,
    required this.providerName,
    this.providerImageUrl,
    required this.serviceId,
    required this.serviceName,
    this.staffId,
    this.staffName,
    required this.startTime,
    required this.durationMinutes,
    required this.price,
    required this.currency,
    required this.status,
    required this.canCancel,
    required this.canReschedule,
    required this.canReview,
    this.reviewBlockedReason,
    this.reviewId,
    this.reviewStatus,
    this.reviewEditable = false,
    this.reviewBookingId,
    this.cancellationReason,
    this.rescheduleBlockedReason,
    this.discountAmount = 0,
    this.discountTitle,
  });

  bool get hasDiscount => discountAmount > 0;

  /// The customer has written a review for this salon, in whatever state.
  bool get hasReview => reviewStatus != null;

  /// The review on record was written from another visit to this salon.
  bool get reviewFromOtherVisit =>
      reviewId != null && reviewBookingId != null && reviewBookingId != id;

  /// «ویرایش نظر» is offered: a review exists and its author may still change it.
  bool get canEditReview => reviewId != null && reviewEditable;

  bool get isUpcoming => startTime.isAfter(DateTime.now());

  /// A visit that took place can be booked again at the same salon, with the
  /// same service chosen.
  bool get canRebook =>
      status.toLowerCase() == 'completed' && providerId.isNotEmpty;

  BookingSummary copyWith({
    String? id,
    DateTime? startTime,
    String? status,
    bool? canCancel,
    bool? canReschedule,
    bool? canReview,
    ReviewModerationStatus? reviewStatus,
  }) {
    return BookingSummary(
      id: id ?? this.id,
      providerId: providerId,
      providerName: providerName,
      providerImageUrl: providerImageUrl,
      serviceId: serviceId,
      serviceName: serviceName,
      staffId: staffId,
      staffName: staffName,
      startTime: startTime ?? this.startTime,
      durationMinutes: durationMinutes,
      price: price,
      currency: currency,
      status: status ?? this.status,
      canCancel: canCancel ?? this.canCancel,
      canReschedule: canReschedule ?? this.canReschedule,
      canReview: canReview ?? this.canReview,
      reviewBlockedReason: reviewBlockedReason,
      reviewId: reviewId,
      reviewStatus: reviewStatus ?? this.reviewStatus,
      reviewEditable: reviewEditable,
      reviewBookingId: reviewBookingId,
      cancellationReason: cancellationReason,
      rescheduleBlockedReason: rescheduleBlockedReason,
      discountAmount: discountAmount,
      discountTitle: discountTitle,
    );
  }

  @override
  List<Object?> get props => [
        id,
        providerId,
        providerName,
        providerImageUrl,
        serviceId,
        serviceName,
        staffId,
        staffName,
        startTime,
        durationMinutes,
        price,
        currency,
        status,
        canCancel,
        canReschedule,
        canReview,
        reviewBlockedReason,
        reviewId,
        reviewStatus,
        reviewEditable,
        reviewBookingId,
        cancellationReason,
        rescheduleBlockedReason,
        discountAmount,
        discountTitle,
      ];
}
