import 'package:equatable/equatable.dart';

/// Detailed booking information
class BookingDetail extends Equatable {
  final String id;
  final String bookingReference;
  final String providerId;
  final String providerName;
  final String? providerImageUrl;
  final String providerAddress;
  final double? providerLat;
  final double? providerLng;
  final String serviceId;
  final String serviceName;
  final int serviceDuration;
  final int servicePrice;
  final String? staffId;
  final String? staffName;
  final DateTime startTime;
  final DateTime? endTime;
  final String status;
  final String? customerNotes;
  final String? cancellationReason;
  final bool canCancel;
  final bool canReschedule;
  final bool canReview;

  const BookingDetail({
    required this.id,
    required this.bookingReference,
    required this.providerId,
    required this.providerName,
    this.providerImageUrl,
    required this.providerAddress,
    this.providerLat,
    this.providerLng,
    required this.serviceId,
    required this.serviceName,
    required this.serviceDuration,
    required this.servicePrice,
    this.staffId,
    this.staffName,
    required this.startTime,
    this.endTime,
    required this.status,
    this.customerNotes,
    this.cancellationReason,
    required this.canCancel,
    required this.canReschedule,
    required this.canReview,
  });

  @override
  List<Object?> get props => [
        id,
        bookingReference,
        providerId,
        providerName,
        providerImageUrl,
        providerAddress,
        providerLat,
        providerLng,
        serviceId,
        serviceName,
        serviceDuration,
        servicePrice,
        staffId,
        staffName,
        startTime,
        endTime,
        status,
        customerNotes,
        cancellationReason,
        canCancel,
        canReschedule,
        canReview,
      ];
}
