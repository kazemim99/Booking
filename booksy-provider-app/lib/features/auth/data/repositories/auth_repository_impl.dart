import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../../../core/utils/jwt_decoder.dart';
import '../../domain/entities/provider_session.dart';
import '../../domain/entities/provider_status.dart';
import '../../domain/repositories/auth_repository.dart';
import '../datasources/auth_api_service.dart';
import '../models/auth_models.dart';

class AuthRepositoryImpl implements AuthRepository {
  final AuthApiService _api;
  final SecureStorageService _storage;

  AuthRepositoryImpl(this._api, this._storage);

  @override
  Future<Either<Failure, String>> sendVerificationCode({
    required String phoneNumber,
  }) async {
    try {
      final response = await _api.sendVerificationCode(
        SendVerificationCodeRequest(phoneNumber: phoneNumber),
      );
      if (response.success && response.data != null) {
        return Right(response.data!.message);
      }
      return Left(ServerFailure(response.message ?? 'خطا در ارسال کد تأیید'));
    } on DioException catch (e) {
      return Left(_mapDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
    }
  }

  @override
  Future<Either<Failure, ProviderSession>> completeProviderAuthentication({
    required String phoneNumber,
    required String code,
    String? firstName,
    String? lastName,
    String? email,
  }) async {
    try {
      final response = await _api.completeProviderAuth(
        CompleteProviderAuthRequest(
          phoneNumber: phoneNumber,
          code: code,
          firstName: firstName,
          lastName: lastName,
          email: email,
        ),
      );

      if (response.success && response.data != null) {
        final session = response.data!.toSession();
        await _persist(session, phoneNumber);
        return Right(session);
      }
      return Left(AuthFailure(response.message ?? 'خطا در تأیید کد'));
    } on DioException catch (e) {
      return Left(_mapDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
    }
  }

  @override
  Future<Either<Failure, ProviderSession>> refreshToken() async {
    try {
      final refresh = await _storage.getRefreshToken();
      if (refresh == null || refresh.isEmpty) {
        return const Left(AuthFailure('توکن تازه‌سازی یافت نشد'));
      }
      final response = await _api.refreshToken(
        RefreshTokenRequest(refreshToken: refresh),
      );
      if (response.success && response.data != null) {
        final data = response.data!;
        await _storage.saveAccessToken(data.accessToken);
        if (data.refreshToken.isNotEmpty) {
          await _storage.saveRefreshToken(data.refreshToken);
        }
        final restored = await getCurrentSession();
        return restored.fold(
          Left.new,
          (session) => session == null
              ? const Left(AuthFailure('نشست کاربری یافت نشد'))
              : Right(session.copyWith(
                  accessToken: data.accessToken,
                  refreshToken: data.refreshToken,
                  expiresIn: data.expiresIn,
                )),
        );
      }
      return Left(AuthFailure(response.message ?? 'خطا در بروزرسانی توکن'));
    } on DioException catch (e) {
      return Left(_mapDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
    }
  }

  @override
  Future<Either<Failure, void>> logout() async {
    try {
      try {
        await _api.logout();
      } catch (_) {
        // best-effort: ignore server errors, always clear locally
      }
      await _storage.clearSession();
      return const Right(null);
    } catch (e) {
      return const Left(CacheFailure('خطا در خروج از حساب'));
    }
  }

  @override
  Future<bool> isLoggedIn() => _storage.isLoggedIn();

  @override
  Future<Either<Failure, ProviderSession?>> getCurrentSession() async {
    try {
      final accessToken = await _storage.getAccessToken();
      final refreshToken = await _storage.getRefreshToken();
      final userId = await _storage.getUserId();
      final phoneNumber = await _storage.getPhoneNumber();

      if (accessToken == null ||
          refreshToken == null ||
          userId == null ||
          phoneNumber == null) {
        return const Right(null);
      }

      // Prefer claims from the JWT; fall back to persisted status.
      final claims = JwtDecoder.decode(accessToken);
      final storedStatus = await _storage.getProviderStatus();
      final storedProviderId = await _storage.getProviderId();

      final status = ProviderStatus.tryParse(
        claims?.providerStatus ?? storedStatus,
      );
      final providerId = claims?.providerId ?? storedProviderId;

      final session = ProviderSession(
        accessToken: accessToken,
        refreshToken: refreshToken,
        expiresIn: 0,
        user: ProviderUser(
          id: userId,
          phoneNumber: phoneNumber,
          fullName: '',
        ),
        providerId: providerId,
        providerStatus: status,
        // Restored session: not a fresh registration.
        isNewProvider: false,
        requiresOnboarding: status?.needsOnboarding ?? (providerId == null),
      );
      return Right(session);
    } catch (e) {
      return const Left(CacheFailure('خطا در بازیابی نشست'));
    }
  }

  // ==================== Helpers ====================

  Future<void> _persist(ProviderSession session, String phoneNumber) async {
    await _storage.saveSession(
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      userId: session.user.id,
      providerId: session.providerId,
      providerStatus: session.providerStatus?.wireName,
      phoneNumber: phoneNumber,
    );
  }

  Failure _mapDioError(DioException error) {
    if (error.type == DioExceptionType.connectionError ||
        error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.receiveTimeout ||
        error.type == DioExceptionType.sendTimeout) {
      return NetworkFailure(error.message ?? 'خطای شبکه');
    }

    final statusCode = error.response?.statusCode;
    final message = error.message ?? 'خطای سرور';

    switch (statusCode) {
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
