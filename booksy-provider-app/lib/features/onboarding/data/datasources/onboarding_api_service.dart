import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../domain/entities/onboarding_data.dart';
import '../../domain/entities/onboarding_draft.dart';
import '../models/draft_snapshot.dart';
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

  /// Uploads gallery images (step 7). Multipart under the `files` field — the
  /// backend resolves the draft from the authenticated user, so no providerId
  /// is sent in the body.
  Future<void> uploadGallery(List<GalleryImageUpload> images) async {
    final form = FormData();
    for (final img in images) {
      form.files.add(MapEntry(
        'files',
        MultipartFile.fromBytes(img.bytes, filename: img.name),
      ));
    }
    await _dio.post(ApiConstants.registrationGallery, data: form);
  }

  Future<void> complete(String providerId) async {
    await _dio.post(
      ApiConstants.registrationComplete,
      data: {'providerId': providerId},
    );
  }

  /// Returns the resumable draft (all saved fields), or null if none.
  Future<OnboardingDraft?> getDraft() async {
    try {
      final res = await _dio.get(ApiConstants.registrationProgress);
      final body = res.data;
      final data = (body is Map && body['data'] is Map) ? body['data'] : body;
      if (data is Map) {
        return DraftSnapshot.fromProgressJson(Map<String, dynamic>.from(data));
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
