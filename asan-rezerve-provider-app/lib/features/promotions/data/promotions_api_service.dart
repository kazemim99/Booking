import 'package:dio/dio.dart';

import '../../../core/api/config/api_constants.dart';

/// The discount endpoints the salon uses (authenticated Dio).
class PromotionsApiService {
  final Dio _dio;
  PromotionsApiService(this._dio);

  Future<List<Map<String, dynamic>>> list(String providerId) async =>
      _unwrapList((await _dio.get(ApiConstants.providerPromotions(providerId))).data);

  Future<Map<String, dynamic>> create(String providerId, Map<String, dynamic> body) async =>
      _unwrap((await _dio.post(ApiConstants.providerPromotions(providerId), data: body)).data);

  Future<Map<String, dynamic>> update(String providerId, String promotionId, Map<String, dynamic> body) async =>
      _unwrap((await _dio.put(ApiConstants.providerPromotion(providerId, promotionId), data: body)).data);

  /// [action] is `pause`, `resume` or `end`.
  Future<Map<String, dynamic>> change(String providerId, String promotionId, String action) async =>
      _unwrap((await _dio.post('${ApiConstants.providerPromotion(providerId, promotionId)}/$action')).data);

  Future<List<Map<String, dynamic>>> campaigns(String providerId) async =>
      _unwrapList((await _dio.get(ApiConstants.providerCampaigns(providerId))).data);

  Future<Map<String, dynamic>> join(String providerId, String campaignId) async =>
      _unwrap((await _dio.post(ApiConstants.campaignEnrollment(providerId, campaignId))).data);

  Future<Map<String, dynamic>> leave(String providerId, String campaignId) async =>
      _unwrap((await _dio.delete(ApiConstants.campaignEnrollment(providerId, campaignId))).data);

  static Map<String, dynamic> _unwrap(dynamic body) {
    final data = (body is Map && body['data'] is Map) ? body['data'] : body;
    return data is Map ? Map<String, dynamic>.from(data) : <String, dynamic>{};
  }

  static List<Map<String, dynamic>> _unwrapList(dynamic body) {
    final data = (body is Map && body.containsKey('data')) ? body['data'] : body;
    return data is List ? data.whereType<Map>().map((m) => Map<String, dynamic>.from(m)).toList() : const [];
  }
}
