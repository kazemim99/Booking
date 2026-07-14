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

  AppointmentsBloc(this.repository) : super(const AppointmentsState()) {
    on<AppointmentsRequested>(_onRequested);
    on<AppointmentCancelled>(_onCancelled);
    on<AppointmentRescheduled>(_onRescheduled);
  }

  Future<void> _onRequested(
    AppointmentsRequested event,
    Emitter<AppointmentsState> emit,
  ) async {
    emit(state.copyWith(status: AppointmentsStatus.loading));

    final results = await Future.wait([
      repository.getMyBookings(upcoming: true),
      repository.getMyBookings(upcoming: false),
    ]);

    if (results[0].isLeft() && results[1].isLeft()) {
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

    final upcoming = results[0].getOrElse(() => const []);
    final past = results[1].getOrElse(() => const []);
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
