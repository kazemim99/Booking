import 'package:dio/dio.dart';

import '../api/config/api_constants.dart';
import 'push_token_source.dart';

/// `POST /DeviceTokens` and `DELETE /DeviceTokens?token=` — both scoped to the caller by the backend, so a
/// device can only ever be attached to, or removed from, the signed-in account.
class DioDeviceTokenApi implements DeviceTokenApi {
  final Dio serviceCatalogDio;
  DioDeviceTokenApi({required this.serviceCatalogDio});

  @override
  Future<void> register(String token, String platform) =>
      serviceCatalogDio.post(ApiConstants.deviceTokens, data: {'token': token, 'platform': platform});

  @override
  Future<void> revoke(String token) =>
      serviceCatalogDio.delete(ApiConstants.deviceTokens, queryParameters: {'token': token});
}
