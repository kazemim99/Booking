import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/errors/failures.dart';
import '../domain/invitation_repository.dart';
import '../domain/invitation_summary.dart';

enum AcceptPhase { loading, ready, accepting, accepted, notFound, error }

class AcceptInvitationState extends Equatable {
  final AcceptPhase phase;
  final InvitationSummary? summary;
  final String? error;

  /// The accept call returned 401 — the invitee has an account but isn't logged
  /// in on this device. The UI offers a login CTA instead of an error.
  final bool needsLogin;

  const AcceptInvitationState({
    this.phase = AcceptPhase.loading,
    this.summary,
    this.error,
    this.needsLogin = false,
  });

  AcceptInvitationState copyWith({
    AcceptPhase? phase,
    InvitationSummary? summary,
    String? error,
    bool? needsLogin,
  }) {
    return AcceptInvitationState(
      phase: phase ?? this.phase,
      summary: summary ?? this.summary,
      error: error,
      needsLogin: needsLogin ?? this.needsLogin,
    );
  }

  @override
  List<Object?> get props => [phase, summary, error, needsLogin];
}

/// Drives the accept-invitation screen for an already-registered person: load the
/// public summary, then accept (reusing the existing account). A new (account-less)
/// invitee is routed to registration separately.
class AcceptInvitationCubit extends Cubit<AcceptInvitationState> {
  final InvitationRepository _repository;
  final String invitationId;

  AcceptInvitationCubit(this._repository, this.invitationId)
      : super(const AcceptInvitationState());

  Future<void> load() async {
    emit(const AcceptInvitationState(phase: AcceptPhase.loading));
    final result = await _repository.fetchSummary(invitationId);
    if (isClosed) return;
    result.fold(
      (f) => emit(AcceptInvitationState(phase: AcceptPhase.error, error: f.message)),
      (summary) => summary == null
          ? emit(const AcceptInvitationState(phase: AcceptPhase.notFound))
          : emit(AcceptInvitationState(phase: AcceptPhase.ready, summary: summary)),
    );
  }

  Future<void> accept() async {
    if (state.summary == null) return;
    emit(state.copyWith(phase: AcceptPhase.accepting, needsLogin: false));
    final result = await _repository.accept(invitationId);
    if (isClosed) return;
    result.fold(
      (f) => f is AuthFailure
          // Needs to log in first; keep the summary on screen.
          ? emit(state.copyWith(phase: AcceptPhase.ready, needsLogin: true))
          : emit(state.copyWith(phase: AcceptPhase.ready, error: f.message)),
      (_) => emit(state.copyWith(phase: AcceptPhase.accepted)),
    );
  }
}
