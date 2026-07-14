import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/api/models/api_response.dart';
import '../models/auth_models.dart';

/// Auth remote data source (manual Dio — no retrofit codegen, per CLAUDE.md).
///
/// Uses the unauthenticated (`authDio`) instance for send/complete/refresh, and
/// the authenticated instance for logout.
class AuthApiService {
  final Dio _authDio;
  final Dio _authedDio;

  AuthApiService(this._authDio, this._authedDio);

  Future<ApiResponse<SendVerificationCodeResponse>> sendVerificationCode(
    SendVerificationCodeRequest request,
  ) async {
    final res = await _authDio.post(
      ApiConstants.sendVerificationCode,
      data: request.toJson(),
    );
    return ApiResponse.fromJson(
      _asMap(res.data),
      (json) => SendVerificationCodeResponse.fromJson(_asMap(json)),
    );
  }

  Future<ApiResponse<CompleteProviderAuthResponse>> completeProviderAuth(
    CompleteProviderAuthRequest request,
  ) async {
    final res = await _authDio.post(
      ApiConstants.completeProviderAuth,
      data: request.toJson(),
    );
    return ApiResponse.fromJson(
      _asMap(res.data),
      (json) => CompleteProviderAuthResponse.fromJson(_asMap(json)),
    );
  }

  Future<ApiResponse<RefreshTokenResponse>> refreshToken(
    RefreshTokenRequest request,
  ) async {
    final res = await _authDio.post(
      ApiConstants.refreshToken,
      data: request.toJson(),
    );
    return ApiResponse.fromJson(
      _asMap(res.data),
      (json) => RefreshTokenResponse.fromJson(_asMap(json)),
    );
  }

  Future<void> logout() async {
    await _authedDio.post(ApiConstants.logout);
  }

  /// GET current provider status (ServiceCatalog). Returns null when the
  /// provider has no profile yet (404). Used after onboarding completes so the
  /// locally-cached status (still "Drafted") is refreshed from the server.
  Future<({String? providerId, String? status})?> getCurrentProviderStatus() async {
    try {
      final res = await _authedDio.get(ApiConstants.providerCurrentStatus);
      final body = res.data;
      final data = (body is Map && body['data'] is Map) ? body['data'] : body;
      if (data is Map) {
        return (
          providerId: data['providerId']?.toString(),
          status: data['status']?.toString(),
        );
      }
      return null;
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return null;
      rethrow;
    }
  }

  static Map<String, dynamic> _asMap(Object? data) {
    if (data is Map<String, dynamic>) return data;
    if (data is Map) return Map<String, dynamic>.from(data);
    return <String, dynamic>{};
  }
}
