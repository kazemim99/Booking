import 'package:json_annotation/json_annotation.dart';

part 'booking_models.g.dart';

// ==================== Enums ====================

enum BookingStatus {
  @JsonValue('Pending')
  pending,
  @JsonValue('Requested')
  requested,
  @JsonValue('Confirmed')
  confirmed,
  @JsonValue('InProgress')
  inProgress,
  @JsonValue('Completed')
  completed,
  @JsonValue('Cancelled')
  cancelled,
  @JsonValue('NoShow')
  noShow,
}

// ==================== Request Models ====================

/// Create booking request
@JsonSerializable()
class CreateBookingRequest {
  final String customerId;
  final String providerId;
  final String serviceId;
  final String staffProviderId;
  final DateTime startTime;
  final String? customerNotes;

  CreateBookingRequest({
    required this.customerId,
    required this.providerId,
    required this.serviceId,
    required this.staffProviderId,
    required this.startTime,
    this.customerNotes,
  });

  factory CreateBookingRequest.fromJson(Map<String, dynamic> json) =>
      _$CreateBookingRequestFromJson(json);

  Map<String, dynamic> toJson() => _$CreateBookingRequestToJson(this);
}

/// Cancel booking request
@JsonSerializable()
class CancelBookingRequest {
  final String reason;
  final String? notes;

  CancelBookingRequest({
    required this.reason,
    this.notes,
  });

  factory CancelBookingRequest.fromJson(Map<String, dynamic> json) =>
      _$CancelBookingRequestFromJson(json);

  Map<String, dynamic> toJson() => _$CancelBookingRequestToJson(this);
}

/// Reschedule booking request
@JsonSerializable()
class RescheduleBookingRequest {
  final DateTime newStartTime;
  final String? reason;

  RescheduleBookingRequest({
    required this.newStartTime,
    this.reason,
  });

  factory RescheduleBookingRequest.fromJson(Map<String, dynamic> json) =>
      _$RescheduleBookingRequestFromJson(json);

  Map<String, dynamic> toJson() => _$RescheduleBookingRequestToJson(this);
}

// ==================== Response Models ====================

/// Booking DTO (Appointment)
@JsonSerializable()
class BookingDto {
  final String id;
  final String customerId;
  final String providerId;
  final String serviceId;
  final String staffProviderId;
  final DateTime startTime;
  final DateTime? endTime;
  final BookingStatus status;
  final String? customerNotes;
  final String? providerNotes;
  final double? totalAmount;
  final double? depositAmount;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final DateTime? cancelledAt;
  final String? cancellationReason;

  // Related entities (populated in API responses)
  final ServiceDto? service;
  final ProviderSummaryDto? provider;
  final StaffDto? staff;

  BookingDto({
    required this.id,
    required this.customerId,
    required this.providerId,
    required this.serviceId,
    required this.staffProviderId,
    required this.startTime,
    this.endTime,
    required this.status,
    this.customerNotes,
    this.providerNotes,
    this.totalAmount,
    this.depositAmount,
    required this.createdAt,
    this.updatedAt,
    this.cancelledAt,
    this.cancellationReason,
    this.service,
    this.provider,
    this.staff,
  });

  factory BookingDto.fromJson(Map<String, dynamic> json) =>
      _$BookingDtoFromJson(json);

  Map<String, dynamic> toJson() => _$BookingDtoToJson(this);

  /// Check if booking is upcoming
  bool get isUpcoming {
    return (status == BookingStatus.requested ||
            status == BookingStatus.confirmed ||
            status == BookingStatus.pending) &&
        startTime.isAfter(DateTime.now());
  }

  /// Check if booking is past
  bool get isPast => startTime.isBefore(DateTime.now());

  /// Check if booking can be cancelled
  bool get canCancel {
    return status == BookingStatus.requested ||
        status == BookingStatus.confirmed ||
        status == BookingStatus.pending;
  }

  /// Check if booking can be rescheduled
  bool get canReschedule {
    return status == BookingStatus.requested ||
        status == BookingStatus.confirmed;
  }
}

/// Customer Booking DTO (simplified for my-bookings endpoint)
@JsonSerializable()
class CustomerBookingDto {
  final String id;
  final String providerId;
  final String providerName;
  final String? providerImageUrl;
  final String serviceName;
  final int durationMinutes;
  final DateTime startTime;
  final BookingStatus status;
  final double? totalAmount;
  final String? staffName;

  CustomerBookingDto({
    required this.id,
    required this.providerId,
    required this.providerName,
    this.providerImageUrl,
    required this.serviceName,
    required this.durationMinutes,
    required this.startTime,
    required this.status,
    this.totalAmount,
    this.staffName,
  });

  factory CustomerBookingDto.fromJson(Map<String, dynamic> json) =>
      _$CustomerBookingDtoFromJson(json);

  Map<String, dynamic> toJson() => _$CustomerBookingDtoToJson(this);
}

/// Paginated bookings response
@JsonSerializable()
class PaginatedBookingsResponse {
  final List<CustomerBookingDto> items;
  final int totalItems;
  final int pageNumber;
  final int pageSize;
  final int totalPages;

  PaginatedBookingsResponse({
    required this.items,
    required this.totalItems,
    required this.pageNumber,
    required this.pageSize,
    required this.totalPages,
  });

  factory PaginatedBookingsResponse.fromJson(Map<String, dynamic> json) =>
      _$PaginatedBookingsResponseFromJson(json);

  Map<String, dynamic> toJson() => _$PaginatedBookingsResponseToJson(this);

  bool get hasNextPage => pageNumber < totalPages;
  bool get hasPreviousPage => pageNumber > 1;
}

// ==================== Supporting DTOs ====================

/// Service DTO (minimal for bookings)
@JsonSerializable()
class ServiceDto {
  final String id;
  final String name;
  final String? description;
  final int durationMinutes;
  final double price;
  final String? imageUrl;

  ServiceDto({
    required this.id,
    required this.name,
    this.description,
    required this.durationMinutes,
    required this.price,
    this.imageUrl,
  });

  factory ServiceDto.fromJson(Map<String, dynamic> json) =>
      _$ServiceDtoFromJson(json);

  Map<String, dynamic> toJson() => _$ServiceDtoToJson(this);
}

/// Provider Summary DTO (minimal for bookings)
@JsonSerializable()
class ProviderSummaryDto {
  final String id;
  final String businessName;
  final String? logoUrl;
  final String? address;
  final double? rating;

  ProviderSummaryDto({
    required this.id,
    required this.businessName,
    this.logoUrl,
    this.address,
    this.rating,
  });

  factory ProviderSummaryDto.fromJson(Map<String, dynamic> json) =>
      _$ProviderSummaryDtoFromJson(json);

  Map<String, dynamic> toJson() => _$ProviderSummaryDtoToJson(this);
}

/// Staff DTO
@JsonSerializable()
class StaffDto {
  final String id;
  final String name;
  final String? profilePictureUrl;
  final String? specialization;

  StaffDto({
    required this.id,
    required this.name,
    this.profilePictureUrl,
    this.specialization,
  });

  factory StaffDto.fromJson(Map<String, dynamic> json) =>
      _$StaffDtoFromJson(json);

  Map<String, dynamic> toJson() => _$StaffDtoToJson(this);
}
