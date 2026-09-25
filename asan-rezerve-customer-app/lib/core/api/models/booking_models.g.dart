// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'booking_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

CreateBookingRequest _$CreateBookingRequestFromJson(
        Map<String, dynamic> json) =>
    CreateBookingRequest(
      customerId: json['customerId'] as String,
      providerId: json['providerId'] as String,
      serviceId: json['serviceId'] as String,
      staffProviderId: json['staffProviderId'] as String,
      startTime: parseWallClock(json['startTime'] as String),
      customerNotes: json['customerNotes'] as String?,
    );

Map<String, dynamic> _$CreateBookingRequestToJson(
        CreateBookingRequest instance) =>
    <String, dynamic>{
      'customerId': instance.customerId,
      'providerId': instance.providerId,
      'serviceId': instance.serviceId,
      'staffProviderId': instance.staffProviderId,
      'startTime': instance.startTime.toIso8601String(),
      'customerNotes': instance.customerNotes,
    };

CancelBookingRequest _$CancelBookingRequestFromJson(
        Map<String, dynamic> json) =>
    CancelBookingRequest(
      reason: json['reason'] as String,
      notes: json['notes'] as String?,
    );

Map<String, dynamic> _$CancelBookingRequestToJson(
        CancelBookingRequest instance) =>
    <String, dynamic>{
      'reason': instance.reason,
      'notes': instance.notes,
    };

RescheduleBookingRequest _$RescheduleBookingRequestFromJson(
        Map<String, dynamic> json) =>
    RescheduleBookingRequest(
      newStartTime: DateTime.parse(json['newStartTime'] as String),
      reason: json['reason'] as String?,
    );

Map<String, dynamic> _$RescheduleBookingRequestToJson(
        RescheduleBookingRequest instance) =>
    <String, dynamic>{
      'newStartTime': instance.newStartTime.toIso8601String(),
      'reason': instance.reason,
    };

BookingDto _$BookingDtoFromJson(Map<String, dynamic> json) => BookingDto(
      id: json['id'] as String,
      customerId: json['customerId'] as String,
      providerId: json['providerId'] as String,
      serviceId: json['serviceId'] as String,
      staffProviderId: json['staffProviderId'] as String,
      startTime: parseWallClock(json['startTime'] as String),
      endTime: json['endTime'] == null
          ? null
          : parseWallClock(json['endTime'] as String),
      status: $enumDecode(_$BookingStatusEnumMap, json['status']),
      customerNotes: json['customerNotes'] as String?,
      providerNotes: json['providerNotes'] as String?,
      totalAmount: (json['totalAmount'] as num?)?.toDouble(),
      depositAmount: (json['depositAmount'] as num?)?.toDouble(),
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
      cancelledAt: json['cancelledAt'] == null
          ? null
          : DateTime.parse(json['cancelledAt'] as String),
      cancellationReason: json['cancellationReason'] as String?,
      service: json['service'] == null
          ? null
          : ServiceDto.fromJson(json['service'] as Map<String, dynamic>),
      provider: json['provider'] == null
          ? null
          : ProviderSummaryDto.fromJson(
              json['provider'] as Map<String, dynamic>),
      staff: json['staff'] == null
          ? null
          : StaffDto.fromJson(json['staff'] as Map<String, dynamic>),
    );

Map<String, dynamic> _$BookingDtoToJson(BookingDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'customerId': instance.customerId,
      'providerId': instance.providerId,
      'serviceId': instance.serviceId,
      'staffProviderId': instance.staffProviderId,
      'startTime': instance.startTime.toIso8601String(),
      'endTime': instance.endTime?.toIso8601String(),
      'status': _$BookingStatusEnumMap[instance.status]!,
      'customerNotes': instance.customerNotes,
      'providerNotes': instance.providerNotes,
      'totalAmount': instance.totalAmount,
      'depositAmount': instance.depositAmount,
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': instance.updatedAt?.toIso8601String(),
      'cancelledAt': instance.cancelledAt?.toIso8601String(),
      'cancellationReason': instance.cancellationReason,
      'service': instance.service,
      'provider': instance.provider,
      'staff': instance.staff,
    };

const _$BookingStatusEnumMap = {
  BookingStatus.pending: 'Pending',
  BookingStatus.requested: 'Requested',
  BookingStatus.confirmed: 'Confirmed',
  BookingStatus.inProgress: 'InProgress',
  BookingStatus.completed: 'Completed',
  BookingStatus.cancelled: 'Cancelled',
  BookingStatus.noShow: 'NoShow',
};

CustomerBookingDto _$CustomerBookingDtoFromJson(Map<String, dynamic> json) =>
    CustomerBookingDto(
      id: json['id'] as String,
      providerId: json['providerId'] as String,
      providerName: json['providerName'] as String,
      providerImageUrl: json['providerImageUrl'] as String?,
      serviceName: json['serviceName'] as String,
      durationMinutes: (json['durationMinutes'] as num).toInt(),
      startTime: parseWallClock(json['startTime'] as String),
      status: $enumDecode(_$BookingStatusEnumMap, json['status']),
      totalAmount: (json['totalAmount'] as num?)?.toDouble(),
      staffName: json['staffName'] as String?,
    );

Map<String, dynamic> _$CustomerBookingDtoToJson(CustomerBookingDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'providerId': instance.providerId,
      'providerName': instance.providerName,
      'providerImageUrl': instance.providerImageUrl,
      'serviceName': instance.serviceName,
      'durationMinutes': instance.durationMinutes,
      'startTime': instance.startTime.toIso8601String(),
      'status': _$BookingStatusEnumMap[instance.status]!,
      'totalAmount': instance.totalAmount,
      'staffName': instance.staffName,
    };

PaginatedBookingsResponse _$PaginatedBookingsResponseFromJson(
        Map<String, dynamic> json) =>
    PaginatedBookingsResponse(
      items: (json['items'] as List<dynamic>)
          .map((e) => CustomerBookingDto.fromJson(e as Map<String, dynamic>))
          .toList(),
      totalItems: (json['totalItems'] as num).toInt(),
      pageNumber: (json['pageNumber'] as num).toInt(),
      pageSize: (json['pageSize'] as num).toInt(),
      totalPages: (json['totalPages'] as num).toInt(),
    );

Map<String, dynamic> _$PaginatedBookingsResponseToJson(
        PaginatedBookingsResponse instance) =>
    <String, dynamic>{
      'items': instance.items,
      'totalItems': instance.totalItems,
      'pageNumber': instance.pageNumber,
      'pageSize': instance.pageSize,
      'totalPages': instance.totalPages,
    };

ServiceDto _$ServiceDtoFromJson(Map<String, dynamic> json) => ServiceDto(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String?,
      durationMinutes: (json['durationMinutes'] as num).toInt(),
      price: (json['price'] as num).toDouble(),
      imageUrl: json['imageUrl'] as String?,
    );

Map<String, dynamic> _$ServiceDtoToJson(ServiceDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'description': instance.description,
      'durationMinutes': instance.durationMinutes,
      'price': instance.price,
      'imageUrl': instance.imageUrl,
    };

ProviderSummaryDto _$ProviderSummaryDtoFromJson(Map<String, dynamic> json) =>
    ProviderSummaryDto(
      id: json['id'] as String,
      businessName: json['businessName'] as String,
      logoUrl: json['logoUrl'] as String?,
      address: json['address'] as String?,
      rating: (json['rating'] as num?)?.toDouble(),
    );

Map<String, dynamic> _$ProviderSummaryDtoToJson(ProviderSummaryDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'businessName': instance.businessName,
      'logoUrl': instance.logoUrl,
      'address': instance.address,
      'rating': instance.rating,
    };

StaffDto _$StaffDtoFromJson(Map<String, dynamic> json) => StaffDto(
      id: json['id'] as String,
      name: json['name'] as String,
      profilePictureUrl: json['profilePictureUrl'] as String?,
      specialization: json['specialization'] as String?,
    );

Map<String, dynamic> _$StaffDtoToJson(StaffDto instance) => <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'profilePictureUrl': instance.profilePictureUrl,
      'specialization': instance.specialization,
    };
