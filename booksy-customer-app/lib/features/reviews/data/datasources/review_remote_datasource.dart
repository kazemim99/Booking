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
    final data = response.data is Map<String, dynamic>
        ? (response.data as Map<String, dynamic>)['data']
        : response.data;
    return data is Map<String, dynamic> ? data : const {};
  }

  Future<void> createReview({
    required String bookingId,
    required double rating,
    String? comment,
  }) =>
      serviceCatalogDio.post(
        ApiConstants.bookingReview(bookingId),
        data: {
          'rating': rating,
          if (comment != null && comment.trim().isNotEmpty) 'comment': comment.trim(),
        },
      );
}
