import 'package:equatable/equatable.dart';
import '../../domain/entities/user.dart';

/// Auth States
abstract class AuthState extends Equatable {
  const AuthState();

  @override
  List<Object?> get props => [];
}

/// Initial state
class AuthInitial extends AuthState {
  const AuthInitial();
}

/// Loading state
class AuthLoading extends AuthState {
  const AuthLoading();
}

/// OTP sent successfully
class OtpSentSuccess extends AuthState {
  final String message;
  final String phoneNumber;

  const OtpSentSuccess({
    required this.message,
    required this.phoneNumber,
  });

  @override
  List<Object?> get props => [message, phoneNumber];
}

/// OTP resent successfully
class OtpResentSuccess extends AuthState {
  final String message;

  const OtpResentSuccess(this.message);

  @override
  List<Object?> get props => [message];
}

/// Authenticated (logged in)
class Authenticated extends AuthState {
  final AuthSession session;

  const Authenticated(this.session);

  @override
  List<Object?> get props => [session];
}

/// Unauthenticated (not logged in)
class Unauthenticated extends AuthState {
  const Unauthenticated();
}

/// Auth error
class AuthError extends AuthState {
  final String message;

  const AuthError(this.message);

  @override
  List<Object?> get props => [message];
}

/// Logged out
class LoggedOut extends AuthState {
  const LoggedOut();
}
