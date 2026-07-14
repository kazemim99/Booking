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
    CancelToken? cancelToken,
  }) async {
    final response = await serviceCatalogDio.get(
      ApiConstants.searchProviders,
      cancelToken: cancelToken,
      queryParameters: {
        if (searchTerm != null && searchTerm.length >= 2)
          'SearchTerm': searchTerm,
        if (serviceCategory != null) 'ServiceCategory': serviceCategory,
        'PageNumber': pageNumber,
        'PageSize': pageSize,
        'SortBy': 'rating',
        'SortDescending': true,
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
}
