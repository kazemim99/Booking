import 'package:json_annotation/json_annotation.dart';

part 'provider_models.g.dart';

/// Provider search result DTO
@JsonSerializable()
class ProviderDto {
  final String id;
  final String businessName;
  final String? description;
  final String? type;
  final String? status;
  final String? city;
  final String? state;
  final String? country;
  final String? logoUrl;
  final String? profileImageUrl;
  final bool? allowOnlineBooking;
  final bool? offersMobileServices;
  final double? averageRating;
  final int? totalReviews;
  final int? serviceCount;
  final int? yearsInBusiness;
  final bool? isVerified;
  final List<String>? tags;
  final String? registeredAt;
  final String? lastActiveAt;

  ProviderDto({
    required this.id,
    required this.businessName,
    this.description,
    this.type,
    this.status,
    this.city,
    this.state,
    this.country,
    this.logoUrl,
    this.profileImageUrl,
    this.allowOnlineBooking,
    this.offersMobileServices,
    this.averageRating,
    this.totalReviews,
    this.serviceCount,
    this.yearsInBusiness,
    this.isVerified,
    this.tags,
    this.registeredAt,
    this.lastActiveAt,
  });

  factory ProviderDto.fromJson(Map<String, dynamic> json) =>
      _$ProviderDtoFromJson(json);

  Map<String, dynamic> toJson() => _$ProviderDtoToJson(this);
}

/// Provider search request
@JsonSerializable()
class SearchProvidersRequest {
  final String? query;
  final List<String>? categoryIds;
  final double? latitude;
  final double? longitude;
  final double? radiusKm;
  final int? minRating;
  final int? maxPrice;
  final bool? openNow;
  final String? sortBy; // 'rating', 'distance', 'price'
  final String? sortOrder; // 'asc', 'desc'
  final int? pageNumber;
  final int? pageSize;

  SearchProvidersRequest({
    this.query,
    this.categoryIds,
    this.latitude,
    this.longitude,
    this.radiusKm,
    this.minRating,
    this.maxPrice,
    this.openNow,
    this.sortBy,
    this.sortOrder,
    this.pageNumber,
    this.pageSize,
  });

  factory SearchProvidersRequest.fromJson(Map<String, dynamic> json) =>
      _$SearchProvidersRequestFromJson(json);

  Map<String, dynamic> toJson() => _$SearchProvidersRequestToJson(this);
}

/// Recently visited provider DTO — one row of
/// GET /customers/{id}/recently-visited (RecentlyVisitedProviderViewModel).
///
/// Read tolerantly: a server from before customer-app-ux-review-fixes P1 sends
/// only `providerId`, `visitedAt` and `viewSource`, so [providerName] may be
/// missing (the repository drops such rows), [lastVisitedAt] falls back to
/// `visitedAt` and [visitCount] to 1.
@JsonSerializable()
class RecentlyVisitedProviderDto {
  final String providerId;
  final String? providerName;
  final String? providerType;
  final String? logoUrl;
  final String? city;
  final String? state;
  final double? averageRating;
  final int? totalReviews;
  @JsonKey(readValue: _readLastVisitedAt)
  final DateTime lastVisitedAt;
  @JsonKey(defaultValue: 1)
  final int visitCount;
  final String? viewSource;

  RecentlyVisitedProviderDto({
    required this.providerId,
    this.providerName,
    this.providerType,
    this.logoUrl,
    this.city,
    this.state,
    this.averageRating,
    this.totalReviews,
    required this.lastVisitedAt,
    required this.visitCount,
    this.viewSource,
  });

  factory RecentlyVisitedProviderDto.fromJson(Map<String, dynamic> json) =>
      _$RecentlyVisitedProviderDtoFromJson(json);

  Map<String, dynamic> toJson() => _$RecentlyVisitedProviderDtoToJson(this);
}

/// `lastVisitedAt`, or the older `visitedAt` a pre-P1 server sends instead.
Object? _readLastVisitedAt(Map json, String key) =>
    json[key] ?? json['visitedAt'];

/// Favorite provider DTO — one row of GET /customers/{id}/favorites
/// (FavoriteProviderViewModel).
///
/// A server from before customer-app-ux-review-fixes P1 sends only
/// `providerId`, `notes` and `addedAt`, so [providerName] may be missing; the
/// repository drops such rows.
@JsonSerializable()
class FavoriteProviderDto {
  final String providerId;
  final String? providerName;
  final String? providerType;
  final String? logoUrl;
  final String? city;
  final String? state;
  final double? averageRating;
  final int? totalReviews;
  final DateTime addedAt;
  final String? notes;

  FavoriteProviderDto({
    required this.providerId,
    this.providerName,
    this.providerType,
    this.logoUrl,
    this.city,
    this.state,
    this.averageRating,
    this.totalReviews,
    required this.addedAt,
    this.notes,
  });

  factory FavoriteProviderDto.fromJson(Map<String, dynamic> json) =>
      _$FavoriteProviderDtoFromJson(json);

  Map<String, dynamic> toJson() => _$FavoriteProviderDtoToJson(this);
}

/// Record provider visit request
@JsonSerializable()
class RecordProviderVisitRequest {
  final String providerId;
  final String? viewSource;

  RecordProviderVisitRequest({
    required this.providerId,
    this.viewSource,
  });

  factory RecordProviderVisitRequest.fromJson(Map<String, dynamic> json) =>
      _$RecordProviderVisitRequestFromJson(json);

  Map<String, dynamic> toJson() => _$RecordProviderVisitRequestToJson(this);
}
