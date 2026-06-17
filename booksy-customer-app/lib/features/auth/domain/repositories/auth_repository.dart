import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/user.dart';

/// Auth Repository Interface (Domain Layer)
/// Defines the contract for authentication operations
abstract class AuthRepository {
  /// Send OTP verification code to phone number
  Future<Either<Failure, String>> sendVerificationCode({
    required String phoneNumber,
    String countryCode = '+98',
  });

  /// Verify OTP and complete authentication (login/register)
  Future<Either<Failure, AuthSession>> completeAuthentication({
    required String phoneNumber,
    required String code,
    String? firstName,
    String? lastName,
    String? email,
  });

  /// Resend OTP code
  Future<Either<Failure, String>> resendOtp({
    required String phoneNumber,
  });

  /// Refresh access token
  Future<Either<Failure, AuthSession>> refreshToken();

  /// Logout user
  Future<Either<Failure, void>> logout();

  /// Check if user is logged in
  Future<bool> isLoggedIn();

  /// Get current auth session from storage
  Future<Either<Failure, AuthSession?>> getCurrentSession();
}
