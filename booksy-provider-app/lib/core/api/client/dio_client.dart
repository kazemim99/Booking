import 'package:dio/dio.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';
import '../config/api_constants.dart';
import '../interceptors/error_interceptor.dart';

/// Builds configured Dio instances (manual — no injectable codegen).
class DioFactory {
  DioFactory._();

  static BaseOptions _baseOptions() => BaseOptions(
        baseUrl: ApiConstants.userManagementBaseUrl,
        connectTimeout: ApiConstants.connectTimeout,
        receiveTimeout: ApiConstants.receiveTimeout,
        sendTimeout: ApiConstants.sendTimeout,
        headers: const {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'Accept-Language': ApiConstants.acceptLanguage,
        },
      );

  static PrettyDioLogger _logger() => PrettyDioLogger(
        requestHeader: false,
        requestBody: true,
        responseBody: true,
        error: true,
        compact: true,
      );

  /// Unauthenticated Dio for auth endpoints (send/complete/refresh). No auth
  /// interceptor — avoids recursion during refresh.
  static Dio createAuthDio() {
    return Dio(_baseOptions())..interceptors.addAll([ErrorInterceptor(), _logger()]);
  }

  /// Authenticated Dio. Caller adds the AuthInterceptor (which needs the
  /// unauthenticated Dio for refresh) to keep construction order explicit.
  static Dio createAuthenticatedDio(List<Interceptor> interceptors) {
    return Dio(_baseOptions())
      ..interceptors.addAll([...interceptors, ErrorInterceptor(), _logger()]);
  }
}
