import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/constants/app_strings.dart';
import '../../domain/entities/booking_summary.dart';
import '../../domain/repositories/bookings_repository.dart';

// ---------- Events ----------

abstract class AppointmentsEvent extends Equatable {
  const AppointmentsEvent();

  @override
  List<Object?> get props => [];
}

class AppointmentsRequested extends AppointmentsEvent {
  const AppointmentsRequested();
}

/// Reload without the skeleton — the list stays on screen while it runs.
/// Used on return from a booking's detail, where it may have been cancelled
/// or rescheduled.
class AppointmentsRefreshed extends AppointmentsEvent {
  const AppointmentsRefreshed();
}

/// Cancel with confirmation already given. Applies optimistically and
/// rolls back on failure.
class AppointmentCancelled extends AppointmentsEvent {
  final BookingSummary booking;
  final String reason;

  const AppointmentCancelled(this.booking, {this.reason = ''});

  @override
  List<Object?> get props => [booking, reason];
}

/// A reschedule succeeded elsewhere (slot picker); update the card in place.
class AppointmentRescheduled extends AppointmentsEvent {
  final String bookingId;
  final DateTime newStartTime;

  const AppointmentRescheduled(this.bookingId, this.newStartTime);

  @override
  List<Object?> get props => [bookingId, newStartTime];
}

// ---------- State ----------

enum AppointmentsStatus { loading, loaded, empty, error }

/// One-shot feedback for the UI (snackbars).
enum AppointmentsNotice { none, cancelSuccess, cancelFailure }

class AppointmentsState extends Equatable {
  final AppointmentsStatus status;
  final List<BookingSummary> upcoming;
  final List<BookingSummary> past;
  final String? errorMessage;
  final AppointmentsNotice notice;

  const AppointmentsState({
    this.status = AppointmentsStatus.loading,
    this.upcoming = const [],
    this.past = const [],
    this.errorMessage,
    this.notice = AppointmentsNotice.none,
  });

  AppointmentsState copyWith({
    AppointmentsStatus? status,
    List<BookingSummary>? upcoming,
    List<BookingSummary>? past,
    String? errorMessage,
    AppointmentsNotice? notice,
  }) {
    return AppointmentsState(
      status: status ?? this.status,
      upcoming: upcoming ?? this.upcoming,
      past: past ?? this.past,
      errorMessage: errorMessage,
      notice: notice ?? AppointmentsNotice.none,
    );
  }

  @override
  List<Object?> get props => [status, upcoming, past, errorMessage, notice];
}

// ---------- Bloc ----------

class AppointmentsBloc extends Bloc<AppointmentsEvent, AppointmentsState> {
  final BookingsRepository repository;

  /// Bumped when a cancel or reschedule on this screen starts and when it
  /// ends. Handlers run concurrently, so a refresh that read the lists
  /// before (or during) such a change must not overwrite it with the old
  /// copy of the booking.
  int _changes = 0;

  /// Cancels still waiting for the server.
  int _changesInFlight = 0;

  AppointmentsBloc(this.repository) : super(const AppointmentsState()) {
    on<AppointmentsRequested>(_onRequested);
    on<AppointmentsRefreshed>(_onRefreshed);
    on<AppointmentCancelled>(_onCancelled);
    on<AppointmentRescheduled>(_onRescheduled);
  }

  Future<void> _onRequested(
    AppointmentsRequested event,
    Emitter<AppointmentsState> emit,
  ) async {
    emit(state.copyWith(status: AppointmentsStatus.loading));
    await _load(emit);
  }

  Future<void> _onRefreshed(
    AppointmentsRefreshed event,
    Emitter<AppointmentsState> emit,
  ) async {
    // Nothing on screen to keep yet: a normal load.
    if (state.status == AppointmentsStatus.loading ||
        state.status == AppointmentsStatus.error) {
      return _onRequested(const AppointmentsRequested(), emit);
    }
    await _load(emit, keepOnFailure: true);
  }

  /// [keepOnFailure] is a refresh: the list is on screen, so a failed read
  /// keeps it, and a read overtaken by a change made here is dropped (the
  /// screen already shows the change; the next refresh reads the server's
  /// copy).
  Future<void> _load(
    Emitter<AppointmentsState> emit, {
    bool keepOnFailure = false,
  }) async {
    final changesBefore = _changes;
    final results = await Future.wait([
      repository.getMyBookings(upcoming: true),
      repository.getMyBookings(upcoming: false),
    ]);
    if (keepOnFailure && (_changes != changesBefore || _changesInFlight > 0)) {
      return;
    }

    if (results[0].isLeft() && results[1].isLeft()) {
      if (keepOnFailure) return;
      final message = results[0]
          .swap()
          .getOrElse(() => throw StateError('unreachable'))
          .message;
      emit(state.copyWith(
        status: AppointmentsStatus.error,
        errorMessage: message,
      ));
      return;
    }

    // A refresh keeps whichever list could not be re-read.
    final upcoming =
        results[0].getOrElse(() => keepOnFailure ? state.upcoming : const []);
    final past =
        results[1].getOrElse(() => keepOnFailure ? state.past : const []);
    emit(state.copyWith(
      status: upcoming.isEmpty && past.isEmpty
          ? AppointmentsStatus.empty
          : AppointmentsStatus.loaded,
      upcoming: upcoming,
      past: past,
    ));
  }

  Future<void> _onCancelled(
    AppointmentCancelled event,
    Emitter<AppointmentsState> emit,
  ) async {
    final before = state.upcoming;
    _changes++;
    _changesInFlight++;

    // Optimistic: flip the card immediately.
    emit(state.copyWith(
      upcoming: [
        for (final b in before)
          if (b.id == event.booking.id)
            b.copyWith(
              status: 'Cancelled',
              canCancel: false,
              canReschedule: false,
            )
          else
            b,
      ],
    ));

    final result = await repository.cancelBooking(
      bookingId: event.booking.id,
      reason: event.reason.isEmpty
          ? AppStrings.cancelBookingConfirmTitle
          : event.reason,
    );
    _changes++;
    _changesInFlight--;

    result.fold(
      // Roll back on failure.
      (failure) => emit(state.copyWith(
        upcoming: before,
        errorMessage: failure.message,
        notice: AppointmentsNotice.cancelFailure,
      )),
      (_) => emit(state.copyWith(notice: AppointmentsNotice.cancelSuccess)),
    );
  }

  void _onRescheduled(
    AppointmentRescheduled event,
    Emitter<AppointmentsState> emit,
  ) {
    _changes++;
    emit(state.copyWith(
      upcoming: [
        for (final b in state.upcoming)
          if (b.id == event.bookingId)
            b.copyWith(startTime: event.newStartTime)
          else
            b,
      ],
    ));
  }
}
