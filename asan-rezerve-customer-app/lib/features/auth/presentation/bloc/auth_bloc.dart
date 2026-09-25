import 'dart:async';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:injectable/injectable.dart';

import '../../../../core/push/push_registration.dart';
import '../../domain/entities/user.dart';
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

  /// Push registration rides the auth flow: register once a session exists, revoke before it is cleared.
  /// Optional with a no-op default so the generated DI (injection.config.dart) still compiles unchanged;
  /// `injection.dart` re-registers the bloc with the real one after `getIt.init()`.
  final PushLifecycle _push;

  AuthBloc(
    this._sendVerificationCodeUseCase,
    this._completeAuthenticationUseCase,
    this._authRepository, {
    // A named parameter cannot be a private initializing formal (`this._push`), whatever the lint suggests.
    // ignore: prefer_initializing_formals
    PushLifecycle push = const NoPush(),
  })  : _push = push,
        super(const AuthInitial()) {
    on<SendVerificationCodeEvent>(_onSendVerificationCode);
    on<VerifyCodeEvent>(_onVerifyCode);
    on<ResendOtpEvent>(_onResendOtp);
    on<CheckAuthStatusEvent>(_onCheckAuthStatus);
    on<LogoutEvent>(_onLogout);
    on<RefreshTokenEvent>(_onRefreshToken);
    on<UserNameChangedEvent>(_onUserNameChanged);
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
      (session) => _signedIn(session, emit),
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

    // AppRouter's AuthNotifier only leaves the splash route on a TERMINAL state
    // (Authenticated / Unauthenticated / LoggedOut) — see app_router.dart. This handler
    // is the only thing that runs at cold start, so if it throws before emitting one,
    // the router is stuck on splash forever with no way to recover.
    //
    // That is exactly what happened: flutter_secure_storage on web decrypts each value
    // with a key derived per browser origin/session, so a token written by an earlier
    // build or session can fail to decrypt after a rebuild and throw here instead of
    // returning null. A cold-start auth check must never be allowed to leave the app
    // in an unresolved state — falling back to signed-out is always safe and always
    // recoverable (the customer can just sign in again), whereas an infinite spinner
    // is not.
    try {
      final isLoggedIn = await _authRepository.isLoggedIn();

      if (isLoggedIn) {
        final result = await _authRepository.getCurrentSession();
        result.fold(
          (failure) => emit(const Unauthenticated()),
          (session) {
            if (session != null) {
              _signedIn(session, emit);
            } else {
              emit(const Unauthenticated());
            }
          },
        );
      } else {
        emit(const Unauthenticated());
      }
    } catch (_) {
      emit(const Unauthenticated());
    }
  }

  /// Handle logout event
  Future<void> _onLogout(
    LogoutEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());

    // Before the session goes: revoking the device is an authenticated call, and afterwards it would be 401.
    await _push.onSigningOut();

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
      (session) => _signedIn(session, emit),
    );
  }

  /// The signed-in person gave their name (the name page after sign-up, the profile, or the booking confirm step).
  /// The session carries it from now on, and it is kept with the stored session, so nothing asks for it again —
  /// not the next booking, and not after a restart. Nobody signed in: nothing to rename.
  Future<void> _onUserNameChanged(
    UserNameChangedEvent event,
    Emitter<AuthState> emit,
  ) async {
    final current = state;
    if (current is! Authenticated) return;
    final session = current.session;
    emit(Authenticated(AuthSession(
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      user: session.user.withName(firstName: event.firstName, lastName: event.lastName),
      customer: session.customer,
      expiresIn: session.expiresIn,
    )));
    await _authRepository.rememberUserName(firstName: event.firstName, lastName: event.lastName);
  }

  /// A session exists on this device. Push registration is started but not awaited: it must never delay, or
  /// fail, signing in.
  void _signedIn(AuthSession session, Emitter<AuthState> emit) {
    emit(Authenticated(session));
    unawaited(_push.onSignedIn());
  }
}
