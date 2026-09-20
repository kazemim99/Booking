// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'provider_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ProviderDto _$ProviderDtoFromJson(Map<String, dynamic> json) => ProviderDto(
      id: json['id'] as String,
      businessName: json['businessName'] as String,
      description: json['description'] as String?,
      type: json['type'] as String?,
      status: json['status'] as String?,
      city: json['city'] as String?,
      state: json['state'] as String?,
      country: json['country'] as String?,
      logoUrl: json['logoUrl'] as String?,
      profileImageUrl: json['profileImageUrl'] as String?,
      allowOnlineBooking: json['allowOnlineBooking'] as bool?,
      offersMobileServices: json['offersMobileServices'] as bool?,
      averageRating: (json['averageRating'] as num?)?.toDouble(),
      totalReviews: (json['totalReviews'] as num?)?.toInt(),
      serviceCount: (json['serviceCount'] as num?)?.toInt(),
      yearsInBusiness: (json['yearsInBusiness'] as num?)?.toInt(),
      isVerified: json['isVerified'] as bool?,
      tags: (json['tags'] as List<dynamic>?)?.map((e) => e as String).toList(),
      registeredAt: json['registeredAt'] as String?,
      lastActiveAt: json['lastActiveAt'] as String?,
    );

Map<String, dynamic> _$ProviderDtoToJson(ProviderDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'businessName': instance.businessName,
      'description': instance.description,
      'type': instance.type,
      'status': instance.status,
      'city': instance.city,
      'state': instance.state,
      'country': instance.country,
      'logoUrl': instance.logoUrl,
      'profileImageUrl': instance.profileImageUrl,
      'allowOnlineBooking': instance.allowOnlineBooking,
      'offersMobileServices': instance.offersMobileServices,
      'averageRating': instance.averageRating,
      'totalReviews': instance.totalReviews,
      'serviceCount': instance.serviceCount,
      'yearsInBusiness': instance.yearsInBusiness,
      'isVerified': instance.isVerified,
      'tags': instance.tags,
      'registeredAt': instance.registeredAt,
      'lastActiveAt': instance.lastActiveAt,
    };

SearchProvidersRequest _$SearchProvidersRequestFromJson(
        Map<String, dynamic> json) =>
    SearchProvidersRequest(
      query: json['query'] as String?,
      categoryIds: (json['categoryIds'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      radiusKm: (json['radiusKm'] as num?)?.toDouble(),
      minRating: (json['minRating'] as num?)?.toInt(),
      maxPrice: (json['maxPrice'] as num?)?.toInt(),
      openNow: json['openNow'] as bool?,
      sortBy: json['sortBy'] as String?,
      sortOrder: json['sortOrder'] as String?,
      pageNumber: (json['pageNumber'] as num?)?.toInt(),
      pageSize: (json['pageSize'] as num?)?.toInt(),
    );

Map<String, dynamic> _$SearchProvidersRequestToJson(
        SearchProvidersRequest instance) =>
    <String, dynamic>{
      'query': instance.query,
      'categoryIds': instance.categoryIds,
      'latitude': instance.latitude,
      'longitude': instance.longitude,
      'radiusKm': instance.radiusKm,
      'minRating': instance.minRating,
      'maxPrice': instance.maxPrice,
      'openNow': instance.openNow,
      'sortBy': instance.sortBy,
      'sortOrder': instance.sortOrder,
      'pageNumber': instance.pageNumber,
      'pageSize': instance.pageSize,
    };

RecentlyVisitedProviderDto _$RecentlyVisitedProviderDtoFromJson(
        Map<String, dynamic> json) =>
    RecentlyVisitedProviderDto(
      providerId: json['providerId'] as String,
      providerName: json['providerName'] as String,
      providerType: json['providerType'] as String?,
      logoUrl: json['logoUrl'] as String?,
      city: json['city'] as String?,
      state: json['state'] as String?,
      averageRating: (json['averageRating'] as num?)?.toDouble(),
      totalReviews: (json['totalReviews'] as num?)?.toInt(),
      lastVisitedAt: DateTime.parse(json['lastVisitedAt'] as String),
      visitCount: (json['visitCount'] as num).toInt(),
      viewSource: json['viewSource'] as String?,
    );

Map<String, dynamic> _$RecentlyVisitedProviderDtoToJson(
        RecentlyVisitedProviderDto instance) =>
    <String, dynamic>{
      'providerId': instance.providerId,
      'providerName': instance.providerName,
      'providerType': instance.providerType,
      'logoUrl': instance.logoUrl,
      'city': instance.city,
      'state': instance.state,
      'averageRating': instance.averageRating,
      'totalReviews': instance.totalReviews,
      'lastVisitedAt': instance.lastVisitedAt.toIso8601String(),
      'visitCount': instance.visitCount,
      'viewSource': instance.viewSource,
    };

FavoriteProviderDto _$FavoriteProviderDtoFromJson(Map<String, dynamic> json) =>
    FavoriteProviderDto(
      providerId: json['providerId'] as String,
      providerName: json['providerName'] as String,
      providerType: json['providerType'] as String?,
      logoUrl: json['logoUrl'] as String?,
      city: json['city'] as String?,
      state: json['state'] as String?,
      averageRating: (json['averageRating'] as num?)?.toDouble(),
      totalReviews: (json['totalReviews'] as num?)?.toInt(),
      addedAt: DateTime.parse(json['addedAt'] as String),
      notes: json['notes'] as String?,
    );

Map<String, dynamic> _$FavoriteProviderDtoToJson(
        FavoriteProviderDto instance) =>
    <String, dynamic>{
      'providerId': instance.providerId,
      'providerName': instance.providerName,
      'providerType': instance.providerType,
      'logoUrl': instance.logoUrl,
      'city': instance.city,
      'state': instance.state,
      'averageRating': instance.averageRating,
      'totalReviews': instance.totalReviews,
      'addedAt': instance.addedAt.toIso8601String(),
      'notes': instance.notes,
    };

RecordProviderVisitRequest _$RecordProviderVisitRequestFromJson(
        Map<String, dynamic> json) =>
    RecordProviderVisitRequest(
      providerId: json['providerId'] as String,
      viewSource: json['viewSource'] as String?,
    );

Map<String, dynamic> _$RecordProviderVisitRequestToJson(
        RecordProviderVisitRequest instance) =>
    <String, dynamic>{
      'providerId': instance.providerId,
      'viewSource': instance.viewSource,
    };
