import 'package:equatable/equatable.dart';

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
  final bool canReview;
  final String? cancellationReason;

  /// Why an active booking cannot be moved right now (the salon's rule, in Persian), or null when it can. The
  /// screens show «تغییر زمان» disabled with this instead of letting the customer choose a slot and only then
  /// learn the window has closed (QA 2026-09-24).
  final String? rescheduleBlockedReason;

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
    this.cancellationReason,
    this.rescheduleBlockedReason,
  });

  bool get isUpcoming => startTime.isAfter(DateTime.now());

  /// A visit that took place can be booked again at the same salon, with the
  /// same service chosen.
  bool get canRebook =>
      status.toLowerCase() == 'completed' && providerId.isNotEmpty;

  BookingSummary copyWith({
    DateTime? startTime,
    String? status,
    bool? canCancel,
    bool? canReschedule,
  }) {
    return BookingSummary(
      id: id,
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
      canReview: canReview,
      cancellationReason: cancellationReason,
      rescheduleBlockedReason: rescheduleBlockedReason,
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
        cancellationReason,
        rescheduleBlockedReason,
      ];
}
