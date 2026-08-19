import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/api/models/provider_models.dart';

/// Remote data source for provider search.
/// GET /api/v1/Providers/search — SearchTerm requires ≥2 characters.
class SearchRemoteDataSource {
  final Dio serviceCatalogDio;

  SearchRemoteDataSource({required this.serviceCatalogDio});

  Future<List<ProviderDto>> searchProviders({
    String? searchTerm,
    String? serviceCategory,
    int pageNumber = 1,
    int pageSize = 20,
    double? latitude,
    double? longitude,
    double? radiusKm,
    String sortBy = 'rating',
    CancelToken? cancelToken,
  }) async {
    // Geo params ride the existing search contract (ProviderSearchRequest
    // already exposes latitude/longitude/radiusKm/sortBy) — no new endpoint.
    // Distance sort is descending-off so nearest comes first.
    final sortByDistance = sortBy == 'distance';
    final response = await serviceCatalogDio.get(
      ApiConstants.searchProviders,
      cancelToken: cancelToken,
      queryParameters: {
        if (searchTerm != null && searchTerm.length >= 2)
          'SearchTerm': searchTerm,
        if (serviceCategory != null) 'ServiceCategory': serviceCategory,
        if (latitude != null) 'Latitude': latitude,
        if (longitude != null) 'Longitude': longitude,
        if (radiusKm != null) 'RadiusKm': radiusKm,
        'PageNumber': pageNumber,
        'PageSize': pageSize,
        'SortBy': sortBy,
        'SortDescending': !sortByDistance,
      },
    );

    if (response.statusCode == 200 && response.data != null) {
      // Backend returns: { success, data: { items: [...] }, metadata } —
      // same double-wrapped shape the home datasource unwraps.
      final responseData = response.data;
      final List<dynamic> items;
      if (responseData is Map<String, dynamic>) {
        final data = responseData['data'];
        if (data is Map<String, dynamic> && data['items'] is List) {
          items = data['items'] as List<dynamic>;
        } else if (data is List) {
          items = data;
        } else {
          items = [];
        }
      } else if (responseData is List) {
        items = responseData;
      } else {
        items = [];
      }
      return items
          .map((json) => ProviderDto.fromJson(json as Map<String, dynamic>))
          .toList();
    }

    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to search providers',
    );
  }

  /// Providers near a point, WITH their coordinates and distance.
  ///
  /// Deliberately a separate call from [searchProviders]: they hit different endpoints with different
  /// response shapes. `/Providers/search` returns neither a position nor a distance, so it cannot place a
  /// pin on a map — only `/Providers/by-location` can.
  ///
  /// [category] is a `ServiceCategory` ENUM NAME (`Barbershop`, `Spa`, …), never a Persian label.
  Future<List<ProviderLocationDto>> providersByLocation({
    required double latitude,
    required double longitude,
    double radiusKm = 10,
    String? category,
    int pageNumber = 1,
    int pageSize = 50,
    CancelToken? cancelToken,
  }) async {
    final response = await serviceCatalogDio.get(
      ApiConstants.providersByLocation,
      cancelToken: cancelToken,
      queryParameters: {
        'latitude': latitude,
        'longitude': longitude,
        'radiusKm': radiusKm,
        // Only send when set: the server rejects an unknown category rather than ignoring it.
        if (category != null && category.isNotEmpty) 'type': category,
        'pageNumber': pageNumber,
        'pageSize': pageSize,
      },
    );

    if (response.statusCode == 200 && response.data != null) {
      final body = response.data;
      List<dynamic> items = const [];
      if (body is Map<String, dynamic>) {
        final data = body['data'];
        if (data is Map<String, dynamic> && data['items'] is List) {
          items = data['items'] as List<dynamic>;
        } else if (data is List) {
          items = data;
        }
      } else if (body is List) {
        items = body;
      }

      return items
          .whereType<Map<String, dynamic>>()
          .map(ProviderLocationDto.fromJson)
          .toList();
    }

    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load providers by location',
    );
  }
}

/// A provider as returned by `/Providers/by-location`.
///
/// Distinct from `ProviderDto` because the payload differs: coordinates live in a nested `coordinates`
/// object, the distance arrives pre-computed as `distanceKm`, the address is nested, and there is no
/// `profileImageUrl` — only `logoUrl`.
class ProviderLocationDto {
  final String id;
  final String businessName;
  final String? description;
  final String? category;
  final double? latitude;
  final double? longitude;
  final double? distanceKm;
  final String? logoUrl;
  final String? city;
  final String? street;
  final double? averageRating;
  final int? serviceCount;

  const ProviderLocationDto({
    required this.id,
    required this.businessName,
    this.description,
    this.category,
    this.latitude,
    this.longitude,
    this.distanceKm,
    this.logoUrl,
    this.city,
    this.street,
    this.averageRating,
    this.serviceCount,
  });

  factory ProviderLocationDto.fromJson(Map<String, dynamic> json) {
    final coords = json['coordinates'];
    final address = json['address'];

    double? asDouble(dynamic v) => (v as num?)?.toDouble();

    return ProviderLocationDto(
      id: json['id'].toString(),
      businessName: json['businessName'] as String? ?? '',
      description: json['description'] as String?,
      category: json['type'] as String?,
      latitude: coords is Map<String, dynamic> ? asDouble(coords['latitude']) : null,
      longitude: coords is Map<String, dynamic> ? asDouble(coords['longitude']) : null,
      distanceKm: asDouble(json['distanceKm']),
      logoUrl: json['logoUrl'] as String?,
      city: address is Map<String, dynamic> ? address['city'] as String? : null,
      street: address is Map<String, dynamic> ? address['street'] as String? : null,
      averageRating: asDouble(json['averageRating']),
      serviceCount: (json['serviceCount'] as num?)?.toInt(),
    );
  }
}
