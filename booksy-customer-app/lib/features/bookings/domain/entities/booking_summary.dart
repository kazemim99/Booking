import 'package:equatable/equatable.dart';

/// Booking summary for list view
class BookingSummary extends Equatable {
  final String id;
  final String providerName;
  final String? providerImageUrl;
  final String serviceName;
  final DateTime startTime;
  final int price;
  final String status;
  final bool canCancel;
  final bool canReschedule;
  final bool canReview;
  final String? cancellationReason;

  const BookingSummary({
    required this.id,
    required this.providerName,
    this.providerImageUrl,
    required this.serviceName,
    required this.startTime,
    required this.price,
    required this.status,
    required this.canCancel,
    required this.canReschedule,
    required this.canReview,
    this.cancellationReason,
  });

  @override
  List<Object?> get props => [
        id,
        providerName,
        providerImageUrl,
        serviceName,
        startTime,
        price,
        status,
        canCancel,
        canReschedule,
        canReview,
        cancellationReason,
      ];
}
