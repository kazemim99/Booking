import 'package:dartz/dartz.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/network/connectivity_service.dart';
import '../../domain/entities/home_booking.dart';
import '../../domain/repositories/home_repository.dart';

enum CalendarStatus { loading, ready, failed }

class CalendarState extends Equatable {
  final CalendarStatus status;

  /// Date-only anchor of the visible week (Saturday — the Iranian week).
  final DateTime weekStart;

  /// Date-only selected day whose timeline is shown.
  final DateTime selectedDay;

  /// Date-only day → that day's bookings (start order).
  final Map<DateTime, List<HomeBooking>> bookingsByDay;

  /// A refresh failed but previously fetched data is still shown.
  final bool stale;
  final String? error;

  const CalendarState({
    required this.status,
    required this.weekStart,
    required this.selectedDay,
    this.bookingsByDay = const {},
    this.stale = false,
    this.error,
  });

  List<HomeBooking> get selectedDayBookings =>
      bookingsByDay[selectedDay] ?? const [];

  int countFor(DateTime day) =>
      bookingsByDay[CalendarCubit.dateOnly(day)]?.length ?? 0;

  CalendarState copyWith({
    CalendarStatus? status,
    DateTime? weekStart,
    DateTime? selectedDay,
    Map<DateTime, List<HomeBooking>>? bookingsByDay,
    bool? stale,
    String? Function()? error,
  }) {
    return CalendarState(
      status: status ?? this.status,
      weekStart: weekStart ?? this.weekStart,
      selectedDay: selectedDay ?? this.selectedDay,
      bookingsByDay: bookingsByDay ?? this.bookingsByDay,
      stale: stale ?? this.stale,
      error: error != null ? error() : this.error,
    );
  }

  @override
  List<Object?> get props =>
      [status, weekStart, selectedDay, bookingsByDay, stale, error];
}

/// State for the Calendar tab (spec: provider-calendar): one fetch per
/// visible week with a stale-result guard for fast paging; day selection is
/// pure state; booking quick actions mirror the Home's semantics (offline
/// refused, success refreshes).
class CalendarCubit extends Cubit<CalendarState> {
  final HomeRepository _repository;
  final ConnectivityService _connectivity;
  final DateTime Function() _now;
  int _fetchSeq = 0;

  CalendarCubit(
    this._repository,
    this._connectivity, {
    DateTime Function()? now,
  })  : _now = now ?? DateTime.now,
        super(_initial((now ?? DateTime.now)()));

  static CalendarState _initial(DateTime now) {
    final today = dateOnly(now);
    return CalendarState(
      status: CalendarStatus.loading,
      weekStart: weekStartOf(today),
      selectedDay: today,
    );
  }

  static DateTime dateOnly(DateTime d) => DateTime(d.year, d.month, d.day);

  /// Saturday-anchored week start (DateTime.weekday: Mon=1..Sun=7, Sat=6).
  static DateTime weekStartOf(DateTime day) {
    final d = dateOnly(day);
    return d.subtract(Duration(days: (d.weekday + 1) % 7));
  }

  Future<void> load() => _fetchWeek(state.weekStart);

  /// Pure state — the week's data is already loaded.
  void selectDay(DateTime day) =>
      emit(state.copyWith(selectedDay: dateOnly(day)));

  Future<void> nextWeek() => _moveWeek(7);
  Future<void> previousWeek() => _moveWeek(-7);

  Future<void> _moveWeek(int days) {
    final weekStart = state.weekStart.add(Duration(days: days));
    emit(state.copyWith(weekStart: weekStart, selectedDay: weekStart));
    return _fetchWeek(weekStart);
  }

  Future<void> jumpToToday() {
    final today = dateOnly(_now());
    final weekStart = weekStartOf(today);
    emit(state.copyWith(weekStart: weekStart, selectedDay: today));
    return _fetchWeek(weekStart);
  }

  Future<void> refresh() => _fetchWeek(state.weekStart, keepData: true);

  Future<void> _fetchWeek(DateTime weekStart, {bool keepData = false}) async {
    final seq = ++_fetchSeq;
    emit(state.copyWith(
      status: keepData && state.bookingsByDay.isNotEmpty
          ? CalendarStatus.ready
          : CalendarStatus.loading,
      error: () => null,
    ));

    final result = await _repository.fetchBookings(
      from: weekStart,
      to: weekStart.add(const Duration(days: 7)),
    );
    if (isClosed || seq != _fetchSeq) return; // superseded — discard

    result.fold(
      (f) {
        if (state.bookingsByDay.isNotEmpty) {
          emit(state.copyWith(
              status: CalendarStatus.ready, stale: true, error: () => null));
        } else {
          emit(state.copyWith(
              status: CalendarStatus.failed, error: () => f.message));
        }
      },
      (bookings) {
        final byDay = <DateTime, List<HomeBooking>>{};
        for (final b in bookings) {
          // A booking without a parsable start lands on the week's first day
          // rather than vanishing from the calendar.
          final key = b.start == null ? weekStart : dateOnly(b.start!);
          (byDay[key] ??= []).add(b);
        }
        for (final list in byDay.values) {
          list.sort((a, b) {
            final sa = a.start, sb = b.start;
            if (sa == null || sb == null) {
              return sa == sb ? 0 : (sa == null ? -1 : 1);
            }
            return sa.compareTo(sb);
          });
        }
        emit(state.copyWith(
          status: CalendarStatus.ready,
          bookingsByDay: byDay,
          stale: false,
          error: () => null,
        ));
      },
    );
  }

  // ---- Booking quick actions (Home-identical semantics) ----

  Future<Failure?> confirmBooking(String id) =>
      _mutate(() => _repository.confirmBooking(id));

  Future<Failure?> declineBooking(String id, {required String reason}) =>
      _mutate(() => _repository.declineBooking(id, reason: reason));

  Future<Failure?> completeBooking(String id) =>
      _mutate(() => _repository.completeBooking(id));

  Future<Failure?> markNoShow(String id) =>
      _mutate(() => _repository.markNoShow(id));

  Future<Failure?> _mutate(
    Future<Either<Failure, void>> Function() call,
  ) async {
    if (!await _connectivity.isOnline) {
      return const NetworkFailure(AppStrings.offline);
    }
    if (isClosed) return null;
    final result = await call();
    return result.fold((f) => f, (_) {
      refresh();
      return null;
    });
  }
}
