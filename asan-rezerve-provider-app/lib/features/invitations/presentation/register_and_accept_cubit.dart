import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../domain/invitation_repository.dart';
import '../domain/invitation_summary.dart';

enum RegisterAcceptPhase {
  loading,
  notFound,
  loadError,
  enteringInfo,
  sendingOtp,
  verifyingOtp,
  registering,
  registered,
}

class RegisterAndAcceptState extends Equatable {
  final RegisterAcceptPhase phase;
  final InvitationSummary? summary;
  final String? maskedPhone;
  final String? error;
  final String? otpError;

  const RegisterAndAcceptState({
    this.phase = RegisterAcceptPhase.loading,
    this.summary,
    this.maskedPhone,
    this.error,
    this.otpError,
  });

  RegisterAndAcceptState copyWith({
    RegisterAcceptPhase? phase,
    InvitationSummary? summary,
    String? maskedPhone,
    String? error,
    String? otpError,
  }) {
    return RegisterAndAcceptState(
      phase: phase ?? this.phase,
      summary: summary ?? this.summary,
      maskedPhone: maskedPhone ?? this.maskedPhone,
      error: error,
      otpError: otpError,
    );
  }

  @override
  List<Object?> get props => [phase, summary, maskedPhone, error, otpError];
}

/// Drives the new-user register-and-accept screen: load the public summary,
/// collect a name, send an OTP to the invitation's own (masked-to-us) phone,
/// then verify it to create the account and accept in one call.
///
/// The result carries no session token (RegisterAndAcceptInvitationResult has
/// none — the handler creates the account and membership but does not mint a
/// JWT), so after success the caller is directed to the standard sign-in
/// screen rather than straight to the dashboard. They know their own phone
/// number even though this flow never learns it, so the normal OTP login
/// works from there.
class RegisterAndAcceptCubit extends Cubit<RegisterAndAcceptState> {
  final InvitationRepository _repository;
  final String invitationId;

  RegisterAndAcceptCubit(this._repository, this.invitationId)
      : super(const RegisterAndAcceptState());

  Future<void> load() async {
    emit(const RegisterAndAcceptState(phase: RegisterAcceptPhase.loading));
    final result = await _repository.fetchSummary(invitationId);
    if (isClosed) return;
    result.fold(
      (f) => emit(RegisterAndAcceptState(
          phase: RegisterAcceptPhase.loadError, error: f.message)),
      (summary) => summary == null
          ? emit(const RegisterAndAcceptState(phase: RegisterAcceptPhase.notFound))
          : emit(RegisterAndAcceptState(
              phase: RegisterAcceptPhase.enteringInfo, summary: summary)),
    );
  }

  Future<void> sendOtp() async {
    emit(state.copyWith(phase: RegisterAcceptPhase.sendingOtp, error: null));
    final result = await _repository.sendOtp(invitationId);
    if (isClosed) return;
    result.fold(
      (f) => emit(state.copyWith(
          phase: RegisterAcceptPhase.enteringInfo, error: f.message)),
      (maskedPhone) => emit(state.copyWith(
          phase: RegisterAcceptPhase.verifyingOtp, maskedPhone: maskedPhone)),
    );
  }

  Future<void> register({
    required String firstName,
    required String lastName,
    String? email,
    required String otpCode,
  }) async {
    emit(state.copyWith(phase: RegisterAcceptPhase.registering, otpError: null));
    final result = await _repository.registerAndAccept(
      invitationId,
      firstName: firstName,
      lastName: lastName,
      email: email,
      otpCode: otpCode,
    );
    if (isClosed) return;
    result.fold(
      (f) => emit(state.copyWith(
          phase: RegisterAcceptPhase.verifyingOtp, otpError: f.message)),
      (_) => emit(state.copyWith(phase: RegisterAcceptPhase.registered)),
    );
  }

  /// Back from the OTP step to editing the name (e.g. wrong info entirely).
  void backToInfo() =>
      emit(state.copyWith(phase: RegisterAcceptPhase.enteringInfo, otpError: null));
}
