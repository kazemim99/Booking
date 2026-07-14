import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/provider_session.dart';

/// Provider authentication repository contract (domain layer).
abstract class AuthRepository {
  /// Sends an OTP to [phoneNumber] (canonical `09…`). Returns a user-facing
  /// message on success.
  Future<Either<Failure, String>> sendVerificationCode({
    required String phoneNumber,
  });

  /// Verifies [code] and completes provider auth (login or first-time
  /// registration), persisting the session. Returns the [ProviderSession].
  Future<Either<Failure, ProviderSession>> completeProviderAuthentication({
    required String phoneNumber,
    required String code,
    String? firstName,
    String? lastName,
    String? email,
  });

  /// Refreshes the access token using the stored refresh token.
  Future<Either<Failure, ProviderSession>> refreshToken();

  /// Logs out: best-effort server call + local session clear.
  Future<Either<Failure, void>> logout();

  /// Whether a token is present locally.
  Future<bool> isLoggedIn();

  /// Restores the session from secure storage (no network); null if none.
  Future<Either<Failure, ProviderSession?>> getCurrentSession();
}
