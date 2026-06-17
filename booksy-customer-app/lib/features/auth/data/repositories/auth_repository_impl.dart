import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import 'package:injectable/injectable.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../domain/entities/user.dart';
import '../../domain/repositories/auth_repository.dart';
import '../datasources/auth_api_service.dart';
import '../models/auth_models.dart';

/// Auth Repository Implementation
@LazySingleton(as: AuthRepository)
class AuthRepositoryImpl implements AuthRepository {
  final AuthApiService _authApiService;
  final SecureStorageService _storageService;

  AuthRepositoryImpl(this._authApiService, this._storageService);

  @override
  Future<Either<Failure, String>> sendVerificationCode({
    required String phoneNumber,
    String countryCode = '+98',
  }) async {
    try {
      final request = SendVerificationCodeRequest(
        phoneNumber: phoneNumber,
        countryCode: countryCode,
      );

      final response = await _authApiService.sendVerificationCode(request);

      if (response.success && response.data != null) {
        return Right(response.data!.message);
      } else {
        return Left(ServerFailure(response.message ?? 'خطا در ارسال کد تایید'));
      }
    } on DioException catch (e) {
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, AuthSession>> completeAuthentication({
    required String phoneNumber,
    required String code,
    String? firstName,
    String? lastName,
    String? email,
  }) async {
    try {
      final request = CompleteCustomerAuthRequest(
        phoneNumber: phoneNumber,
        code: code,
        firstName: firstName,
        lastName: lastName,
        email: email,
      );

      final response = await _authApiService.completeCustomerAuth(request);

      if (response.success && response.data != null) {
        final authData = response.data!;

        // Save tokens and user data to secure storage
        await _storageService.saveAuthSession(
          accessToken: authData.accessToken,
          refreshToken: authData.refreshToken,
          userId: authData.userId,
          customerId: authData.customerId,
          phoneNumber: phoneNumber,
        );

        // Convert DTO to Entity
        final authSession = _mapToAuthSession(authData);

        return Right(authSession);
      } else {
        return Left(AuthFailure(response.message ?? 'خطا در تایید کد'));
      }
    } on DioException catch (e) {
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, String>> resendOtp({
    required String phoneNumber,
  }) async {
    try {
      final request = ResendOtpRequest(phoneNumber: phoneNumber);
      final response = await _authApiService.resendOtp(request);

      if (response.success && response.data != null) {
        return Right(response.data!.message);
      } else {
        return Left(ServerFailure(response.message ?? 'خطا در ارسال مجدد کد'));
      }
    } on DioException catch (e) {
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, AuthSession>> refreshToken() async {
    try {
      final refreshToken = await _storageService.getRefreshToken();

      if (refreshToken == null || refreshToken.isEmpty) {
        return const Left(AuthFailure('رفرش توکن یافت نشد'));
      }

      final request = RefreshTokenRequest(refreshToken: refreshToken);
      final response = await _authApiService.refreshToken(request);

      if (response.success && response.data != null) {
        final tokenData = response.data!;

        // Save new tokens
        await _storageService.saveAccessToken(tokenData.accessToken);
        await _storageService.saveRefreshToken(tokenData.refreshToken);

        // Get current session (we only update tokens, not user data)
        final currentSession = await getCurrentSession();

        return currentSession.fold(
          (failure) => Left(failure),
          (session) {
            if (session == null) {
              return const Left(AuthFailure('سشن کاربری یافت نشد'));
            }

            // Return updated session with new tokens
            final updatedSession = AuthSession(
              accessToken: tokenData.accessToken,
              refreshToken: tokenData.refreshToken,
              user: session.user,
              customer: session.customer,
              expiresIn: tokenData.expiresIn,
            );

            return Right(updatedSession);
          },
        );
      } else {
        return Left(AuthFailure(response.message ?? 'خطا در بروزرسانی توکن'));
      }
    } on DioException catch (e) {
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطای نامشخص: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, void>> logout() async {
    try {
      // Call logout API (ignore errors as we clear local session anyway)
      try {
        await _authApiService.logout();
      } catch (_) {
        // Ignore API errors during logout
      }

      // Clear local session
      await _storageService.clearAuthSession();

      return const Right(null);
    } catch (e) {
      return const Left(CacheFailure('خطا در خروج از حساب کاربری'));
    }
  }

  @override
  Future<bool> isLoggedIn() async {
    return await _storageService.isLoggedIn();
  }

  @override
  Future<Either<Failure, AuthSession?>> getCurrentSession() async {
    try {
      final accessToken = await _storageService.getAccessToken();
      final refreshToken = await _storageService.getRefreshToken();
      final userId = await _storageService.getUserId();
      final customerId = await _storageService.getCustomerId();
      final phoneNumber = await _storageService.getPhoneNumber();

      if (accessToken == null ||
          refreshToken == null ||
          userId == null ||
          phoneNumber == null) {
        return const Right(null);
      }

      // Create minimal session from stored data
      // Note: In production, you might want to fetch full user data from API
      final user = User(
        id: userId,
        phoneNumber: phoneNumber,
        createdAt: DateTime.now(), // Placeholder
      );

      final customer = customerId != null
          ? Customer(
              id: customerId,
              userId: userId,
              createdAt: DateTime.now(), // Placeholder
            )
          : null;

      final session = AuthSession(
        accessToken: accessToken,
        refreshToken: refreshToken,
        user: user,
        customer: customer,
        expiresIn: 3600, // Placeholder
      );

      return Right(session);
    } catch (e) {
      return const Left(CacheFailure('خطا در دریافت اطلاعات کاربر'));
    }
  }

  // ==================== Helper Methods ====================

  /// Map DTO to Entity
  AuthSession _mapToAuthSession(CompleteCustomerAuthResponse dto) {
    final user = User(
      id: dto.user.id,
      phoneNumber: dto.user.phoneNumber,
      email: dto.user.email,
      firstName: dto.user.firstName,
      lastName: dto.user.lastName,
      profilePictureUrl: dto.user.profilePictureUrl,
      emailVerified: dto.user.emailVerified,
      phoneVerified: dto.user.phoneVerified,
      createdAt: dto.user.createdAt,
    );

    final customer = dto.customer != null
        ? Customer(
            id: dto.customer!.id,
            userId: dto.customer!.userId,
            preferredLanguage: dto.customer!.preferredLanguage,
            favoriteProviders: dto.customer!.favoriteProviders ?? [],
            bookingCount: dto.customer!.bookingCount,
            createdAt: dto.customer!.createdAt,
          )
        : null;

    return AuthSession(
      accessToken: dto.accessToken,
      refreshToken: dto.refreshToken,
      user: user,
      customer: customer,
      expiresIn: dto.expiresIn,
    );
  }

  /// Handle Dio errors
  Failure _handleDioError(DioException error) {
    if (error.type == DioExceptionType.connectionError ||
        error.type == DioExceptionType.connectionTimeout) {
      return const NetworkFailure('لطفا اتصال اینترنت خود را بررسی کنید');
    }

    if (error.response != null) {
      final statusCode = error.response!.statusCode;
      final message = error.message ?? 'خطای سرور';

      switch (statusCode) {
        case 400:
          return ValidationFailure(message);
        case 401:
          return const AuthFailure('نام کاربری یا رمز عبور اشتباه است');
        case 404:
          return const NotFoundFailure('اطلاعات مورد نظر یافت نشد');
        default:
          return ServerFailure(message);
      }
    }

    return ServerFailure(error.message ?? 'خطای نامشخص');
  }
}
