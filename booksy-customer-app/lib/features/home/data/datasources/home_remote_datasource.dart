import 'package:dio/dio.dart';
import 'package:injectable/injectable.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/api/models/category_models.dart';
import '../../../../core/api/models/booking_models.dart';
import '../../../../core/api/models/provider_models.dart';

/// Remote data source for home screen
@lazySingleton
class HomeRemoteDataSource {
  final Dio serviceCatalogDio;
  final Dio userManagementDio;

  HomeRemoteDataSource({
    @Named('serviceCatalogDio') required this.serviceCatalogDio,
    @Named('userManagementDio') required this.userManagementDio,
  });

  /// Fetch popular categories
  Future<List<PopularCategoryDto>> getPopularCategories() async {
    final response = await serviceCatalogDio.get(
      ApiConstants.popularCategories,
    );

    if (response.statusCode == 200 && response.data != null) {
      // Backend returns wrapped response: { success: true, data: [...], metadata: {...} }
      final responseData = response.data;

      final List<dynamic> items;
      if (responseData is Map<String, dynamic>) {
        // Extract data from wrapped response
        final data = responseData['data'];
        if (data is List) {
          items = data;
        } else {
          items = [];
        }
      } else if (responseData is List) {
        items = responseData;
      } else {
        items = [];
      }

      return items.map((json) => PopularCategoryDto.fromJson(json as Map<String, dynamic>)).toList();
    }

    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load categories',
    );
  }

  /// Fetch upcoming bookings
  Future<List<BookingDto>> getUpcomingBookings({int limit = 2}) async {
    final response = await userManagementDio.get(
      ApiConstants.myBookings,
      queryParameters: {
        'filter': 'upcoming',
        'limit': limit,
      },
    );

    if (response.statusCode == 200 && response.data != null) {
      // Backend returns wrapped response: { success: true, data: [...], metadata: {...} }
      final responseData = response.data;

      final List<dynamic> items;
      if (responseData is Map<String, dynamic>) {
        // Extract data from wrapped response
        final data = responseData['data'];
        if (data is List) {
          items = data;
        } else {
          items = [];
        }
      } else if (responseData is List) {
        items = responseData;
      } else {
        items = [];
      }

      return items.map((json) => BookingDto.fromJson(json as Map<String, dynamic>)).toList();
    }

    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load bookings',
    );
  }

  /// How soon each of these salons can be booked — one call for the whole list.
  Future<List<Map<String, dynamic>>> getAvailabilitySummary(
      List<String> providerIds) async {
    if (providerIds.isEmpty) return const [];
    final response = await serviceCatalogDio.get(
      ApiConstants.providerAvailabilitySummary,
      queryParameters: {'providerIds': providerIds},
    );
    final data = response.data is Map<String, dynamic>
        ? (response.data as Map<String, dynamic>)['data']
        : response.data;
    if (data is! List) return const [];
    return data.whereType<Map<String, dynamic>>().toList();
  }

  /// Fetch top providers (recommended)
  Future<List<ProviderDto>> getTopProviders({int limit = 10}) async {
    final response = await serviceCatalogDio.get(
      ApiConstants.searchProviders,
      queryParameters: {
        'pageSize': limit,
        'pageNumber': 1,
        'sortBy': 'rating',
        'sortOrder': 'desc',
      },
    );

    if (response.statusCode == 200 && response.data != null) {
      // Backend returns double-wrapped response:
      // { success: true, data: { items: [...], pageNumber: 1, ... }, metadata: {...} }
      final responseData = response.data;

      final List<dynamic> items;
      if (responseData is Map<String, dynamic>) {
        // Extract data.items from the double-wrapped response
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

      return items.map((json) => ProviderDto.fromJson(json as Map<String, dynamic>)).toList();
    }

    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load providers',
    );
  }

  /// Fetch promotions (mocked for now - backend endpoint TBD)
  Future<List<Map<String, dynamic>>> getPromotions() async {
    // TODO: Replace with actual API call when backend implements promotions
    // For now, return empty list
    return [];
  }

  /// Fetch recently visited providers for a customer
  Future<List<RecentlyVisitedProviderDto>> getRecentlyVisitedProviders(String customerId, {int limit = 10}) async {
    final response = await userManagementDio.get(
      ApiConstants.recentlyVisitedProviders(customerId),
      queryParameters: {'limit': limit},
    );

    if (response.statusCode == 200) {
      // Handle null response data
      if (response.data == null) {
        return [];
      }

      final responseData = response.data;

      final List<dynamic> items;
      if (responseData is Map<String, dynamic>) {
        final data = responseData['data'];
        if (data == null) {
          return [];
        }
        if (data is List) {
          items = data;
        } else {
          items = [];
        }
      } else if (responseData is List) {
        items = responseData;
      } else {
        items = [];
      }

      return _readRows(items, RecentlyVisitedProviderDto.fromJson);
    }

    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load recently visited providers',
    );
  }

  /// Each row that can be read, in order. One malformed row (a missing id or
  /// date) is skipped rather than failing the whole Home section: the section
  /// shows the salons it can, or stays hidden.
  static List<T> _readRows<T>(
      List<dynamic> items, T Function(Map<String, dynamic>) fromJson) {
    final rows = <T>[];
    for (final item in items) {
      if (item is! Map<String, dynamic>) continue;
      try {
        rows.add(fromJson(item));
      } catch (_) {
        // Skipped: see above.
      }
    }
    return rows;
  }

  /// Fetch favorite providers for a customer
  Future<List<FavoriteProviderDto>> getFavoriteProviders(String customerId) async {
    final response = await userManagementDio.get(
      ApiConstants.customerFavorites(customerId),
    );

    if (response.statusCode == 200) {
      // Handle null response data
      if (response.data == null) {
        return [];
      }

      final responseData = response.data;

      final List<dynamic> items;
      if (responseData is Map<String, dynamic>) {
        final data = responseData['data'];
        if (data == null) {
          return [];
        }
        if (data is List) {
          items = data;
        } else {
          items = [];
        }
      } else if (responseData is List) {
        items = responseData;
      } else {
        items = [];
      }

      return _readRows(items, FavoriteProviderDto.fromJson);
    }

    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load favorite providers',
    );
  }

  /// The ids of the customer's favourite salons.
  ///
  /// Parsed by hand from `FavoriteProviderViewModel`, which carries only
  /// `providerId`, `notes` and `addedAt` — [getFavoriteProviders]'s generated
  /// parser also demands a `providerName` the server never sends.
  Future<Set<String>> getFavoriteProviderIds(String customerId) async {
    final response = await userManagementDio.get(
      ApiConstants.customerFavorites(customerId),
    );
    final body = response.data;
    final rows = body is Map<String, dynamic> ? body['data'] : body;
    if (rows is! List) return const {};
    return {
      for (final row in rows.whereType<Map<String, dynamic>>())
        if (row['providerId'] is String) row['providerId'] as String,
    };
  }

  /// Add a salon to the customer's favourites (AddFavoriteProviderRequest).
  Future<void> addFavoriteProvider(String customerId, String providerId) async {
    await userManagementDio.post(
      ApiConstants.customerFavorites(customerId),
      data: {'providerId': providerId},
    );
  }

  /// Remove a salon from the customer's favourites.
  Future<void> removeFavoriteProvider(
      String customerId, String providerId) async {
    await userManagementDio.delete(
      ApiConstants.customerFavorite(customerId, providerId),
    );
  }

  /// Record a provider visit
  Future<void> recordProviderVisit(String customerId, String providerId, {String? viewSource}) async {
    final response = await userManagementDio.post(
      ApiConstants.recordProviderVisit(customerId),
      data: RecordProviderVisitRequest(
        providerId: providerId,
        viewSource: viewSource,
      ).toJson(),
    );

    if (response.statusCode != 200) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        type: DioExceptionType.badResponse,
        message: 'Failed to record provider visit',
      );
    }
  }
}
