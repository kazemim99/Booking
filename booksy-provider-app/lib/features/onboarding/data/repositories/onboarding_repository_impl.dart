import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/onboarding_data.dart';
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
  Future<Either<Failure, void>> complete(String providerId) {
    return _guard(() => _api.complete(providerId));
  }

  @override
  Future<Either<Failure, String?>> getDraftProviderId() {
    return _guard(() => _api.getDraftProviderId());
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
