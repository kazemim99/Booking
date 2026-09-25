import 'package:equatable/equatable.dart';

/// Base failure type for the domain layer (mirrors the customer app).
abstract class Failure extends Equatable {
  final String message;
  const Failure(this.message);

  @override
  List<Object?> get props => [message];
}

/// API/server errors (5xx, unexpected).
class ServerFailure extends Failure {
  const ServerFailure(super.message);
}

/// Connectivity / offline errors.
class NetworkFailure extends Failure {
  const NetworkFailure(super.message);
}

/// Local storage errors.
class CacheFailure extends Failure {
  const CacheFailure(super.message);
}

/// Input validation errors (client-side or 400/422).
class ValidationFailure extends Failure {
  const ValidationFailure(super.message);
}

/// Authentication failures (wrong/expired code, 401).
class AuthFailure extends Failure {
  const AuthFailure(super.message);
}

/// Rate-limit / too-many-requests (429 or app-level throttle).
class RateLimitFailure extends Failure {
  const RateLimitFailure(super.message);
}

/// Verification blocked after too many attempts (includes lockout time text).
class BlockedFailure extends Failure {
  const BlockedFailure(super.message);
}

/// Not found (404).
class NotFoundFailure extends Failure {
  const NotFoundFailure(super.message);
}
