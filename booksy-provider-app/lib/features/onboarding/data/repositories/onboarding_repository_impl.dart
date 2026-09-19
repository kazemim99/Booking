import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/onboarding_data.dart';
import '../../domain/entities/onboarding_draft.dart';
import '../../../home/domain/entities/saved_customer.dart';
import '../../domain/repositories/onboarding_repository.dart';
import '../datasources/onboarding_api_service.dart';
import '../models/onboarding_models.dart';

class OnboardingRepositoryImpl implements OnboardingRepository {
  final OnboardingApiService _api;
  OnboardingRepositoryImpl(this._api);

  @override
  Future<Either<Failure, String>> createDraft(OnboardingData data) async {
    return _guard(() =>
        _api.registerOrganization(RegisterOrganizationRequest(data)));
  }

  @override
  Future<Either<Failure, void>> saveServices(
    String providerId,
    List<ServiceDraft> services,
  ) {
    return _guard(() =>
        _api.saveServices(SaveServicesRequest(providerId, services)));
  }

  @override
  Future<Either<Failure, void>> saveWorkingHours(
    String providerId,
    List<DayHours> hours,
  ) {
    return _guard(() =>
        _api.saveWorkingHours(SaveWorkingHoursRequest(providerId, hours)));
  }

  @override
  Future<Either<Failure, void>> uploadGallery(
    String providerId,
    List<GalleryImageUpload> images,
  ) {
    return _guard(() => _api.uploadGallery(images));
  }

  @override
  Future<Either<Failure, void>> uploadGalleryImage(
    String providerId,
    GalleryImageUpload image, {
    ProgressCallback? onProgress,
    CancelToken? cancelToken,
  }) {
    return _guard(() => _api.uploadGalleryImage(
          image,
          onSendProgress: onProgress,
          cancelToken: cancelToken,
        ));
  }

  @override
  Future<Either<Failure, void>> complete(String providerId) {
    return _guard(() => _api.complete(providerId));
  }

  @override
  Future<Either<Failure, void>> setOwnerProvidesServices(bool providesServices) {
    return _guard(() => _api.setOwnerProvidesServices(providesServices));
  }

  @override
  Future<Either<Failure, OnboardingDraft?>> getDraft() {
    return _guard(() => _api.getDraft());
  }

  @override
  Future<Either<Failure, void>> addCustomer(
      String providerId, CustomerDraft customer) async {
    try {
      await _api.addCustomer(providerId, customer.toJson());
      return const Right(null);
    } on DioException catch (e) {
      // The server says why (e.g. «این شماره قبلاً برای … ثبت شده است»).
      final data = e.response?.data;
      final error = data is Map ? data['error'] : null;
      final reason = error is Map ? error['message'] : null;
      return Left(reason is String && reason.isNotEmpty
          ? ServerFailure(reason)
          : _mapDioError(e));
    }
  }

  @override
  Future<Either<Failure, int>> importCustomers(
      String providerId, List<CustomerDraft> contacts) {
    return _guard(() => _api.importCustomers(
        providerId, contacts.map((c) => c.toJson()).toList()));
  }

  Future<Either<Failure, T>> _guard<T>(Future<T> Function() op) async {
    try {
      return Right(await op());
    } on DioException catch (e) {
      return Left(_mapDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
    }
  }

  Failure _mapDioError(DioException error) {
    if (error.type == DioExceptionType.connectionError ||
        error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.receiveTimeout ||
        error.type == DioExceptionType.sendTimeout) {
      return NetworkFailure(error.message ?? 'خطای شبکه');
    }
    final code = error.response?.statusCode;
    final message = error.message ?? 'خطای سرور';
    switch (code) {
      case 400:
      case 422:
        return ValidationFailure(message);
      case 401:
        return AuthFailure(message);
      case 404:
        return NotFoundFailure(message);
      case 429:
        return RateLimitFailure(message);
      default:
        return ServerFailure(message);
    }
  }
}
