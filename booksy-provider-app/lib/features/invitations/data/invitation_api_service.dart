import 'package:dio/dio.dart';

import '../../../core/api/config/api_constants.dart';

/// Remote data source for the accept-invitation flow (authenticated Dio; the
/// summary endpoint is anonymous so it works with or without a token, while
/// accept requires one).
class InvitationApiService {
  final Dio _dio;
  InvitationApiService(this._dio);

  /// GET the public invitation summary, or null when the body carries no data.
  Future<Map<String, dynamic>?> getSummary(String invitationId) async {
    final res = await _dio.get(ApiConstants.invitationSummary(invitationId));
    final body = res.data;
    final data = (body is Map && body['data'] is Map) ? body['data'] : body;
    return data is Map ? Map<String, dynamic>.from(data) : null;
  }

  /// POST accept as the authenticated (existing) person.
  Future<void> accept(String invitationId) =>
      _dio.post(ApiConstants.invitationAccept(invitationId));

  /// POST send an OTP to the invitation's own phone (new-user path, step 1).
  /// Returns the masked phone the server echoes back — never the real one.
  Future<String> sendOtp(String invitationId) async {
    final res = await _dio.post(ApiConstants.invitationSendOtp(invitationId));
    final body = res.data;
    final data = (body is Map && body['data'] is Map) ? body['data'] : body;
    return (data is Map ? data['maskedPhoneNumber']?.toString() : null) ?? '';
  }

  /// POST register (name) + verify the OTP + accept, in one call (new-user
  /// path, step 2).
  Future<void> registerAndAccept(
    String invitationId, {
    required String firstName,
    required String lastName,
    String? email,
    required String otpCode,
  }) =>
      _dio.post(
        ApiConstants.invitationRegisterAndAccept(invitationId),
        data: {
          'firstName': firstName,
          'lastName': lastName,
          if (email != null && email.isNotEmpty) 'email': email,
          'otpCode': otpCode,
        },
      );
}
