import 'package:equatable/equatable.dart';

/// Base Failure class
abstract class Failure extends Equatable {
  final String message;

  const Failure(this.message);

  @override
  List<Object> get props => [message];
}

/// Server Failure (API errors)
class ServerFailure extends Failure {
  const ServerFailure(super.message);
}

/// Network Failure (connection errors)
class NetworkFailure extends Failure {
  const NetworkFailure(super.message);
}

/// Cache Failure (local storage errors)
class CacheFailure extends Failure {
  const CacheFailure(super.message);
}

/// Validation Failure (input validation errors)
class ValidationFailure extends Failure {
  const ValidationFailure(super.message);
}

/// Authentication Failure (auth-specific errors)
class AuthFailure extends Failure {
  const AuthFailure(super.message);
}

/// Not Found Failure
class NotFoundFailure extends Failure {
  const NotFoundFailure(super.message);
}

/// Unauthorized Failure
class UnauthorizedFailure extends Failure {
  const UnauthorizedFailure(super.message);
}
