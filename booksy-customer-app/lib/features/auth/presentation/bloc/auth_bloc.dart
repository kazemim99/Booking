import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:injectable/injectable.dart';
import '../../domain/repositories/auth_repository.dart';
import '../../domain/usecases/complete_authentication_usecase.dart';
import '../../domain/usecases/send_verification_code_usecase.dart';
import 'auth_event.dart';
import 'auth_state.dart';

/// Auth BLoC
/// Manages authentication state and handles auth-related events
@injectable
class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final SendVerificationCodeUseCase _sendVerificationCodeUseCase;
  final CompleteAuthenticationUseCase _completeAuthenticationUseCase;
  final AuthRepository _authRepository;

  AuthBloc(
    this._sendVerificationCodeUseCase,
    this._completeAuthenticationUseCase,
    this._authRepository,
  ) : super(const AuthInitial()) {
    on<SendVerificationCodeEvent>(_onSendVerificationCode);
    on<VerifyCodeEvent>(_onVerifyCode);
    on<ResendOtpEvent>(_onResendOtp);
    on<CheckAuthStatusEvent>(_onCheckAuthStatus);
    on<LogoutEvent>(_onLogout);
    on<RefreshTokenEvent>(_onRefreshToken);
  }

  /// Handle send verification code event
  Future<void> _onSendVerificationCode(
    SendVerificationCodeEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());

    final result = await _sendVerificationCodeUseCase(
      phoneNumber: event.phoneNumber,
      countryCode: event.countryCode,
    );

    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (message) => emit(OtpSentSuccess(
        message: message,
        phoneNumber: event.phoneNumber,
      )),
    );
  }

  /// Handle verify code event
  Future<void> _onVerifyCode(
    VerifyCodeEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());

    final result = await _completeAuthenticationUseCase(
      phoneNumber: event.phoneNumber,
      code: event.code,
      firstName: event.firstName,
      lastName: event.lastName,
      email: event.email,
    );

    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (session) => emit(Authenticated(session)),
    );
  }

  /// Handle resend OTP event
  Future<void> _onResendOtp(
    ResendOtpEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());

    final result = await _authRepository.resendOtp(
      phoneNumber: event.phoneNumber,
    );

    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (message) => emit(OtpResentSuccess(message)),
    );
  }

  /// Handle check auth status event
  Future<void> _onCheckAuthStatus(
    CheckAuthStatusEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());

    final isLoggedIn = await _authRepository.isLoggedIn();

    if (isLoggedIn) {
      final result = await _authRepository.getCurrentSession();
      result.fold(
        (failure) => emit(const Unauthenticated()),
        (session) {
          if (session != null) {
            emit(Authenticated(session));
          } else {
            emit(const Unauthenticated());
          }
        },
      );
    } else {
      emit(const Unauthenticated());
    }
  }

  /// Handle logout event
  Future<void> _onLogout(
    LogoutEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());

    final result = await _authRepository.logout();

    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (_) => emit(const LoggedOut()),
    );
  }

  /// Handle refresh token event
  Future<void> _onRefreshToken(
    RefreshTokenEvent event,
    Emitter<AuthState> emit,
  ) async {
    final result = await _authRepository.refreshToken();

    result.fold(
      (failure) {
        // If refresh fails, logout the user
        emit(const Unauthenticated());
      },
      (session) => emit(Authenticated(session)),
    );
  }
}
