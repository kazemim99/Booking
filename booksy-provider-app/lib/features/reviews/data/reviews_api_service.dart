import 'package:dio/dio.dart';

import '../../../core/api/config/api_constants.dart';

/// The reviews endpoints the business uses (authenticated Dio).
class ReviewsApiService {
  final Dio _dio;
  ReviewsApiService(this._dio);

  Future<Map<String, dynamic>> getInbox(String providerId, {int pageNumber = 1, int pageSize = 50}) async {
    final res = await _dio.get(
      ApiConstants.reviewInbox(providerId),
      queryParameters: {'pageNumber': pageNumber, 'pageSize': pageSize},
    );
    return _unwrap(res.data);
  }

  /// The public listing's summary — one row is enough, the statistics cover every published review.
  Future<Map<String, dynamic>> getStatistics(String providerId) async {
    final res = await _dio.get(
      ApiConstants.providerReviews(providerId),
      queryParameters: const {'pageNumber': 1, 'pageSize': 1},
    );
    final statistics = _unwrap(res.data)['statistics'];
    return statistics is Map ? Map<String, dynamic>.from(statistics) : <String, dynamic>{};
  }

  Future<Map<String, dynamic>> addReply(String reviewId, String text) async =>
      _unwrap((await _dio.post(ApiConstants.reviewReply(reviewId), data: {'text': text})).data);

  Future<Map<String, dynamic>> editReply(String reviewId, String text) async =>
      _unwrap((await _dio.put(ApiConstants.reviewReply(reviewId), data: {'text': text})).data);

  Future<void> removeReply(String reviewId) => _dio.delete(ApiConstants.reviewReply(reviewId));

  static Map<String, dynamic> _unwrap(dynamic body) {
    final data = (body is Map && body['data'] is Map) ? body['data'] : body;
    return data is Map ? Map<String, dynamic>.from(data) : <String, dynamic>{};
  }
}
