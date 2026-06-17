import 'package:equatable/equatable.dart';

/// Auth Events
abstract class AuthEvent extends Equatable {
  const AuthEvent();

  @override
  List<Object?> get props => [];
}

/// Send verification code event
class SendVerificationCodeEvent extends AuthEvent {
  final String phoneNumber;
  final String countryCode;

  const SendVerificationCodeEvent({
    required this.phoneNumber,
    this.countryCode = '+98',
  });

  @override
  List<Object?> get props => [phoneNumber, countryCode];
}

/// Verify OTP code and complete authentication
class VerifyCodeEvent extends AuthEvent {
  final String phoneNumber;
  final String code;
  final String? firstName;
  final String? lastName;
  final String? email;

  const VerifyCodeEvent({
    required this.phoneNumber,
    required this.code,
    this.firstName,
    this.lastName,
    this.email,
  });

  @override
  List<Object?> get props => [phoneNumber, code, firstName, lastName, email];
}

/// Resend OTP code
class ResendOtpEvent extends AuthEvent {
  final String phoneNumber;

  const ResendOtpEvent(this.phoneNumber);

  @override
  List<Object?> get props => [phoneNumber];
}

/// Check authentication status
class CheckAuthStatusEvent extends AuthEvent {
  const CheckAuthStatusEvent();
}

/// Logout event
class LogoutEvent extends AuthEvent {
  const LogoutEvent();
}

/// Refresh token event
class RefreshTokenEvent extends AuthEvent {
  const RefreshTokenEvent();
}
