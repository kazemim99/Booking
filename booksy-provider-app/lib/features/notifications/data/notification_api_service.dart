import 'package:dio/dio.dart';

import '../../../core/api/config/api_constants.dart';

/// Remote data source for the caller's notification inbox (authenticated Dio).
class NotificationApiService {
  final Dio _dio;
  NotificationApiService(this._dio);

  /// The page body, unwrapped from the `{ data }` envelope when there is one.
  Future<Map<String, dynamic>> getInbox({required int pageNumber, required int pageSize}) async {
    final res = await _dio.get(
      ApiConstants.notificationsInbox,
      queryParameters: {'pageNumber': pageNumber, 'pageSize': pageSize},
    );
    return _unwrap(res.data);
  }

  Future<int> getUnreadCount() async {
    final res = await _dio.get(ApiConstants.notificationsUnreadCount);
    final count = _unwrap(res.data)['unreadCount'];
    return count is num ? count.toInt() : 0;
  }

  /// A 204 has no body; a completed call is the success signal. Non-2xx throws a DioException.
  Future<void> markRead(String id) => _dio.post(ApiConstants.notificationMarkRead(id));

  Future<void> markAllRead() => _dio.post(ApiConstants.notificationsMarkAllRead);

  static Map<String, dynamic> _unwrap(dynamic body) {
    final data = (body is Map && body['data'] is Map) ? body['data'] : body;
    return data is Map ? Map<String, dynamic>.from(data) : <String, dynamic>{};
  }
}
