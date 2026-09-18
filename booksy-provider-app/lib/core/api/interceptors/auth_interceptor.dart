import 'dart:async';
import 'package:dio/dio.dart';
import '../../storage/secure_storage_service.dart';
import '../config/api_constants.dart';

/// Injects the JWT and performs reactive token refresh on 401.
///
/// Refresh is single-flight (AUTH_SPECIFICATION.md E-5/G-8): concurrent 401s
/// share one in-flight refresh so the rotating refresh token is consumed once.
/// On refresh failure the session is cleared and [onSessionExpired] fires so
/// the app can route to login.
class AuthInterceptor extends Interceptor {
  final SecureStorageService _storage;
  final Dio _refreshDio;
  final void Function()? onSessionExpired;

  AuthInterceptor(this._storage, this._refreshDio, {this.onSessionExpired});

  Future<String>? _refreshFuture;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final token = await _storage.getAccessToken();
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final isAuthError = err.response?.statusCode == 401;
    final alreadyRetried = err.requestOptions.extra['__retried__'] == true;

    if (!isAuthError || alreadyRetried) {
      return handler.next(err);
    }

    final String newToken;
    try {
      newToken = await _coalescedRefresh();
    } catch (_) {
      // Only a failed REFRESH means the session is over.
      await _storage.clearSession();
      onSessionExpired?.call();
      // next(), not reject(): reject() skips the interceptors after this one,
      // so ErrorInterceptor never turned the error into a Persian message and
      // the user saw Dio's English explanation of the status code.
      return handler.next(err);
    }

    // Retry the original request once with the new token. If the retry fails,
    // the session is still valid (the refresh just worked), so surface the
    // retry's own error and keep the user signed in. Clearing here once turned
    // a server fault into a silent sign-out.
    final opts = err.requestOptions
      ..headers['Authorization'] = 'Bearer $newToken'
      ..extra['__retried__'] = true;
    try {
      return handler.resolve(await _refreshDio.fetch(opts));
    } on DioException catch (retryError) {
      return handler.next(retryError);
    }
  }

  /// Returns the new access token, sharing one in-flight refresh across callers.
  Future<String> _coalescedRefresh() {
    return _refreshFuture ??= _performRefresh().whenComplete(() {
      _refreshFuture = null;
    });
  }

  Future<String> _performRefresh() async {
    final refreshToken = await _storage.getRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) {
      throw StateError('No refresh token');
    }

    final response = await _refreshDio.post(
      ApiConstants.refreshToken,
      data: {'refreshToken': refreshToken},
    );

    final body = response.data;
    final data = (body is Map && body['data'] is Map) ? body['data'] : body;
    final accessToken = (data is Map ? data['accessToken'] : null) as String?;
    final newRefresh = (data is Map ? data['refreshToken'] : null) as String?;

    if (accessToken == null || accessToken.isEmpty) {
      throw StateError('No access token in refresh response');
    }

    await _storage.saveAccessToken(accessToken);
    if (newRefresh != null && newRefresh.isNotEmpty) {
      await _storage.saveRefreshToken(newRefresh);
    }
    return accessToken;
  }
}
