import 'package:dio/dio.dart';

import '../../../../core/api/config/api_constants.dart';

/// The reviews endpoints, as the catalogue exposes them.
class ReviewRemoteDataSource {
  final Dio serviceCatalogDio;

  ReviewRemoteDataSource({required this.serviceCatalogDio});

  Future<Map<String, dynamic>> getProviderReviews(String providerId) async {
    final response = await serviceCatalogDio.get(
      ApiConstants.providerReviews(providerId),
      queryParameters: const {'pageNumber': 1, 'pageSize': 20},
    );
    return _unwrap(response.data);
  }

  Future<void> createReview({
    required String bookingId,
    required Map<String, dynamic> body,
  }) =>
      serviceCatalogDio.post(ApiConstants.bookingReview(bookingId), data: body);

  Future<void> editReview({
    required String reviewId,
    required Map<String, dynamic> body,
  }) =>
      serviceCatalogDio.put(ApiConstants.review(reviewId), data: body);

  Future<Map<String, dynamic>> vote(String reviewId, bool isHelpful) async {
    final response = await serviceCatalogDio.put(
      ApiConstants.reviewVote(reviewId),
      data: {'isHelpful': isHelpful},
    );
    return _unwrap(response.data);
  }

  Future<Map<String, dynamic>> getMyReviews() async {
    final response = await serviceCatalogDio.get(
      ApiConstants.myReviews,
      queryParameters: const {'pageNumber': 1, 'pageSize': 50},
    );
    return _unwrap(response.data);
  }

  /// Some responses arrive inside a `{ data: … }` envelope, some bare.
  static Map<String, dynamic> _unwrap(Object? body) {
    final data = body is Map<String, dynamic> && body['data'] is Map<String, dynamic>
        ? body['data']
        : body;
    return data is Map<String, dynamic> ? data : const {};
  }
}
