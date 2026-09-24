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
              // The new token carries the current name (e.g. right after a rename), so the header follows it.
              user: _userFrom(data.accessToken, session.user),
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
        // The name lives in the token. A restored session used to carry none, and the header then fell back to
        // the phone number (production QA 2026-09-23).
        user: _userFrom(
          accessToken,
          ProviderUser(id: userId, phoneNumber: phoneNumber, fullName: ''),
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
  Future<Either<Failure, void>> updateMyName({
    required String firstName,
    required String lastName,
  }) async {
    final sessionOr = await getCurrentSession();
    return sessionOr.fold(Left.new, (session) async {
      if (session == null) {
        return const Left<Failure, void>(AuthFailure('نشست معتبر یافت نشد'));
      }
      try {
        await _api.updateProfileName(session.user.id, firstName, lastName);
        // The name lives in the token too: re-mint it so this device shows the
        // new name instead of the placeholder until the next sign-in.
        await refreshToken();
        return const Right<Failure, void>(null);
      } on DioException catch (e) {
        final data = e.response?.data;
        final error = data is Map ? data['error'] : null;
        final reason = error is Map ? error['message'] : null;
        return Left<Failure, void>(ServerFailure(
            reason is String && reason.isNotEmpty ? reason : 'ذخیره نام ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, ProviderSession>> refreshProviderStatus() async {
    try {
      // Re-mint the token first: it carries the person's name, and onboarding has just saved one on the server.
      // Kept as-is, the sign-in token's placeholder made the app ask «نام شما ثبت نشده» for the name onboarding had
      // taken a minute earlier (QA 2026-09-24). Best-effort: a failed refresh leaves the session as it was.
      await refreshToken();

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

  /// [user] with the name [accessToken] carries; unchanged when the token has none.
  static ProviderUser _userFrom(String accessToken, ProviderUser user) {
    final claims = JwtDecoder.decode(accessToken);
    if (claims == null ||
        (claims.firstName == null && claims.lastName == null && claims.fullName == null)) {
      return user;
    }
    return ProviderUser(
      id: user.id,
      phoneNumber: user.phoneNumber,
      email: user.email,
      firstName: claims.firstName,
      lastName: claims.lastName,
      fullName: claims.fullName ??
          [claims.firstName, claims.lastName].whereType<String>().join(' '),
    );
  }

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
