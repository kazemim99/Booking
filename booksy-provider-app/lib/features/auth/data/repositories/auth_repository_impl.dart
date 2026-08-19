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

  /// In-memory session from the last successful auth/refresh. Preferred over
  /// re-reading secure storage so consumers (e.g. the Home) get a consistent
  /// session within a running session regardless of the platform's storage
  /// round-trip behaviour (flutter_secure_storage is unreliable on web).
  /// Storage remains the source of truth for cold-start restore.
  ProviderSession? _cachedSession;

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
        _cachedSession = session;
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
          (session) {
            if (session == null) {
              return const Left(AuthFailure('نشست کاربری یافت نشد'));
            }
            final refreshed = session.copyWith(
              accessToken: data.accessToken,
              refreshToken: data.refreshToken,
              expiresIn: data.expiresIn,
            );
            _cachedSession = refreshed;
            return Right(refreshed);
          },
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
      _cachedSession = null;
      return const Right(null);
    } catch (e) {
      return const Left(CacheFailure('خطا در خروج از حساب'));
    }
  }

  @override
  Future<bool> isLoggedIn() => _storage.isLoggedIn();

  @override
  Future<Either<Failure, ProviderSession>> switchActiveOrganization({
    required String providerId,
  }) async {
    try {
      final current = _cachedSession;
      // Persist first so a restart lands in the chosen salon, then drop the
      // cache so the next read re-derives the session from storage.
      await _storage.saveProviderState(providerId: providerId, providerStatus: null);
      _cachedSession = null;

      // Re-derive status for the newly active organization from the server.
      final refreshed = await refreshProviderStatus();
      return refreshed.fold(
        (failure) {
          // Keep the switch (it is persisted) but report the status failure so
          // the caller can decide whether to retry.
          _cachedSession = current;
          return Left(failure);
        },
        Right.new,
      );
    } catch (e) {
      return Left(ServerFailure('تغییر سالن ناموفق بود: $e'));
    }
  }

  @override
  Future<Either<Failure, ProviderSession?>> getCurrentSession() async {
    // Prefer the live in-memory session from the last auth/refresh; storage
    // is only the cold-start restore path.
    if (_cachedSession != null) return Right(_cachedSession);
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

      // Prefer the PERSISTED status over the JWT claim: the access token is
      // long-lived (24h) and its provider_status goes stale the moment
      // onboarding completes. Storage is written at login and refreshed by
      // refreshProviderStatus(), so it is always at least as fresh as the JWT.
      final claims = JwtDecoder.decode(accessToken);
      final storedStatus = await _storage.getProviderStatus();
      final storedProviderId = await _storage.getProviderId();

      final status = ProviderStatus.tryParse(
        storedStatus ?? claims?.providerStatus,
      );
      final providerId = storedProviderId ?? claims?.providerId;

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
      _cachedSession = session;
      return Right(session);
    } catch (e) {
      return const Left(CacheFailure('خطا در بازیابی نشست'));
    }
  }

  @override
  Future<Either<Failure, ProviderSession>> refreshProviderStatus() async {
    try {
      final result = await _api.getCurrentProviderStatus();

      // Persist the server-authoritative status so it survives restarts and
      // wins over the stale value cached in the JWT.
      await _storage.saveProviderState(
        providerId: result?.providerId,
        providerStatus: result?.status,
      );

      final restored = await getCurrentSession();
      return restored.fold(
        Left.new,
        (session) {
          if (session == null) {
            return const Left(AuthFailure('نشست کاربری یافت نشد'));
          }
          final updated = ProviderSession(
            accessToken: session.accessToken,
            refreshToken: session.refreshToken,
            expiresIn: session.expiresIn,
            user: session.user,
            // Keep the known providerId if the status endpoint omitted one.
            providerId: result?.providerId ?? session.providerId,
            providerStatus: ProviderStatus.tryParse(result?.status) ??
                session.providerStatus,
            isNewProvider: false,
            requiresOnboarding: false,
          );
          _cachedSession = updated;
          return Right(updated);
        },
      );
    } on DioException catch (e) {
      return Left(_mapDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: $e'));
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
