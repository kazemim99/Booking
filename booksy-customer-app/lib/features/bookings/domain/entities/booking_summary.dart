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
  final DateTime startTime;
  final int durationMinutes;
  final double price;
  final String currency;
  final String status;
  final bool canCancel;
  final bool canReschedule;
  final bool canReview;
  final String? cancellationReason;

  const BookingSummary({
    required this.id,
    required this.providerId,
    required this.providerName,
    this.providerImageUrl,
    required this.serviceId,
    required this.serviceName,
    this.staffId,
    required this.startTime,
    required this.durationMinutes,
    required this.price,
    required this.currency,
    required this.status,
    required this.canCancel,
    required this.canReschedule,
    required this.canReview,
    this.cancellationReason,
  });

  bool get isUpcoming => startTime.isAfter(DateTime.now());

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
      startTime: startTime ?? this.startTime,
      durationMinutes: durationMinutes,
      price: price,
      currency: currency,
      status: status ?? this.status,
      canCancel: canCancel ?? this.canCancel,
      canReschedule: canReschedule ?? this.canReschedule,
      canReview: canReview,
      cancellationReason: cancellationReason,
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
        startTime,
        durationMinutes,
        price,
        currency,
        status,
        canCancel,
        canReschedule,
        canReview,
        cancellationReason,
      ];
}
