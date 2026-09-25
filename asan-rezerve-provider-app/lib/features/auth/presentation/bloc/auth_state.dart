import 'package:equatable/equatable.dart';
import '../../domain/entities/provider_session.dart';
import '../../domain/entities/provider_status.dart';

/// Authentication states (AUTH_SPECIFICATION.md §6).
abstract class AuthState extends Equatable {
  const AuthState();
  @override
  List<Object?> get props => [];
}

/// Initial / not yet resolved.
class AuthInitial extends AuthState {
  const AuthInitial();
}

/// A request is in flight (send / verify / logout / refresh).
class AuthLoading extends AuthState {
  const AuthLoading();
}

/// OTP sent; move to the verification screen.
class OtpSent extends AuthState {
  final String phoneNumber;
  final String message;
  const OtpSent({required this.phoneNumber, required this.message});
  @override
  List<Object?> get props => [phoneNumber, message];
}

/// OTP resent successfully.
class OtpResent extends AuthState {
  final String message;
  const OtpResent(this.message);
  @override
  List<Object?> get props => [message];
}

/// Authenticated and cleared for the dashboard.
class Authenticated extends AuthState {
  final ProviderSession session;
  const Authenticated(this.session);
  @override
  List<Object?> get props => [session];
}

/// Authenticated but must complete business onboarding first.
class NeedsOnboarding extends AuthState {
  final ProviderSession session;
  const NeedsOnboarding(this.session);
  @override
  List<Object?> get props => [session];
}

/// Authenticated but the provider account is blocked
/// (suspended/inactive/archived — E-14 / D-1).
class AccountBlocked extends AuthState {
  final ProviderSession session;
  final ProviderStatus status;
  const AccountBlocked(this.session, this.status);
  @override
  List<Object?> get props => [session, status];
}

/// Not authenticated.
class Unauthenticated extends AuthState {
  const Unauthenticated();
}

/// Recoverable error (validation / wrong code / network / server).
class AuthError extends AuthState {
  final String message;
  const AuthError(this.message);
  @override
  List<Object?> get props => [message];
}

/// Logged out (terminal; router treats like Unauthenticated).
class LoggedOut extends AuthState {
  const LoggedOut();
}
