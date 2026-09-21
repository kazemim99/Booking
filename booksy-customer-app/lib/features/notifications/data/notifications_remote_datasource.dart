import 'package:dio/dio.dart';

import '../../../core/api/config/api_constants.dart';

/// Remote data source for the customer's notification inbox. Manual JSON handling, like the app's other
/// data sources.
class NotificationsRemoteDataSource {
  final Dio serviceCatalogDio;

  NotificationsRemoteDataSource({required this.serviceCatalogDio});

  static Map<String, dynamic> _unwrap(dynamic body) {
    final data = (body is Map && body['data'] is Map) ? body['data'] : body;
    return data is Map ? Map<String, dynamic>.from(data) : <String, dynamic>{};
  }

  Future<Map<String, dynamic>> getInbox({required int pageNumber, required int pageSize}) async {
    final response = await serviceCatalogDio.get(
      ApiConstants.notificationsInbox,
      queryParameters: {'pageNumber': pageNumber, 'pageSize': pageSize},
    );
    return _unwrap(response.data);
  }

  Future<int> getUnreadCount() async {
    final response = await serviceCatalogDio.get(ApiConstants.notificationsUnreadCount);
    final count = _unwrap(response.data)['unreadCount'];
    return count is num ? count.toInt() : 0;
  }

  /// A 204 has no body; a completed call is the success signal. Non-2xx throws a DioException.
  Future<void> markRead(String id) => serviceCatalogDio.post(ApiConstants.notificationMarkRead(id));

  Future<void> markAllRead() => serviceCatalogDio.post(ApiConstants.notificationsMarkAllRead);
}
