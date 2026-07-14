import 'package:equatable/equatable.dart';

abstract class AuthEvent extends Equatable {
  const AuthEvent();
  @override
  List<Object?> get props => [];
}

/// Request an OTP for [phoneNumber] (canonical `09…`).
class SendVerificationCodeRequested extends AuthEvent {
  final String phoneNumber;
  const SendVerificationCodeRequested(this.phoneNumber);
  @override
  List<Object?> get props => [phoneNumber];
}

/// Verify the OTP and complete provider auth.
class VerifyCodeRequested extends AuthEvent {
  final String phoneNumber;
  final String code;
  final String? firstName;
  final String? lastName;
  final String? email;

  const VerifyCodeRequested({
    required this.phoneNumber,
    required this.code,
    this.firstName,
    this.lastName,
    this.email,
  });

  @override
  List<Object?> get props => [phoneNumber, code, firstName, lastName, email];
}

/// Resend the OTP (a fresh send — no dedicated resend endpoint; §5.8).
class ResendCodeRequested extends AuthEvent {
  final String phoneNumber;
  const ResendCodeRequested(this.phoneNumber);
  @override
  List<Object?> get props => [phoneNumber];
}

/// Restore any persisted session at startup (auto-login).
class AuthStatusChecked extends AuthEvent {
  const AuthStatusChecked();
}

/// Log out and clear the session.
class LogoutRequested extends AuthEvent {
  const LogoutRequested();
}

/// External signal that the session expired (refresh failed).
class SessionExpiredSignalled extends AuthEvent {
  const SessionExpiredSignalled();
}

/// Re-fetch provider status from the server and re-resolve routing.
/// Dispatched after onboarding completes (the cached JWT status is stale).
class ProviderStatusRefreshRequested extends AuthEvent {
  const ProviderStatusRefreshRequested();
}
