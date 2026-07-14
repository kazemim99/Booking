import 'package:flutter_bloc/flutter_bloc.dart';
import '../../domain/entities/provider_session.dart';
import '../../domain/entities/provider_status.dart';
import '../../domain/repositories/auth_repository.dart';
import '../../domain/usecases/complete_provider_authentication_usecase.dart';
import '../../domain/usecases/send_verification_code_usecase.dart';
import 'auth_event.dart';
import 'auth_state.dart';

/// Orchestrates provider authentication (AUTH_SPECIFICATION.md §6).
///
/// The terminal post-auth state is derived from provider STATUS, not the
/// backend `requiresOnboarding` flag (BUG-1): blocked → onboarding → authed.
class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final SendVerificationCodeUseCase _sendVerificationCode;
  final CompleteProviderAuthenticationUseCase _completeAuthentication;
  final AuthRepository _authRepository;

  AuthBloc(
    this._sendVerificationCode,
    this._completeAuthentication,
    this._authRepository,
  ) : super(const AuthInitial()) {
    on<SendVerificationCodeRequested>(_onSend);
    on<VerifyCodeRequested>(_onVerify);
    on<ResendCodeRequested>(_onResend);
    on<AuthStatusChecked>(_onCheckStatus);
    on<LogoutRequested>(_onLogout);
    on<SessionExpiredSignalled>(_onSessionExpired);
    on<ProviderStatusRefreshRequested>(_onRefreshProviderStatus);
  }

  /// After onboarding completes the server-side provider status has advanced,
  /// but the cached JWT still says "Drafted". Re-fetch and re-resolve so the
  /// router moves the provider onto the dashboard.
  Future<void> _onRefreshProviderStatus(
    ProviderStatusRefreshRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());
    final result = await _authRepository.refreshProviderStatus();
    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (session) => emit(_resolve(session)),
    );
  }

  Future<void> _onSend(
    SendVerificationCodeRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());
    final result = await _sendVerificationCode(phoneNumber: event.phoneNumber);
    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (message) => emit(OtpSent(phoneNumber: event.phoneNumber, message: message)),
    );
  }

  Future<void> _onVerify(
    VerifyCodeRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());
    final result = await _completeAuthentication(
      phoneNumber: event.phoneNumber,
      code: event.code,
      firstName: event.firstName,
      lastName: event.lastName,
      email: event.email,
    );
    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (session) => emit(_resolve(session)),
    );
  }

  Future<void> _onResend(
    ResendCodeRequested event,
    Emitter<AuthState> emit,
  ) async {
    // Resend == fresh send (no dedicated endpoint; §5.8).
    final result = await _sendVerificationCode(phoneNumber: event.phoneNumber);
    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (_) => emit(const OtpResent('کد تأیید مجدداً ارسال شد')),
    );
  }

  Future<void> _onCheckStatus(
    AuthStatusChecked event,
    Emitter<AuthState> emit,
  ) async {
    final loggedIn = await _authRepository.isLoggedIn();
    if (!loggedIn) {
      emit(const Unauthenticated());
      return;
    }
    final result = await _authRepository.getCurrentSession();
    result.fold(
      (_) => emit(const Unauthenticated()),
      (session) => emit(session == null ? const Unauthenticated() : _resolve(session)),
    );
  }

  Future<void> _onLogout(
    LogoutRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());
    final result = await _authRepository.logout();
    result.fold(
      (failure) => emit(AuthError(failure.message)),
      (_) => emit(const LoggedOut()),
    );
  }

  Future<void> _onSessionExpired(
    SessionExpiredSignalled event,
    Emitter<AuthState> emit,
  ) async {
    emit(const Unauthenticated());
  }

  /// Central post-auth routing decision. Order matters:
  /// blocked (suspended/inactive/archived) → onboarding (new/drafted/no
  /// profile) → authenticated.
  AuthState _resolve(ProviderSession session) {
    if (session.isBlocked) {
      return AccountBlocked(
        session,
        session.providerStatus ?? ProviderStatus.suspended,
      );
    }
    if (session.needsOnboarding) {
      return NeedsOnboarding(session);
    }
    return Authenticated(session);
  }
}
