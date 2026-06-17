import 'package:dio/dio.dart';
import 'package:injectable/injectable.dart';
import '../../storage/secure_storage_service.dart';

/// Auth Interceptor
/// Automatically adds JWT token to requests and handles 401 errors
@injectable
class AuthInterceptor extends Interceptor {
  final SecureStorageService _storageService;
  final Dio _dio;

  AuthInterceptor(this._storageService, @Named('authDio') this._dio);

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Add access token to headers if available
    final token = await _storageService.getAccessToken();

    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }

    // Add default headers
    options.headers['Accept'] = 'application/json';
    options.headers['Content-Type'] = 'application/json';
    options.headers['Accept-Language'] = 'fa-IR';

    return handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    // Handle 401 Unauthorized - try to refresh token
    if (err.response?.statusCode == 401) {
      try {
        // Get refresh token
        final refreshToken = await _storageService.getRefreshToken();

        if (refreshToken == null || refreshToken.isEmpty) {
          // No refresh token available, logout
          await _storageService.clearAuthSession();
          return handler.reject(err);
        }

        // Try to refresh the access token
        final response = await _dio.post(
          '/v1/Auth/refresh',
          data: {'refreshToken': refreshToken},
        );

        if (response.statusCode == 200) {
          final data = response.data;
          final newAccessToken = data['data']?['accessToken'] ?? data['accessToken'];
          final newRefreshToken = data['data']?['refreshToken'] ?? data['refreshToken'];

          if (newAccessToken != null) {
            // Save new tokens
            await _storageService.saveAccessToken(newAccessToken);
            if (newRefreshToken != null) {
              await _storageService.saveRefreshToken(newRefreshToken);
            }

            // Retry the original request with new token
            final opts = Options(
              method: err.requestOptions.method,
              headers: {
                ...err.requestOptions.headers,
                'Authorization': 'Bearer $newAccessToken',
              },
            );

            final cloneReq = await _dio.request(
              err.requestOptions.path,
              options: opts,
              data: err.requestOptions.data,
              queryParameters: err.requestOptions.queryParameters,
            );

            return handler.resolve(cloneReq);
          }
        }

        // Token refresh failed, clear session
        await _storageService.clearAuthSession();
        return handler.reject(err);
      } catch (e) {
        // Token refresh failed, clear session
        await _storageService.clearAuthSession();
        return handler.reject(err);
      }
    }

    return handler.next(err);
  }
}
