import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';
import '../models/onboarding_models.dart';

/// Onboarding remote data source (authenticated Dio; manual — no codegen).
class OnboardingApiService {
  final Dio _dio;
  OnboardingApiService(this._dio);

  /// Creates the organization draft (step 3). Returns the providerId.
  Future<String> registerOrganization(RegisterOrganizationRequest request) async {
    final res = await _dio.post(
      ApiConstants.registerOrganization,
      data: request.toJson(),
    );
    return _providerId(res.data) ??
        (throw StateError('No providerId in draft response'));
  }

  Future<void> saveServices(SaveServicesRequest request) async {
    await _dio.post(ApiConstants.registrationServices, data: request.toJson());
  }

  Future<void> saveWorkingHours(SaveWorkingHoursRequest request) async {
    await _dio.post(
      ApiConstants.registrationWorkingHours,
      data: request.toJson(),
    );
  }

  Future<void> complete(String providerId) async {
    await _dio.post(
      ApiConstants.registrationComplete,
      data: {'providerId': providerId},
    );
  }

  /// Returns the draft providerId if a registration is in progress, else null.
  Future<String?> getDraftProviderId() async {
    try {
      final res = await _dio.get(ApiConstants.registrationProgress);
      final body = res.data;
      final data = (body is Map && body['data'] is Map) ? body['data'] : body;
      if (data is Map && data['hasDraft'] == true) {
        final draft = data['draftData'];
        if (draft is Map) return draft['providerId']?.toString();
      }
      return null;
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return null;
      rethrow;
    }
  }

  String? _providerId(Object? body) {
    final data = (body is Map && body['data'] is Map) ? body['data'] : body;
    if (data is Map) return data['providerId']?.toString();
    return null;
  }
}
